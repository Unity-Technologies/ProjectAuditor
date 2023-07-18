using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class AudioClipAnalyzer : IAudioClipModuleAnalyzer
    {
        internal const string PAA4000 = nameof(PAA4000);    // Long AudioClips which aren’t set to streaming
        internal const string PAA4001 = nameof(PAA4001);    // Very small ACs (uncompressed size <200KB) that ARE set to streaming. These should probably be Decompress on Load
        internal const string PAA4002 = nameof(PAA4002);    // Stereo clips not forced to Mono on mobile platforms
        internal const string PAA4003 = nameof(PAA4003);    // Stereo clips not forced to Mono if they’re not streaming audio (only non-diagetic music should be stereo, really)
        internal const string PAA4004 = nameof(PAA4004);    // Decompress on Load used with long clips
        internal const string PAA4005 = nameof(PAA4005);    // Compressed In Memory used with compression formats that are not trivial to decompress (e.g. everything other than PCM or ADPCM)
        internal const string PAA4006 = nameof(PAA4006);    // Large compressed samples on mobile: Decrease quality or downsample
        internal const string PAA4007 = nameof(PAA4007);    // Bitrates > 48KHz
        internal const string PAA4008 = nameof(PAA4008);    // Preload Audio Data ticked (increases load times and is only needed for audio that must start IMMEDIATELY upon scene load)
        internal const string PAA4009 = nameof(PAA4009);    // If Load In Background isn’t enabled on ACs over (TUNEABLE) size/length (if it’s not ticked, loading will block the main thread)
        internal const string PAA4010 = nameof(PAA4010);    // If MP3 is used. Vorbis is better
        internal const string PAA4011 = nameof(PAA4011);    // Source assets that aren’t .WAV or .AIFF. Other formats (.MP3, .OGG, etc.) are lossy

        private static string s_PlatformString = "";

        internal static readonly Descriptor k_AudioLongClipDoesNotStreamDescriptor = new Descriptor(
            PAA4000,
            "Audio: Long AudioClip is not set to Streaming",
            Area.Memory,
            "The AudioClip has a runtime memory footprint larger than the streaming buffer size of 200KB, but its <b>Load Type</b> is not set to <b>Streaming</b>. Storing the whole clip in memory rather than streaming it may be an inefficient use of memory.",
            "Consider setting <b>Load Type</b> to <b>Streaming</b> in the AudioClip Import Settings."
        )
        {
            messageFormat = "AudioClip '{0}' Load Type is not set to Streaming",
            fixer = (issue) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.relativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    var sampleSettings = audioImporter.GetOverrideSampleSettings(s_PlatformString);
                    sampleSettings.loadType = AudioClipLoadType.Streaming;
                    audioImporter.SetOverrideSampleSettings(s_PlatformString, sampleSettings);
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioShortClipStreamsDescriptor = new Descriptor(
            PAA4001,
            "Audio: Short AudioClip is set to streaming",
            Area.Memory,
            "The AudioClip has a runtime memory footprint smaller than the streaming buffer size of 200KB, but its <b>Load Type</b> is set to <b>Streaming</b>. Requiring a streaming buffer for this clip is an inefficient use of memory.",
            "Set <b>Load Type</b> to <b>Compressed in Memory</b> or <b>Decompress On Load</b> in the AudioClip Import Settings."
        )
        {
            messageFormat = "AudioClip '{0}' Load Type is set to Streaming",
        };

        internal static readonly Descriptor k_AudioStereoClipsOnMobileDescriptor = new Descriptor(
            PAA4002,
            "Audio: AudioClip is stereo",
            Area.Memory,
            "The audio source asset is in stereo, and <b>Force To Mono</b> is not enabled in the AudioClip Import Settings. Stereo clips are generally not needed on mobile platforms, and have double the memory footprint of mono clips.",
            "Tick the <b>Force To Mono</b> checkbox in the AudioClip Import Settings."
        )
        {
            messageFormat = "AudioClip '{0}' is stereo",
            fixer = (issue) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.relativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    audioImporter.forceToMono = true;
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioStereoClipWhichIsNotStreamingDescriptor = new Descriptor(
            PAA4003,
            "Audio: AudioClip is stereo",
            new[] { Area.Memory, Area.Quality },
            "The audio source asset is in stereo, <b>Force To Mono</b> is not enabled in the AudioClip Import Settings, and the <b>Load Type</b> is not <b>Streaming</b>, which implies the AudioClip may be used as a diagetic positional sound effect. Positional effects should be mono; only non-diagetic music and effects should be stereo.",
            "Tick the <b>Force To Mono</b> checkbox in the AudioClip Import Settings."
        )
        {
            messageFormat = "AudioClip '{0}' is stereo",
            fixer = (issue) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.relativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    audioImporter.forceToMono = true;
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioLongDecompressedClipDescriptor = new Descriptor(
            PAA4004,
            "Audio: AudioClip is set to Decompress On Load",
            new[] {Area.Memory, Area.LoadTime},
            "The AudioClip is long, and its <b>Load Type</b> is set to <b>Decompress On Load</b>. The clip's memory footprint may be excessive, and decompression may impact load times.",
            "Consider setting the <b>Load Type</b> to <b>Compressed In Memory</b> or <b>Streaming</b>. If you have concerns about the CPU cost of decompressing <b>Compressed In Memory</b> clips for playback, consider a format which is fast to decompress, such as <b>ADPCM</b>."
        )
        {
            messageFormat = "AudioClip '{0}' is set to Decompress On Load",
        };

        internal static readonly Descriptor k_AudioCompressedInMemoryDescriptor = new Descriptor(
            PAA4005,
            "Audio: Compressed AudioClip is Compressed In Memory",
            Area.CPU,
            "The AudioClip's <b>Load Type</b> is set to <b>Compressed In Memory</b> but the clip is imported with a format that is not trivial to decompress. Decompression will be performed every time the clip is played, and may impact CPU performance.",
            "If runtime performance is impacted, either set the <b>Load Type</b> to <b>Decompress On Load</b> or set the <b>Compression Format</b> to <b>ADPCM</b>, which is fast to decompress."
        )
        {
            messageFormat = "AudioClip '{0}' is Compressed In Memory",
        };

        // Large compressed samples on mobile: Decrease quality or downsample
        internal static readonly Descriptor k_AudioLargeCompressedMobileDescriptor = new Descriptor(
            PAA4006,
            "Audio: Compressed clip could be optimized for mobile",
            new[] {Area.Memory, Area.BuildSize},
            "The AudioClip has a large file size despite using compression. Mobile speakers and headphones are generally of mediocre quality and cannot discernibly reproduce very high-fidelity sounds, so there may be an opportunity to optimize the clip's file size and memory footprint.",
            "Reduce the <b>Quality</b> slider as far as possible without introducing audible artefacts. Alternatively, try setting the <b>Sample Rate Setting</b> to <b>Override</b> and the <b>Sample Rate</b> to a suitable value. <b>22050</b> Hz or is fine for most sounds, and <b>44100</b> Hz (CD Quality) can be useful for prominent sounds or music if they include high frequencies."
        )
        {
            messageFormat = "AudioClip '{0}' Compressed clip could be optimized for mobile",
        };

        internal static readonly Descriptor k_Audio48KHzDescriptor = new Descriptor(
            PAA4007,
            "Audio: Sample Rate is over 48 KHz",
            new[] {Area.Memory, Area.BuildSize, Area.LoadTime},
            "The AudioClip's source sample rate is higher than 48 KHz, and the <b>Sample Rate Setting</b> does not override it. Most Blu-Rays are at 48KHz, and higher sample rates are generally only used during the recording process or for scientific data. If compression is applied during the import process the sample rate gets capped at 48KHz. If compression isn't applied, the runtime memory footprint for this clip will be excessive. In both cases, the source file size is excessive.",
            "Set the <b>Sample Rate Setting</b> to <b>Override</b> and the <b>Sample Rate</b> to <b>48000</b> Hz or lower."
        )
        {
            messageFormat = "AudioClip '{0}' Sample Rate is over 48KHz",
            fixer = (issue) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.relativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    var sampleSettings = audioImporter.GetOverrideSampleSettings(s_PlatformString);
                    sampleSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                    sampleSettings.sampleRateOverride = 48000;
                    audioImporter.SetOverrideSampleSettings(s_PlatformString, sampleSettings);
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioPreloadDescriptor = new Descriptor(
            PAA4008,
            "Audio: Preload Audio Data is enabled",
            Area.LoadTime,
            "The <b>Preload Audio Data</b> checkbox is ticked for this AudioClip. This forces scene/prefab loading to wait synchronously until the AudioClip has completed loading before continuing running, and can impact scene load/initialization times.",
            "Consider un-ticking the <b>Preload Audio Data</b> checkbox. Audio preloading is only required when the AudioClip must play at the exact moment the scene begins simulating, or if the audio timing must be very precise the first time it is played."
        )
        {
            messageFormat = "AudioClip '{0}' is set to Preload Audio Data",
            fixer = (issue) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.relativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    audioImporter.preloadAudioData = false;
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioLoadInBackgroundDisabledDescriptor = new Descriptor(
            PAA4009,
            "Audio: Load In Background is not enabled",
            new [] {Area.CPU, Area.LoadTime},
            "This AudioClip is large, and the <b>Load In Background</b> checkbox is not ticked. Loading will be performed synchronously and will block the main thread. This may impact load times or create CPU spikes, depending on when the clip is loaded.",
            "Tick the <b>Load In Background</b> checkbox in the AudioClip Import Settings."
        )
        {
            messageFormat = "AudioClip '{0}' Load In Background is not enabled",
            fixer = (issue) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.relativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    audioImporter.loadInBackground = true;
                    audioImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_AudioMP3Descriptor = new Descriptor(
            PAA4010,
            "Audio: Compression Format is MP3",
            Area.Quality,
            "The AudioClip's <b>Compression Format</b> is set to <b>MP3</b>. MP3 is an old compression format which has been surpassed in efficiency and quality by newer formats such as Vorbis.",
            "Set the <b>Compression Format</b> to <b>Vorbis</b> in the AudioClip's Import Settings."
        )
        {
            messageFormat = "AudioClip '{0}' Compression Format is MP3",
            fixer = (issue) =>
            {
                var audioImporter = AssetImporter.GetAtPath(issue.relativePath) as AudioImporter;
                if (audioImporter != null)
                {
                    var sampleSettings = audioImporter.GetOverrideSampleSettings(s_PlatformString);
                    sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                    audioImporter.SetOverrideSampleSettings(s_PlatformString, sampleSettings);
                    audioImporter.SaveAndReimport();
                }
            }
        };

        // Source assets that aren’t .WAV or .AIFF. Other formats (.MP3, .OGG, etc.) are lossy
        internal static readonly Descriptor k_AudioCompressedSourceAssetDescriptor = new Descriptor(
            PAA4011,
            "Audio: Source asset is in a lossy compressed format",
            Area.Quality,
            "The file format used by the source asset for the AudioClip uses a lossy compression format. The Asset Import process decompresses the audio data and recompresses it in the chosen runtime format. This may result in a further loss of sound quality.",
            "Wherever possible, select a lossless file format such as .WAV or .AIFF for source assets."
        )
        {
            messageFormat = "AudioClip '{0}' source asset is in a lossy compressed format",
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_AudioLongClipDoesNotStreamDescriptor);
            module.RegisterDescriptor(k_AudioShortClipStreamsDescriptor);
            module.RegisterDescriptor(k_AudioStereoClipsOnMobileDescriptor);
            module.RegisterDescriptor(k_AudioStereoClipWhichIsNotStreamingDescriptor);
            module.RegisterDescriptor(k_AudioLongDecompressedClipDescriptor);
            module.RegisterDescriptor(k_AudioCompressedInMemoryDescriptor);
            module.RegisterDescriptor(k_AudioLargeCompressedMobileDescriptor);
            module.RegisterDescriptor(k_Audio48KHzDescriptor);
            module.RegisterDescriptor(k_AudioPreloadDescriptor);
            module.RegisterDescriptor(k_AudioLoadInBackgroundDisabledDescriptor);
            module.RegisterDescriptor(k_AudioMP3Descriptor);
            module.RegisterDescriptor(k_AudioCompressedSourceAssetDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, AudioImporter audioImporter)
        {
            var assetPath = audioImporter.assetPath;
            s_PlatformString = projectAuditorParams.platform.ToString();

            var sampleSettings = audioImporter.GetOverrideSampleSettings(s_PlatformString);
            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            // GET CLIP STATS

            var clipName = Path.GetFileNameWithoutExtension(assetPath);
            // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
            var runtimeSize = Profiler.GetRuntimeMemorySizeLong(audioClip);
            var origSize = (int)GetPropertyValue(audioImporter, "origSize");
            var compSize = (int)GetPropertyValue(audioImporter, "compSize");

            // REPORT FILE FOR AUDIOCLIP VIEW

            var ts = new TimeSpan(0, 0, 0, 0, (int)(audioClip.length * 1000.0f));

            yield return ProjectIssue.Create(IssueCategory.AudioClip, clipName)
                .WithCustomProperties(
                    new object[(int)AudioClipProperty.Num]
            {
                String.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds),
                origSize,
                compSize,
                runtimeSize,
                (100.0f * (float)compSize / (float)origSize).ToString("0.00", CultureInfo.InvariantCulture.NumberFormat) + "%",
                sampleSettings.compressionFormat,
                ((float)audioClip.frequency / 1000.0f).ToString("G0", CultureInfo.InvariantCulture.NumberFormat) + " KHz",
                audioImporter.forceToMono,
                audioImporter.loadInBackground,
#if UNITY_2022_2_OR_NEWER
                sampleSettings.preloadAudioData,
#else
                audioImporter.preloadAudioData,
#endif
                sampleSettings.loadType,

            }).WithLocation(assetPath);

            // DIAGNOSTICS

            bool isMobileTarget = (projectAuditorParams.platform == BuildTarget.Android ||
                                   projectAuditorParams.platform == BuildTarget.iOS ||
                                   projectAuditorParams.platform == BuildTarget.Switch);

            bool isStreaming = sampleSettings.loadType == AudioClipLoadType.Streaming;

            var sourceFileExtension = System.IO.Path.GetExtension(assetPath).ToUpper() ?? string.Empty;
            if (sourceFileExtension.StartsWith("."))
                sourceFileExtension = sourceFileExtension.Substring(1);

            if (runtimeSize > projectAuditorParams.settings.StreamingClipThresholdBytes && !isStreaming)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioLongClipDoesNotStreamDescriptor, clipName)
                     .WithLocation(assetPath);
            }

            if (runtimeSize < projectAuditorParams.settings.StreamingClipThresholdBytes && isStreaming)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioShortClipStreamsDescriptor, clipName)
                    .WithLocation(assetPath);
            }

            if (audioClip.channels > 1 && audioImporter.forceToMono == false)
            {
                if(isMobileTarget)
                {
                    yield return ProjectIssue.Create(
                            IssueCategory.AssetDiagnostic, k_AudioStereoClipsOnMobileDescriptor, clipName)
                        .WithLocation(assetPath);
                }
                else if(!isStreaming)
                {
                    yield return ProjectIssue.Create(
                            IssueCategory.AssetDiagnostic, k_AudioStereoClipWhichIsNotStreamingDescriptor, clipName)
                        .WithLocation(assetPath);
                }
            }

            if (runtimeSize > projectAuditorParams.settings.LongDecompressedClipThresholdBytes &&
                sampleSettings.loadType == AudioClipLoadType.DecompressOnLoad)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioLongDecompressedClipDescriptor, clipName)
                    .WithLocation(assetPath);
            }

            if (sampleSettings.loadType == AudioClipLoadType.CompressedInMemory &&
                sampleSettings.compressionFormat != AudioCompressionFormat.PCM &&
                sampleSettings.compressionFormat != AudioCompressionFormat.ADPCM)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioCompressedInMemoryDescriptor, clipName)
                    .WithLocation(assetPath);
            }

            if (isMobileTarget &&
                compSize > projectAuditorParams.settings.LongCompressedMobileClipThresholdBytes &&
                sampleSettings.compressionFormat != AudioCompressionFormat.PCM &&
                sampleSettings.compressionFormat != AudioCompressionFormat.ADPCM &&
                audioClip.frequency >= 48000 &&
                sampleSettings.quality > 0.98f)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioLargeCompressedMobileDescriptor, clipName)
                    .WithLocation(assetPath);
            }

            // Annoyingly, if a clip is compressed, it can't go higher than 48KHz: The frequency gets clamped when it's
            // passed to FMOD and it's not trivial to get the sample rate of the original source audio file. If we find
            // a workaround for that, we should change this. In the meantime, it's useful for uncompressed samples at least.
            if (audioClip.frequency > 48000)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_Audio48KHzDescriptor, clipName)
                    .WithLocation(assetPath);
            }

            if (audioImporter.preloadAudioData)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioPreloadDescriptor, clipName)
                    .WithLocation(assetPath);
            }

            if (!audioImporter.loadInBackground && compSize > projectAuditorParams.settings.LoadInBackGroundClipSizeThresholdBytes)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioLoadInBackgroundDisabledDescriptor, clipName)
                    .WithLocation(assetPath);
            }

            if (sampleSettings.compressionFormat == AudioCompressionFormat.MP3)
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioMP3Descriptor, clipName)
                    .WithLocation(assetPath);
            }

            if (sourceFileExtension != "WAV" &&
                sourceFileExtension != "AIFF" &&
                sourceFileExtension != "AIF")
            {
                yield return ProjectIssue.Create(
                        IssueCategory.AssetDiagnostic, k_AudioCompressedSourceAssetDescriptor, clipName)
                    .WithLocation(assetPath);
            }
        }

        private static object GetPropertyValue(AssetImporter assetImporter, string propertyName)
        {
            Type objType = assetImporter.GetType();
            PropertyInfo propInfo = objType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            return propInfo.GetValue(assetImporter, null);
        }
    }
}
