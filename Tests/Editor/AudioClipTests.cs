using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    internal class AudioClipTests : TestFixtureBase
    {
        private byte[] m_VeryLongWavData;
        private byte[] m_LongWavData;
        private byte[] m_ShortWavData;

        const string k_LongNonStreamingClipName = "LongNonStreamingClip.wav";
        const string k_ShortNonStreamingClipName = "ShortNonStreamingClip.wav";
        const string k_LongStreamingClipName = "LongStreamingClip.wav";
        const string k_ShortStreamingClipName = "ShortStreamingClip.wav";
        const string k_CompressedInMemoryClipName = "CompressedInMemoryClip.wav";
        const string k_PCMInMemoryClipName = "PCMInMemoryClip.wav";

        TestAsset m_TestLongNonStreamingClipAsset;
        TestAsset m_TestShortNonStreamingClipAsset;
        TestAsset m_TestLongStreamingClipAsset;
        TestAsset m_TestShortStreamingClipAsset;
        TestAsset m_TestCompressedInMemoryClipAsset;
        TestAsset m_TestPCMInMemoryClipAsset;

        private string m_BuildTargetString;


        [OneTimeSetUp]
        public void SetUp()
        {
            m_VeryLongWavData = AudioClipGeneratorUtil.CreateTestWav( 640000, 2, 48000);
            m_LongWavData = AudioClipGeneratorUtil.CreateTestWav( 64000, 2, 48000);
            m_ShortWavData = AudioClipGeneratorUtil.CreateTestWav( 500, 2, 96000);

            m_BuildTargetString = EditorUserBuildSettings.activeBuildTarget.ToString();

            m_TestLongNonStreamingClipAsset = CreateTestAudioClip(
                k_LongNonStreamingClipName, m_LongWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad);

            m_TestShortNonStreamingClipAsset = CreateTestAudioClip(
                k_ShortNonStreamingClipName, m_ShortWavData, m_BuildTargetString,
                AudioCompressionFormat.Vorbis, AudioClipLoadType.DecompressOnLoad, true);

            m_TestLongStreamingClipAsset = CreateTestAudioClip(
                k_LongStreamingClipName, m_LongWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.Streaming);

            m_TestShortStreamingClipAsset = CreateTestAudioClip(
                k_ShortStreamingClipName, m_ShortWavData, m_BuildTargetString,
                AudioCompressionFormat.Vorbis, AudioClipLoadType.Streaming, true);

            m_TestCompressedInMemoryClipAsset = CreateTestAudioClip(
                k_CompressedInMemoryClipName, m_VeryLongWavData, m_BuildTargetString,
                AudioCompressionFormat.Vorbis, AudioClipLoadType.CompressedInMemory, true);

            m_TestPCMInMemoryClipAsset = CreateTestAudioClip(
                k_PCMInMemoryClipName, m_ShortWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.CompressedInMemory, true);
        }

        private TestAsset CreateTestAudioClip(string name, byte[] data, string platformString,
            AudioCompressionFormat format, AudioClipLoadType loadType,
            bool forceToMono = false, bool preload = true, bool loadInBackground = false)
        {
            var testAsset = new TestAsset(name, data);
            var audioImporter = AssetImporter.GetAtPath(testAsset.relativePath) as AudioImporter;
            Assert.NotNull(audioImporter);

            var sampleSettings = audioImporter.GetOverrideSampleSettings(platformString);
            sampleSettings.compressionFormat = format;
            sampleSettings.loadType = loadType;


#if UNITY_2022_2_OR_NEWER
            sampleSettings.preloadAudioData = preload;
#else
            audioImporter.preloadAudioData = preload;
#endif

            audioImporter.forceToMono = forceToMono;
            audioImporter.loadInBackground = loadInBackground;

            audioImporter.SetOverrideSampleSettings(platformString, sampleSettings);
            audioImporter.SaveAndReimport();
            return testAsset;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }

        // PAA4000 Long AudioClips which aren’t set to streaming
        [Test]
        public void AudioClip_LongNonStreaming_IsReportedAndFixed()
        {
            var asset = CreateTestAudioClip(
                "PAA4000.wav", m_LongWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad);

            var issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioLongClipDoesNotStreamDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.NotNull(issue.descriptor.fixer);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4000);

            issue.descriptor.Fix(issue);

            issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioLongClipDoesNotStreamDescriptor));

            Assert.Null(issue);
        }

        // PAA4001 Very small ACs (uncompressed size <200KB) that ARE set to streaming. These should probably be Decompress on Load
        [Test]
        public void AudioClip_ShortStreaming_IsReported()
        {
            var issue = AnalyzeAndFindAssetIssues(m_TestShortStreamingClipAsset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioShortClipStreamsDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4001);
        }

        // PAA4002 Stereo clips not forced to Mono on mobile platforms
        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void AudioClip_StereoClipNotForcedToMonoOnMobile_IsReportedAndFixed()
        {
            var platform = m_Platform;
            m_Platform = BuildTarget.Android;

            var asset = CreateTestAudioClip(
                "PAA4002.wav", m_ShortWavData, BuildTarget.Android.ToString(),
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad);

            var issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioStereoClipsOnMobileDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4002);

            issue.descriptor.Fix(issue);

            issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioStereoClipsOnMobileDescriptor));

            Assert.Null(issue);

            m_Platform = platform;
        }

        // PAA4003 Stereo clips not forced to Mono on non-mobile platforms if they’re not streaming audio (only non-diagetic music should be stereo, really)
        [Test]
#if UNITY_EDITOR_WIN
        [RequirePlatformSupport(BuildTarget.StandaloneWindows)]
#elif UNITY_EDITOR_OSX
        [RequirePlatformSupport(BuildTarget.StandaloneOSX)]
#elif UNITY_EDITOR_LINUX
        [RequirePlatformSupport(BuildTarget.StandaloneLinux)]
#endif
        public void AudioClip_NonStreamingStereoClipNotForcedToMono_IsReportedAndFixed()
        {
            var platform = m_Platform;

#if UNITY_EDITOR_WIN
            m_Platform = BuildTarget.StandaloneWindows;
#elif UNITY_EDITOR_OSX
            m_Platform = BuildTarget.StandaloneOSX;
#elif UNITY_EDITOR_LINUX
            m_Platform = BuildTarget.StandaloneLinux;
#endif
            var asset = CreateTestAudioClip(
                "PAA4003.wav", m_ShortWavData, m_Platform.ToString(),
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad);

            var issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioStereoClipWhichIsNotStreamingDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4003);

            issue.descriptor.Fix(issue);

            issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioStereoClipWhichIsNotStreamingDescriptor));

            Assert.Null(issue);

            m_Platform = platform;
        }

        // PAA4004 Decompress on Load used with long clips
        [Test]
        public void AudioClip_LongClipDecompressOnLoad_IsReported()
        {
            var issue = AnalyzeAndFindAssetIssues(m_TestLongNonStreamingClipAsset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioLongDecompressedClipDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4004);
        }

        // PAA4005 Compressed In Memory used with compression formats that are not trivial to decompress (e.g. everything other than PCM or ADPCM)
        [Test]
        public void AudioClip_CompressedInMemory_IsReported()
        {
            var issue = AnalyzeAndFindAssetIssues(m_TestCompressedInMemoryClipAsset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioCompressedInMemoryDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4005);
        }

        // PAA4006 Large compressed samples on mobile: Decrease quality or downsample
        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void AudioClip_LargeCompressedOnMobile_IsReported()
        {
            var platform = m_Platform;
            m_Platform = BuildTarget.Android;

            var issue = AnalyzeAndFindAssetIssues(m_TestCompressedInMemoryClipAsset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioLargeCompressedMobileDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4006);

            m_Platform = platform;
        }

        // PAA4007 Bitrates > 48KHz
        [Test]
        public void AudioClip_HighBitrate_IsReportedAndFixed()
        {
            // m_ShortWavData is 96KHz when not compressed
            var asset = CreateTestAudioClip(
                "PAA4007.wav", m_ShortWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad, true);

            var issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_Audio48KHzDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4007);

            issue.descriptor.Fix(issue);

            issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_Audio48KHzDescriptor));

            Assert.Null(issue);
        }

        // PAA4008 Preload Audio Data ticked (increases load times and is only needed for audio that must start IMMEDIATELY upon scene load)
        [Test]
        public void AudioClip_PreloadAudioData_IsReportedAndFixed()
        {
            var asset = CreateTestAudioClip(
                "PAA4008.wav", m_ShortWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad, true, true);

            var issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioPreloadDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4008);

            issue.descriptor.Fix(issue);

            issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioPreloadDescriptor));

            Assert.Null(issue);
        }

        // PAA4009 If Load In Background isn’t enabled on ACs over (TUNEABLE) size/length (if it’s not ticked, loading will block the main thread)
        [Test]
        public void AudioClip_LoadInBackGroundNotEnabled_IsReportedAndFixed()
        {
            var asset = CreateTestAudioClip(
                "PAA4009.wav", m_LongWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad);

            var issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioLoadInBackgroundDisabledDescriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4009);

            issue.descriptor.Fix(issue);

            issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioLoadInBackgroundDisabledDescriptor));

            Assert.Null(issue);
        }

        // PAA4010 If MP3 is used. Vorbis is better
        [Test]
        [RequirePlatformSupport(BuildTarget.iOS)]
        public void AudioClip_MP3Compression_IsReportedAndFixed()
        {
            var platform = m_Platform;
            m_Platform = BuildTarget.iOS;

            var asset = CreateTestAudioClip(
                "PAA4010.wav", m_LongWavData, BuildTarget.iOS.ToString(),
                AudioCompressionFormat.MP3, AudioClipLoadType.DecompressOnLoad);

            var issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioMP3Descriptor));

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);
            Assert.IsTrue(issue.descriptor.id == AudioClipAnalyzer.PAA4010);

            issue.descriptor.Fix(issue);

            issue = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.descriptor.Equals(AudioClipAnalyzer.k_AudioMP3Descriptor));

            Assert.Null(issue);

            m_Platform = platform;
        }

        // PAA4011 Source assets that aren’t .WAV or .AIFF. Other formats (.MP3, .OGG, etc.) are lossy
        // TODO: This test hasn't been implemented, largely due to the fact that generating a valid .MP3 or .OGG source asset is considerably more complex than generating a WAV

        // ----------------------------
        // TESTS FOR FALSE POSITIVES
        // ----------------------------

        // Testing to make sure we don't report false positives for:
        // PAA4002 Stereo clips not forced to Mono on mobile platforms
        // PAA4003 Stereo clips not forced to Mono on non-mobile platforms if they’re not streaming audio (only non-diagetic music should be stereo, really)
        // PAA4006 Large compressed samples on mobile: Decrease quality or downsample
        [Test]
#if UNITY_EDITOR_WIN
        [RequirePlatformSupport(BuildTarget.Android, BuildTarget.StandaloneWindows)]
#elif UNITY_EDITOR_OSX
        [RequirePlatformSupport(BuildTarget.Android, BuildTarget.StandaloneOSX)]
#elif UNITY_EDITOR_LINUX
        [RequirePlatformSupport(BuildTarget.Android, BuildTarget.StandaloneLinux)]
#endif
        public void AudioClip_StereoFalsePositives_AreNotReported()
        {
            var platform = m_Platform;
            m_Platform = BuildTarget.Android;

            var foundIssues = AnalyzeAndFindAssetIssues(m_TestShortNonStreamingClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4002));

            foundIssues = AnalyzeAndFindAssetIssues(m_TestLongNonStreamingClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4003));

#if UNITY_EDITOR_WIN
            m_Platform = BuildTarget.StandaloneWindows;
#elif UNITY_EDITOR_OSX
            m_Platform = BuildTarget.StandaloneOSX;
#elif UNITY_EDITOR_LINUX
            m_Platform = BuildTarget.StandaloneLinux;
#endif

            foundIssues = AnalyzeAndFindAssetIssues(m_TestLongStreamingClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4002));

            foundIssues = AnalyzeAndFindAssetIssues(m_TestShortNonStreamingClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4003));

            foundIssues = AnalyzeAndFindAssetIssues(m_TestCompressedInMemoryClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4006));

            m_Platform = platform;
        }

        // Testing to make sure we don't report false positives for:
        // PAA4000 Long AudioClips which aren’t set to streaming
        // PAA4001 Very small ACs (uncompressed size <200KB) that ARE set to streaming. These should probably be Decompress on Load
        // PAA4004 Decompress on Load used with long clips
        // PAA4005 Compressed In Memory used with compression formats that are not trivial to decompress (e.g. everything other than PCM or ADPCM)
        // PAA4006 Large compressed samples on mobile: Decrease quality or downsample
        // PAA4007 Bitrates > 48KHz
        // PAA4009 If Load In Background isn’t enabled on ACs over (TUNEABLE) size/length (if it’s not ticked, loading will block the main thread)
        // PAA4010 If MP3 is used. Vorbis is better
        // PAA4011 Source assets that aren’t .WAV or .AIFF. Other formats (.MP3, .OGG, etc.) are lossy
        [Test]
        public void AudioClip_FalsePositives_AreNotReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TestShortNonStreamingClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4000));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4001));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4004));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4005));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4006));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4007)); // Compression clamps bitrate to 48KHz
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4010));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4011));

            foundIssues = AnalyzeAndFindAssetIssues(m_TestLongStreamingClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4000));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4001));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4004));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4005));

            foundIssues = AnalyzeAndFindAssetIssues(m_TestPCMInMemoryClipAsset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4005));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4006));
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4009)); // It has loadInBackground = true

            var asset = CreateTestAudioClip(
                "xPAA4008.wav", m_ShortWavData, m_BuildTargetString,
                AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad, true, false);
            foundIssues = AnalyzeAndFindAssetIssues(asset, IssueCategory.AssetDiagnostic);
            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == AudioClipAnalyzer.PAA4008));
        }
    }
}
