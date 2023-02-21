using System;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Profiling
{
    public enum TargetFramerate
    {
        [InspectorName("30")]
        FPS30,
        [InspectorName("60")]
        FPS60,
        [InspectorName("120")]
        FPS120
    }

    public enum CPUTimeArea
    {
        Uncategorized,
        WaitMarkers,
        EditorAndProfiler,
        Behaviour,
        VisualScripting,
        Rendering,
        Animation,
        Physics,
        UI,
        Audio,
        Events,
        Input,
        AI,
        LoadStream,
        Cinemachine,
        GC
    }

    public enum CPUTimeSubarea
    {
        None,
        RenderingParticles,
        RenderingVFX,
        RenderingStreaming,
    }

    public struct MarkerDefinition
    {
        private string m_Name;
        public string Name
        {
            set { m_Name = value; UpdateHashCode(); }
            get => m_Name;
        }

        public int NameHash;
        public int Level;
        public CPUTimeArea Area;
        public CPUTimeSubarea SubArea;
        public bool NameIsSubString;

        public MarkerDefinition[] SubMarkers;

        internal MarkerDefinition(int level, string name, CPUTimeArea area, CPUTimeSubarea subArea = CPUTimeSubarea.None, bool nameIsSubstring = false, MarkerDefinition[] subMarkers = null)
        {
            m_Name = name;
            NameHash = m_Name.GetHashCode();
            Level = level;
            Area = area;
            SubArea = subArea;
            NameIsSubString = nameIsSubstring;
            SubMarkers = subMarkers;
        }

        void UpdateHashCode()
        {
            NameHash = Name.GetHashCode();
        }
    }

    public struct MarkerStats
    {
        public MarkerDefinition Definition;

        public float FrameTimeMs;
        public float FrameTimePercentage;
        public float FrameGcMemory;

        public float SubMarkersTimeMs;
        public float SubMarkersPercent;
        public float SubMarkersGcMemory;
    }

    public struct FrameInfo
    {
        public float TotalTimeMs;
        public int TotalGcMemory;

        public MarkerStats[] MarkerStats;
    }

    public struct ProfileReport
    {
        public int NumFrames;

        // Lowest/highest frames and frame timing limits
        public int LowFrameIdx;
        public float LowFPS;
        public float LowFrameMs;

        public int HighFrameIdx;
        public float HighFPS;
        public float HighFrameMs;

        public float SlowFrameTime;
        public float FastFrameTime;

        // Frame timing stats for N-th percentile of fastest frames
        public int NumSlowFrameTimeFrames;
        public int NumFastFrameTimeFrames;
        public int PercentileFrameCount;
        public float MaxPercentileFrameTime;

        public FrameInfo[] FrameInfos;

        public delegate bool MarkerStatConditionDelegate(ref MarkerStats stats);

        public void Init()
        {
            NumFrames = 0;

            LowFrameIdx = -1;
            LowFPS = float.MaxValue;
            LowFrameMs = float.MinValue;

            HighFrameIdx = -1;
            HighFPS = float.MinValue;
            HighFrameMs = float.MaxValue;

            SlowFrameTime = 0;
            FastFrameTime = 0;

            NumSlowFrameTimeFrames = 0;
            NumFastFrameTimeFrames = 0;
            PercentileFrameCount = 0;

            MaxPercentileFrameTime = 0;

            FrameInfos = new FrameInfo[1];
        }

        public bool IsValid() => NumFrames > 0;

        public FrameInfo[] GetFramesWithMarkerPredicate(MarkerStatConditionDelegate markerPredicate, int startFrameIndex, int endFrameIndex)
        {
            if (startFrameIndex < 0)
            {
                throw new ArgumentOutOfRangeException($"Parameter startFrameIndex {startFrameIndex} is out of bounds, lower than first index 0 (zero).");
            }
            if (endFrameIndex > NumFrames - 1)
            {
                throw new ArgumentOutOfRangeException($"Parameter endFrameIndex {endFrameIndex} is out of bounds, higher than last index {NumFrames - 1}.");
            }
            if (startFrameIndex > endFrameIndex)
            {
                throw new ArgumentException($"Parameter startFrameIndex {startFrameIndex} is higher than endFrameIndex {endFrameIndex}. It should be equal or lower instead.");
            }

            var length = endFrameIndex - startFrameIndex + 1;
            FrameInfo[] outFrameInfos = new FrameInfo[length];

            for (int i = 0; i < length; ++i)
            {
                ref readonly FrameInfo inFrameInfo = ref FrameInfos[i + startFrameIndex];
                ref FrameInfo outFrameInfo = ref outFrameInfos[i];
                List<MarkerStats> stats = new List<MarkerStats>();

                for (int j = 0; j < inFrameInfo.MarkerStats.Length; ++j)
                {
                    ref MarkerStats m = ref inFrameInfo.MarkerStats[j];

                    if (markerPredicate(ref m))
                    {
                        stats.Add(m);
                    }
                }

                outFrameInfo.MarkerStats = stats.ToArray();
            }

            return outFrameInfos;
        }
    }

    public class ProfileAnalyzer
    {
        internal static readonly float k_MinProfilerMarkerPercentage = 0.001f;

        public struct ProfileAnalyzerParams
        {
            public TargetFramerate TargetFramerate;
            public float FastFramePercentile;
        }

        // A nested list of important known Profiler markers
        internal static readonly MarkerDefinition[] MarkerDefinitions =
        {
            // Animation
            new MarkerDefinition(1, "Initialization.DirectorSampleTime", CPUTimeArea.Animation),
            new MarkerDefinition(1, "Update.DirectorUpdate", CPUTimeArea.Animation),
            new MarkerDefinition(1, "PreLateUpdate.DirectorUpdateAnimationBegin", CPUTimeArea.Animation),
            new MarkerDefinition(1, "PreLateUpdate.DirectorUpdateAnimationEnd", CPUTimeArea.Animation),
            new MarkerDefinition(1, "PreLateUpdate.ConstraintManagerUpdate", CPUTimeArea.Animation),
            new MarkerDefinition(1, "PreLateUpdate.LegacyAnimationUpdate", CPUTimeArea.Animation),
            new MarkerDefinition(1, "FixedUpdate.LegacyFixedAnimationUpdate", CPUTimeArea.Animation),
            new MarkerDefinition(1, "PostLateUpdate.DirectorLateUpdate", CPUTimeArea.Animation),

            // Physics
            new MarkerDefinition(1, "EarlyUpdate.PhysicsResetInterpolatedTransformPosition", CPUTimeArea.Physics),
            new MarkerDefinition(1, "PreUpdate.PhysicsUpdate", CPUTimeArea.Physics),
            new MarkerDefinition(1, "PreUpdate.Physics2DUpdate", CPUTimeArea.Physics),
            new MarkerDefinition(1, "FixedUpdate.PhysicsFixedUpdate", CPUTimeArea.Physics),
            new MarkerDefinition(1, "FixedUpdate.Physics2DFixedUpdate", CPUTimeArea.Physics),
            new MarkerDefinition(1, "FixedUpdate.FixedUpdate.ScriptRunDelayedFixedFrameRate", CPUTimeArea.Physics),
            new MarkerDefinition(1, "PostLateUpdate.PhysicsSkinnedClothBeginUpdate", CPUTimeArea.Physics),

            // Behaviour
            new MarkerDefinition(1, "FixedUpdate.ScriptRunBehaviourFixedUpdate", CPUTimeArea.Behaviour, CPUTimeSubarea.None, false,
                new MarkerDefinition[]
                {
                    new MarkerDefinition(2, "EventMachine`2.FixedUpdate()", CPUTimeArea.VisualScripting, CPUTimeSubarea.None, true),
                    new MarkerDefinition(2, ".FixedUpdate()", CPUTimeArea.Behaviour, CPUTimeSubarea.None, true),
                }
            ),
            new MarkerDefinition(1, "FixedUpdate.ScriptRunDelayedFixedFrameRate", CPUTimeArea.Behaviour, CPUTimeSubarea.None, false,
                new MarkerDefinition[]
                {
                    new MarkerDefinition(3, "CinemachineBrain.AfterPhysics()", CPUTimeArea.Cinemachine, CPUTimeSubarea.None, true),
                }
            ),
            new MarkerDefinition(1, "Update.ScriptRunBehaviourUpdate", CPUTimeArea.Behaviour, CPUTimeSubarea.None, false,
                new MarkerDefinition[]
                {
                    new MarkerDefinition(2, "EventMachine`2.Update()", CPUTimeArea.VisualScripting, CPUTimeSubarea.None, true),
                    new MarkerDefinition(2, "DecalProjector.Update()", CPUTimeArea.Rendering),
                    new MarkerDefinition(2, "EventSystem.Update()", CPUTimeArea.UI),

                    new MarkerDefinition(2, ".Update()", CPUTimeArea.Behaviour, CPUTimeSubarea.None, true,
                        new MarkerDefinition[]
                        {
                            new MarkerDefinition(3, "Physics.RaycastAll", CPUTimeArea.Behaviour),
                            new MarkerDefinition(3, "Instantiate", CPUTimeArea.Behaviour),
                            new MarkerDefinition(3, "AddComponent", CPUTimeArea.Behaviour),
                        }
                    ),
                    new MarkerDefinition(2, "GC.Collect", CPUTimeArea.GC),
                }
            ),
            new MarkerDefinition(1, "PreLateUpdate.ScriptRunBehaviourLateUpdate", CPUTimeArea.Behaviour, CPUTimeSubarea.None, false,
                new MarkerDefinition[]
                {
                    new MarkerDefinition(2, "EventMachine`2.LateUpdate()", CPUTimeArea.VisualScripting, CPUTimeSubarea.None, true),
                    new MarkerDefinition(2, "CinemachineBrain.LateUpdate()", CPUTimeArea.Cinemachine, CPUTimeSubarea.None, false),
                    new MarkerDefinition(2, "VolumetricFog.LateUpdate()", CPUTimeArea.Rendering, CPUTimeSubarea.None, false),
                    new MarkerDefinition(2, "DecalProjector.LateUpdate()", CPUTimeArea.Rendering),
                    new MarkerDefinition(2, ".LateUpdate()", CPUTimeArea.Behaviour, CPUTimeSubarea.None, true),
                }
            ),

            // Events
            new MarkerDefinition(1, "PostLateUpdate.PlayerSendFrameStarted", CPUTimeArea.Events, CPUTimeSubarea.None, false,
                new MarkerDefinition[]
                {
                    new MarkerDefinition(2, "NativeInputSystem.", CPUTimeArea.Input, CPUTimeSubarea.None, true),
                }
            ),
            new MarkerDefinition(1, "PostLateUpdate.PlayerSendFrameComplete", CPUTimeArea.Events),

            // Input
            new MarkerDefinition(1, "InputSystemPlayerLoopRunnerInitializationSystem", CPUTimeArea.Input),
            new MarkerDefinition(1, "EarlyUpdate.UpdateInputManager", CPUTimeArea.Input),
            new MarkerDefinition(1, "PreUpdate.NewInputUpdate", CPUTimeArea.Input),
            new MarkerDefinition(1, "PreUpdate.SendMouseEvents", CPUTimeArea.Input),
            new MarkerDefinition(1, "InputProcess", CPUTimeArea.Input),
            new MarkerDefinition(1, "FixedUpdate.NewInputFixedUpdate", CPUTimeArea.Input),

            // AI (Navigation)
            new MarkerDefinition(1, "PreUpdate.AIUpdate", CPUTimeArea.AI),
            new MarkerDefinition(1, "PreLateUpdate.AIUpdatePostScript", CPUTimeArea.AI),

            // Rendering
            new MarkerDefinition(1, "PreLateUpdate.ParticleSystemBeginUpdateAll", CPUTimeArea.Rendering, CPUTimeSubarea.RenderingParticles),
            new MarkerDefinition(1, "PreLateUpdate.EndGraphicsJobsAfterScriptUpdate", CPUTimeArea.Rendering),
            new MarkerDefinition(1, "PostLateUpdate.VFXUpdate", CPUTimeArea.Rendering, CPUTimeSubarea.RenderingVFX),
            new MarkerDefinition(1, "PostLateUpdate.FinishFrameRendering", CPUTimeArea.Rendering),
            new MarkerDefinition(1, "PostLateUpdate.UpdateAllRenderers", CPUTimeArea.Rendering),
            new MarkerDefinition(1, "PostLateUpdate.UpdateAllSkinnedMeshes", CPUTimeArea.Rendering),
            new MarkerDefinition(1, "PostLateUpdate.ParticleSystemEndUpdateAll", CPUTimeArea.Rendering),
            new MarkerDefinition(1, "ScriptableRuntimeReflectionSystemWrapper.Internal_ScriptableRuntimeReflectionSystemWrapper_TickRealtimeProbes()", CPUTimeArea.Rendering, CPUTimeSubarea.None, true),
            // TODO: Can be also a child marker
            new MarkerDefinition(1, "RenderPipelineManager.DoRenderLoop_Internal", CPUTimeArea.Rendering, CPUTimeSubarea.None, true),
            new MarkerDefinition(1, "DestroyCullResults", CPUTimeArea.Rendering),
            new MarkerDefinition(1, "RendererNotifyInvisible", CPUTimeArea.Rendering, CPUTimeSubarea.None, true),

            new MarkerDefinition(1, "EarlyUpdate.UpdateTextureStreamingManager", CPUTimeArea.Rendering, CPUTimeSubarea.RenderingStreaming, false),
            new MarkerDefinition(1, "PostLateUpdate.UpdateCustomRenderTextures", CPUTimeArea.Rendering),

            // Audio
            new MarkerDefinition(1, "FixedUpdate.AudioFixedUpdate", CPUTimeArea.Audio),
            new MarkerDefinition(1, "PostLateUpdate.UpdateAudio", CPUTimeArea.Audio),

            // UI
            new MarkerDefinition(1, "EarlyUpdate.UpdateCanvasRectTransform", CPUTimeArea.UI),
            new MarkerDefinition(1, "PostLateUpdate.PlayerUpdateCanvases", CPUTimeArea.UI),
            // TODO: UI specific or 2D/Sprite rendering?
            new MarkerDefinition(1, "PostLateUpdate.UpdateRectTransform", CPUTimeArea.UI),
            new MarkerDefinition(1, "PostLateUpdate.PlayerEmitCanvasGeometry", CPUTimeArea.UI),
            new MarkerDefinition(1, "GUI.Repaint", CPUTimeArea.UI),
            new MarkerDefinition(1, "UGUI.Rendering.RenderOverlays", CPUTimeArea.UI),
            new MarkerDefinition(1, "UGUI.Rendering.EmitWorldScreenspaceCameraGeometry", CPUTimeArea.UI),

            // Loading / Streaming
            new MarkerDefinition(1, "EarlyUpdate.UpdatePreloading", CPUTimeArea.LoadStream, CPUTimeSubarea.None, false,
                new MarkerDefinition[]
                {
                    new MarkerDefinition(2, "GC.Collect", CPUTimeArea.GC),
                }
            ),
            new MarkerDefinition(1, "EarlyUpdate.UpdateStreamingManager", CPUTimeArea.LoadStream),
            new MarkerDefinition(1, "Initialization.AsyncUploadTimeSlicedUpdate", CPUTimeArea.LoadStream),
            new MarkerDefinition(1, "EarlyUpdate.UpdatePreloading", CPUTimeArea.LoadStream),

            new MarkerDefinition(1, "EarlyUpdate.PlayerCleanupCachedData", CPUTimeArea.GC),

            // Waiting (for rendering, frame target rate, etc)
            // TODO: Needs another pass: some are potential child markers of other markers; markers that run between other CPU main thread markers could be categorized as "Rendering"?
            new MarkerDefinition(1, "WaitForLastPresent", CPUTimeArea.WaitMarkers, CPUTimeSubarea.None, true),
            new MarkerDefinition(1, "WaitForTargetFPS", CPUTimeArea.WaitMarkers, CPUTimeSubarea.None, true),
            new MarkerDefinition(1, "Gfx.PresentFrame", CPUTimeArea.WaitMarkers, CPUTimeSubarea.None, false),
            new MarkerDefinition(1, "Gfx.WaitForPresentOnGfxThread", CPUTimeArea.WaitMarkers, CPUTimeSubarea.None, false),
            new MarkerDefinition(1, "Gfx.WaitForRenderThread", CPUTimeArea.WaitMarkers, CPUTimeSubarea.None, false),
            new MarkerDefinition(1, "PostLateUpdate.PresentAfterDraw", CPUTimeArea.WaitMarkers),

            // Editor and Profiler markers
            new MarkerDefinition(1, "PostLateUpdate.ProfilerEndFrame", CPUTimeArea.EditorAndProfiler, CPUTimeSubarea.None, false),
            new MarkerDefinition(1, "PostLateUpdate.ProfilerSynchronizeStats", CPUTimeArea.EditorAndProfiler, CPUTimeSubarea.None, false),
            new MarkerDefinition(1, "PostLateUpdate.UpdateResolution", CPUTimeArea.EditorAndProfiler)
        };

        ProfileReport m_ProfileReport;
        public ref ProfileReport ProfileReport => ref m_ProfileReport;

        public void LoadProfile(string profilePath)
        {
            ProfilerDriver.LoadProfile(profilePath, false);
        }

        public ProfileReport CreateReport(ProfileAnalyzerParams profileAnalyzerParams)
        {
            m_ProfileReport = new ProfileReport();
            m_ProfileReport.Init();

            if (ProfilerDriver.firstFrameIndex >= 0)
            {
                CalculateFrameTimeStats(ref m_ProfileReport, profileAnalyzerParams);
                CollectMarkers(ref m_ProfileReport);
            }

            return m_ProfileReport;
        }

        static void CollectMarkers(ref ProfileReport report)
        {
            report.NumFrames = ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex + 1;
            report.FrameInfos = new FrameInfo[report.NumFrames];

            for (int frameIndex = 0; frameIndex < report.NumFrames; ++frameIndex)
            {
                var frameDataHierarchy = ProfilerDriver.GetHierarchyFrameDataView(frameIndex + ProfilerDriver.firstFrameIndex, 0,
                    HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnDontSort, false);

                CollectMarkersForFrame(ref report, frameDataHierarchy, frameIndex);

                report.FrameInfos[frameIndex].TotalTimeMs = frameDataHierarchy.frameTimeMs;

                var rootId = frameDataHierarchy.GetRootItemID();
                report.FrameInfos[frameIndex].TotalGcMemory = (int)frameDataHierarchy.GetItemColumnDataAsFloat(rootId, HierarchyFrameDataView.columnGcMemory);
            }
        }

        static void CollectMarkersForFrame(ref ProfileReport report,
            HierarchyFrameDataView frameDataHierarchy, int frameId)
        {
            List<int> topLevelItems = new List<int>();
            List<MarkerStats> allMarkerStats = new List<MarkerStats>();

            // PlayerLoop: Collect child markers of PlayerLoop
            FindHierarchyItemsIdByMarkerString(frameDataHierarchy, frameDataHierarchy.GetRootItemID(),
                "PlayerLoop", 0, false, topLevelItems);

            foreach (var topLevelItem in topLevelItems)
            {
                List<int> children = new List<int>();
                frameDataHierarchy.GetItemChildren(topLevelItem, children);

                foreach (var childItemId in children)
                {
                    var framePercent = frameDataHierarchy.GetItemColumnDataAsFloat(childItemId, HierarchyFrameDataView.columnTotalPercent);

                    if (framePercent > k_MinProfilerMarkerPercentage)
                    {
                        var id = frameDataHierarchy.GetItemMarkerID(childItemId);
                        var name = frameDataHierarchy.GetMarkerName(id);
                        var frameMs = frameDataHierarchy.GetItemColumnDataAsFloat(childItemId, HierarchyFrameDataView.columnTotalTime);
                        var frameGcMemory = frameDataHierarchy.GetItemColumnDataAsFloat(childItemId, HierarchyFrameDataView.columnGcMemory);
                        var nameHash = name.GetHashCode();

                        MarkerDefinition foundMarkerDefinition = new MarkerDefinition(-1, "none", CPUTimeArea.Animation);
                        bool hasFoundMarker = false;
                        foreach (var markerDef in MarkerDefinitions)
                        {
                            if (markerDef.NameIsSubString && name.Contains(markerDef.Name))
                            {
                                hasFoundMarker = true;
                                foundMarkerDefinition = markerDef;
                                break;
                            }

                            if (!markerDef.NameIsSubString && nameHash == markerDef.NameHash && markerDef.Name == name)
                            {
                                hasFoundMarker = true;
                                foundMarkerDefinition = markerDef;
                                break;
                            }
                        }

                        // Recursively traverse child items/markers
                        float subMarkerTotalTimeMs = 0;
                        float subMarkerTotalPercent = 0;
                        float subMarkerTotalGcMemory = 0;

                        if (hasFoundMarker && foundMarkerDefinition.SubMarkers != null && foundMarkerDefinition.SubMarkers.Length > 0)
                        {
                            CollectSubMarkers(frameDataHierarchy, foundMarkerDefinition, childItemId, allMarkerStats, ref subMarkerTotalPercent, ref subMarkerTotalGcMemory, ref subMarkerTotalTimeMs);
                        }

                        if (hasFoundMarker)
                        {
                            var markerStats = new MarkerStats
                            {
                                Definition = foundMarkerDefinition,
                                FrameTimeMs = frameMs,
                                FrameTimePercentage = framePercent,
                                FrameGcMemory = frameGcMemory,
                                SubMarkersTimeMs = subMarkerTotalTimeMs,
                                SubMarkersPercent = subMarkerTotalPercent,
                                SubMarkersGcMemory = subMarkerTotalGcMemory
                            };
                            allMarkerStats.Add(markerStats);
                        }
                        else
                        {
                            var unidentifiedDef = new MarkerDefinition
                            {
                                Name = name,
                                Area = CPUTimeArea.Uncategorized,
                                Level = 1
                            };

                            var markerStats = new MarkerStats
                            {
                                Definition = unidentifiedDef,
                                FrameTimeMs = frameMs,
                                FrameTimePercentage = framePercent,
                                FrameGcMemory = frameGcMemory
                            };
                            allMarkerStats.Add(markerStats);
                        }
                    }
                }
            }

            // EditorLoop / Profiler: Collect markers we can ignore for standalone/release
            topLevelItems.Clear();
            FindHierarchyItemsIdByMarkerString(frameDataHierarchy, frameDataHierarchy.GetRootItemID(),
                "EditorLoop", 0, false, topLevelItems);

            CollectEditorProfilerMarkers(frameDataHierarchy, topLevelItems, ref allMarkerStats);

            topLevelItems.Clear();
            FindHierarchyItemsIdByMarkerString(frameDataHierarchy, frameDataHierarchy.GetRootItemID(),
                "Profiler.", 0, true, topLevelItems);

            CollectEditorProfilerMarkers(frameDataHierarchy, topLevelItems, ref allMarkerStats);

            report.FrameInfos[frameId].MarkerStats = allMarkerStats.ToArray();
        }

        private static void CollectSubMarkers(HierarchyFrameDataView frameDataHierarchy,
            MarkerDefinition parentMarkerDefinition, int childItemId, List<MarkerStats> allMarkerStats,
            ref float subMarkerTotalPercent, ref float subMarkerTotalGcMemory, ref float subMarkerTotalTimeMs)
        {
            foreach (var markerDef in parentMarkerDefinition.SubMarkers)
            {
                List<int> childItems = new List<int>();
                FindHierarchyItemsIdByMarkerString(frameDataHierarchy, childItemId, markerDef.Name, 0,
                    markerDef.NameIsSubString, childItems);

                if (childItems.Count > 0)
                {
                    foreach (var itemID in childItems)
                    {
                        var markerFrameMs = frameDataHierarchy.GetItemColumnDataAsFloat(itemID,
                            HierarchyFrameDataView.columnTotalTime);
                        var markerFramePercent = frameDataHierarchy.GetItemColumnDataAsFloat(itemID,
                            HierarchyFrameDataView.columnTotalPercent);
                        var markerFrameGcMemory = frameDataHierarchy.GetItemColumnDataAsFloat(itemID,
                            HierarchyFrameDataView.columnGcMemory);

                        var finalMarkerDef = markerDef;
                        if (markerDef.NameIsSubString)
                        {
                            var markerID = frameDataHierarchy.GetItemMarkerID(itemID);
                            finalMarkerDef.Name = frameDataHierarchy.GetMarkerName(markerID);
                        }

                        float subMarkerPercent = 0;
                        float subMarkerGcMemory = 0;
                        float subMarkerTimeMs = 0;

                        if (markerDef.SubMarkers != null && markerDef.SubMarkers.Length > 0)
                        {
                            CollectSubMarkers(frameDataHierarchy, markerDef, itemID, allMarkerStats, ref subMarkerPercent, ref subMarkerGcMemory, ref subMarkerTimeMs);

                            markerFrameMs += subMarkerTimeMs;
                            markerFramePercent += subMarkerPercent;
                            markerFrameGcMemory += subMarkerGcMemory;
                        }

                        subMarkerTotalTimeMs += markerFrameMs;
                        subMarkerTotalPercent += markerFramePercent;
                        subMarkerTotalGcMemory += markerFrameGcMemory;

                        var markerStats = new MarkerStats
                        {
                            Definition = finalMarkerDef,
                            FrameTimeMs = markerFrameMs,
                            FrameTimePercentage = markerFramePercent,
                            FrameGcMemory = markerFrameGcMemory,
                            SubMarkersPercent = subMarkerPercent,
                            SubMarkersTimeMs = subMarkerTimeMs,
                            SubMarkersGcMemory = subMarkerGcMemory
                        };

                        allMarkerStats.Add(markerStats);
                    }
                }
            }
        }

        static void CollectEditorProfilerMarkers(HierarchyFrameDataView frameDataHierarchy, List<int> topLevelItems,
            ref List<MarkerStats> markerStats)
        {
            foreach (var topLevelItem in topLevelItems)
            {
                var frameMs = frameDataHierarchy.GetItemColumnDataAsFloat(topLevelItem, HierarchyFrameDataView.columnTotalTime);
                var framePercent =
                    frameDataHierarchy.GetItemColumnDataAsFloat(topLevelItem, HierarchyFrameDataView.columnTotalPercent);

                var markerId = frameDataHierarchy.GetItemMarkerID(topLevelItem);
                var markerName = frameDataHierarchy.GetMarkerName(markerId);

                var markerDef = new MarkerDefinition
                {
                    Name = markerName,
                    Area = CPUTimeArea.EditorAndProfiler,
                    Level = 1
                };

                var markerStat = new MarkerStats
                {
                    Definition = markerDef,
                    FrameTimeMs = frameMs,
                    FrameTimePercentage = framePercent
                };

                markerStats.Add(markerStat);
            }
        }

        static void FindHierarchyItemsIdByMarkerString(HierarchyFrameDataView frameDataHierarchy, int itemId,
            string markerStringToFind, int markerStringToFindHash, bool markerStringIsSubString, List<int> foundItems)
        {
            var markerId = frameDataHierarchy.GetItemMarkerID(itemId);
            var markerName = frameDataHierarchy.GetMarkerName(markerId);

            if (markerStringIsSubString && markerName.Contains(markerStringToFind))
            {
                foundItems.Add(itemId);
                return;
            }

            if (markerStringToFindHash == 0)
                markerStringToFindHash = markerStringToFind.GetHashCode();

            var markerNameHash = markerName.GetHashCode();

            if (markerNameHash == markerStringToFindHash && markerName == markerStringToFind)
            {
                foundItems.Add(itemId);
                return;
            }

            List<int> children = new List<int>();
            frameDataHierarchy.GetItemChildren(itemId, children);

            foreach (var childItem in children)
            {
                FindHierarchyItemsIdByMarkerString(frameDataHierarchy, childItem, markerStringToFind, markerStringToFindHash, markerStringIsSubString, foundItems);
            }
        }

        static void CalculateFrameTimeStats(ref ProfileReport report, ProfileAnalyzerParams profileAnalyzerParams)
        {
            report.NumFrames = ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex + 1;

            // Determine slow and fast frame thresholds for a given target framerate
            float SlowFrameTime = 35.37f;
            float FastFrameTime = 25f;
            var TargetFrameRate = profileAnalyzerParams.TargetFramerate;

            switch (TargetFrameRate)
            {
                case TargetFramerate.FPS60:
                    SlowFrameTime = 18.54f;
                    FastFrameTime = 12.5f;
                    break;
                case TargetFramerate.FPS120:
                    SlowFrameTime = 10.12f;
                    FastFrameTime = 6.25f;
                    break;
            }

            report.SlowFrameTime = SlowFrameTime;
            report.FastFrameTime = FastFrameTime;

            // Get all frame timings
            float[] frameTimesMs = new float[report.NumFrames];
            for (int i = 0; i < report.NumFrames; ++i)
            {
                var rawView = ProfilerDriver.GetRawFrameDataView(i + ProfilerDriver.firstFrameIndex, 0);
                frameTimesMs[i] = rawView.frameTimeMs;
            }

            Array.Sort(frameTimesMs);

            // Cut off at given percentage of frames to calculate slow/fast frame times only for N-th percentile of fastest frames
            float percentile = profileAnalyzerParams.FastFramePercentile > 0f ? profileAnalyzerParams.FastFramePercentile : 0.9f;
            int percentileCutoffIndex = Math.Max(0, ((int)(report.NumFrames * percentile)) - 1);
            float percentileCutoffMs = frameTimesMs[percentileCutoffIndex];

            float maxPercentileFrameTime = 0;
            int percentileFrameCount = 0;
            for (int i = ProfilerDriver.firstFrameIndex; i <= ProfilerDriver.lastFrameIndex; ++i)
            {
                var rawView = ProfilerDriver.GetRawFrameDataView(i, 0);

                var fps = rawView.frameFps;
                var ms = rawView.frameTimeMs;

                if (ms > percentileCutoffMs)
                    continue;
                if (ms <= 0)
                    continue;

                maxPercentileFrameTime = Math.Max(maxPercentileFrameTime, ms);
                percentileFrameCount++;

                if (ms > SlowFrameTime)
                    report.NumSlowFrameTimeFrames++;
                else if (ms < FastFrameTime)
                    report.NumFastFrameTimeFrames++;

                if (ms < report.HighFrameMs)
                {
                    report.HighFrameIdx = i;
                    report.HighFPS = fps;
                    report.HighFrameMs = ms;
                }

                if (ms > report.LowFrameMs)
                {
                    report.LowFrameIdx = i;
                    report.LowFPS = fps;
                    report.LowFrameMs = ms;
                }
            }

            report.MaxPercentileFrameTime = maxPercentileFrameTime;
            report.PercentileFrameCount = percentileFrameCount;
        }
    }
}
