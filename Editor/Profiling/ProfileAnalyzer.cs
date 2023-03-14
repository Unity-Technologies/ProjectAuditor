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

    public class MarkerStats
    {
        public MarkerDefinition Definition;

        public float FrameTimeMs;
        public float FrameTimePercentage;
        public float FrameGcMemory;

        public float SubMarkersTimeMs;
        public float SubMarkersPercent;
        public float SubMarkersGcMemory;

        public MarkerStats Parent;
    }

    public struct FrameInfo
    {
        public float TotalTimeMs;
        public int TotalGcMemory;

        public MarkerStats[] MarkerStats;
    }

    public class ProfileReport
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
        public float PercentileCutoffFrameTime;

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

                // Skip frames that don't have captured markers
                if (inFrameInfo.MarkerStats != null)
                {
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
                    new MarkerDefinition(2, "Cinemachine", CPUTimeArea.Cinemachine, CPUTimeSubarea.None, true),

                    new MarkerDefinition(2, ".Update()", CPUTimeArea.Behaviour, CPUTimeSubarea.None, true,
                        new MarkerDefinition[]
                        {
                            new MarkerDefinition(3, "Physics.RaycastAll", CPUTimeArea.Behaviour),
                            new MarkerDefinition(3, "Instantiate", CPUTimeArea.Behaviour),
                            new MarkerDefinition(3, "GameObject.AddComponent", CPUTimeArea.Behaviour),
                        }
                    ),
                    new MarkerDefinition(2, "GC.Collect", CPUTimeArea.GC),
                }
            ),
            new MarkerDefinition(1, "PreLateUpdate.ScriptRunBehaviourLateUpdate", CPUTimeArea.Behaviour, CPUTimeSubarea.None, false,
                new MarkerDefinition[]
                {
                    new MarkerDefinition(2, "EventMachine`2.LateUpdate()", CPUTimeArea.VisualScripting, CPUTimeSubarea.None, true),
                    new MarkerDefinition(2, "CinemachineBrain.LateUpdate()", CPUTimeArea.Cinemachine),
                    new MarkerDefinition(2, "VolumetricFog.LateUpdate()", CPUTimeArea.Rendering),
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
            new MarkerDefinition(1, "Gfx.PresentFrame", CPUTimeArea.WaitMarkers),
            new MarkerDefinition(1, "Gfx.WaitForPresentOnGfxThread", CPUTimeArea.WaitMarkers),
            new MarkerDefinition(1, "Gfx.WaitForRenderThread", CPUTimeArea.WaitMarkers),
            new MarkerDefinition(1, "PostLateUpdate.PresentAfterDraw", CPUTimeArea.WaitMarkers),

            // Editor and Profiler markers
            new MarkerDefinition(1, "PostLateUpdate.ProfilerEndFrame", CPUTimeArea.EditorAndProfiler),
            new MarkerDefinition(1, "PostLateUpdate.ProfilerSynchronizeStats", CPUTimeArea.EditorAndProfiler),
            new MarkerDefinition(1, "PostLateUpdate.UpdateResolution", CPUTimeArea.EditorAndProfiler)
        };

        ProfileReport m_ProfileReport;
        public ref ProfileReport ProfileReport => ref m_ProfileReport;

        public void LoadProfile(string profilePath)
        {
            ProfilerDriver.LoadProfile(profilePath, false);
        }

        /// <summary>
        /// CreateReport implicitly creates a summary for all frames currently stored in the ProfilerDriver.
        /// </summary>
        public ProfileReport CreateReport(ProfileAnalyzerParams profileAnalyzerParams)
        {
            m_ProfileReport = new ProfileReport();
            m_ProfileReport.Init();

            if (ProfilerDriver.firstFrameIndex >= 0)
            {
                m_ProfileReport.NumFrames = ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex + 1;

                CollectMarkers(ref m_ProfileReport);
                CalculateFrameTimeStats(ref m_ProfileReport, profileAnalyzerParams);
            }

            return m_ProfileReport;
        }

        /// <summary>
        /// InitializeReport sets up an "empty" report to then add analyzed frames as needed.
        /// This allows to add any frames, e.g. some that only contain some data (frame times) and some
        /// that got extracted from ProfilerDriver data with marker stats.
        /// </summary>
        public ProfileReport InitializeReport(int frameCount)
        {
            m_ProfileReport = new ProfileReport();
            m_ProfileReport.Init();

            m_ProfileReport.FrameInfos = new FrameInfo[frameCount];
            m_ProfileReport.NumFrames = frameCount;

            return m_ProfileReport;
        }

        public void CollectMarkersForFrames(int frameInfoStartFrame)
        {
            var numFrames = ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex + 1;

            for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
            {
                var frameDataHierarchy = ProfilerDriver.GetHierarchyFrameDataView(frameIndex + ProfilerDriver.firstFrameIndex, 0,
                    HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnDontSort, false);

                CollectMarkersForFrame(ref m_ProfileReport, frameDataHierarchy, frameIndex + frameInfoStartFrame);

                m_ProfileReport.FrameInfos[frameIndex +  frameInfoStartFrame].TotalTimeMs = frameDataHierarchy.frameTimeMs;

                var rootId = frameDataHierarchy.GetRootItemID();
                m_ProfileReport.FrameInfos[frameIndex + frameInfoStartFrame].TotalGcMemory = (int)frameDataHierarchy.GetItemColumnDataAsFloat(rootId, HierarchyFrameDataView.columnGcMemory);
            }
        }

        public void AddFrameCounter(int frameInfoFrameIndex, float timeMs)
        {
            m_ProfileReport.FrameInfos[frameInfoFrameIndex].TotalTimeMs = timeMs;
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
            HierarchyFrameDataView frameDataHierarchy, int frameInfosFrameId)
        {
            List<int> topLevelItems = new List<int>();
            List<MarkerStats> allMarkerStats = new List<MarkerStats>();

            // PlayerLoop: Collect child markers of PlayerLoop
            FindHierarchyItemsIdByMarkerString(frameDataHierarchy, frameDataHierarchy.GetRootItemID(),
                "PlayerLoop", 0, false, topLevelItems, 1);

            foreach (var topLevelItem in topLevelItems)
            {
                List<int> children = new List<int>();
                frameDataHierarchy.GetItemChildren(topLevelItem, children);

                // Iterate over children of PlayerLoop
                // Note: Those are mostly top-level animation/physics/script updates and various other built-in methods
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

                        MarkerStats markerStats = null;
                        if (hasFoundMarker)
                        {
                            markerStats = new MarkerStats
                            {
                                Definition = foundMarkerDefinition,
                                FrameTimeMs = frameMs,
                                FrameTimePercentage = framePercent,
                                FrameGcMemory = frameGcMemory
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

                            markerStats = new MarkerStats
                            {
                                Definition = unidentifiedDef,
                                FrameTimeMs = frameMs,
                                FrameTimePercentage = framePercent,
                                FrameGcMemory = frameGcMemory
                            };
                            allMarkerStats.Add(markerStats);
                        }

                        // Recursively traverse child items/markers
                        float subMarkerTotalTimeMs = 0;
                        float subMarkerTotalPercent = 0;
                        float subMarkerTotalGcMemory = 0;

                        // Found a known marker we want to dig deeper into?
                        // Note: Child nodes/markers here are mostly built-in and MonoBehaviour updates
                        if (hasFoundMarker && foundMarkerDefinition.SubMarkers != null && foundMarkerDefinition.SubMarkers.Length > 0)
                        {
                            CollectKnownSubMarkers(frameDataHierarchy, foundMarkerDefinition, childItemId, allMarkerStats,
                                ref subMarkerTotalPercent, ref subMarkerTotalGcMemory, ref subMarkerTotalTimeMs,
                                markerStats
                            );
                        }

                        markerStats.SubMarkersTimeMs = subMarkerTotalTimeMs;
                        markerStats.SubMarkersPercent = subMarkerTotalPercent;
                        markerStats.SubMarkersGcMemory = subMarkerTotalGcMemory;
                    }
                }
            }

            // EditorLoop / Profiler: Collect markers we can ignore for standalone/release
            topLevelItems.Clear();
            FindHierarchyItemsIdByMarkerString(frameDataHierarchy, frameDataHierarchy.GetRootItemID(),
                "EditorLoop", 0, false, topLevelItems, 1);

            CollectEditorProfilerMarkers(frameDataHierarchy, topLevelItems, ref allMarkerStats);

            topLevelItems.Clear();
            FindHierarchyItemsIdByMarkerString(frameDataHierarchy, frameDataHierarchy.GetRootItemID(),
                "Profiler.", 0, true, topLevelItems, 1);

            CollectEditorProfilerMarkers(frameDataHierarchy, topLevelItems, ref allMarkerStats);

            report.FrameInfos[frameInfosFrameId].MarkerStats = allMarkerStats.ToArray();
        }

        private static bool CollectKnownSubMarkers(HierarchyFrameDataView frameDataHierarchy,
            MarkerDefinition parentMarkerDefinition, int itemId, List<MarkerStats> allMarkerStats,
            ref float subMarkerTotalPercent, ref float subMarkerTotalGcMemory, ref float subMarkerTotalTimeMs,
            MarkerStats parentStats)
        {
            bool bFoundAnyChildren = false;

            // TODO: For all marker NOT found, search deeper from each child that was a non-marker

            HashSet<int> traversedChildren = new HashSet<int>();
            List<MarkerDefinition> missingMarkerDefs = new List<MarkerDefinition>();

            foreach (var markerDef in parentMarkerDefinition.SubMarkers)
            {
                List<int> childItems = new List<int>();
                FindHierarchyItemsIdByMarkerString(frameDataHierarchy, itemId, markerDef.Name, 0,
                    markerDef.NameIsSubString, childItems, 1);

                // Some children had this marker
                if (childItems.Count > 0)
                {
                    foreach (var childItem in childItems)
                    {
                        traversedChildren.Add(childItem);

                        var markerFrameMs = frameDataHierarchy.GetItemColumnDataAsFloat(childItem,
                            HierarchyFrameDataView.columnTotalTime);
                        var markerFramePercent = frameDataHierarchy.GetItemColumnDataAsFloat(childItem,
                            HierarchyFrameDataView.columnTotalPercent);
                        var markerFrameGcMemory = frameDataHierarchy.GetItemColumnDataAsFloat(childItem,
                            HierarchyFrameDataView.columnGcMemory);

                        subMarkerTotalTimeMs += markerFrameMs;
                        subMarkerTotalPercent += markerFramePercent;
                        subMarkerTotalGcMemory += markerFrameGcMemory;

                        var finalMarkerDef = markerDef;
                        if (markerDef.NameIsSubString)
                        {
                            var markerID = frameDataHierarchy.GetItemMarkerID(childItem);
                            finalMarkerDef.Name = frameDataHierarchy.GetMarkerName(markerID);
                        }

                        var markerStats = new MarkerStats
                        {
                            Definition = finalMarkerDef,
                            FrameTimeMs = markerFrameMs,
                            FrameTimePercentage = markerFramePercent,
                            FrameGcMemory = markerFrameGcMemory,
                            // Link this marker to its parent so we can generate a callstack
                            Parent = parentStats
                        };

                        float subMarkerPercent = 0;
                        float subMarkerGcMemory = 0;
                        float subMarkerTimeMs = 0;

                        if (markerDef.SubMarkers != null && markerDef.SubMarkers.Length > 0)
                        {
                            CollectKnownSubMarkers(frameDataHierarchy, markerDef, childItem, allMarkerStats, ref subMarkerPercent, ref subMarkerGcMemory, ref subMarkerTimeMs, markerStats);
                        }

                        markerStats.SubMarkersPercent = subMarkerPercent;
                        markerStats.SubMarkersTimeMs = subMarkerTimeMs;
                        markerStats.SubMarkersGcMemory = subMarkerGcMemory;

                        // Keep this marker since it is a match of a known marker, a marker we're interested in
                        allMarkerStats.Add(markerStats);

                        bFoundAnyChildren = true;
                    }
                }
                else
                {
                    missingMarkerDefs.Add(markerDef);
                }
            }

            foreach (var markerDef in missingMarkerDefs)
            {
                List<int> allChildren = new List<int>();
                frameDataHierarchy.GetItemChildren(itemId, allChildren);

                foreach (var childItem in allChildren)
                {
                    if (traversedChildren.Contains(childItem))
                        continue;

                    var markerFrameMs = frameDataHierarchy.GetItemColumnDataAsFloat(childItem,
                        HierarchyFrameDataView.columnTotalTime);
                    var markerFramePercent = frameDataHierarchy.GetItemColumnDataAsFloat(childItem,
                        HierarchyFrameDataView.columnTotalPercent);
                    var markerFrameGcMemory = frameDataHierarchy.GetItemColumnDataAsFloat(childItem,
                        HierarchyFrameDataView.columnGcMemory);

                    var markerID = frameDataHierarchy.GetItemMarkerID(childItem);
                    var name = frameDataHierarchy.GetMarkerName(markerID);

                    var unidentifiedDef = new MarkerDefinition
                    {
                        Name = name,
                        Area = parentStats.Definition.Area,
                        Level = 1,
                        SubMarkers = new MarkerDefinition[] { markerDef }
                    };

                    var markerStats = new MarkerStats
                    {
                        Definition = unidentifiedDef,
                        FrameTimeMs = markerFrameMs,
                        FrameTimePercentage = markerFramePercent,
                        FrameGcMemory = markerFrameGcMemory
                    };

                    float subMarkerPercent = 0;
                    float subMarkerGcMemory = 0;
                    float subMarkerTimeMs = 0;

                    bool bFoundChildren = CollectKnownSubMarkers(frameDataHierarchy, unidentifiedDef, childItem, allMarkerStats, ref subMarkerPercent, ref subMarkerGcMemory, ref subMarkerTimeMs, markerStats);
                    bFoundAnyChildren |= bFoundChildren;

                    if (bFoundChildren)
                    {
                        markerStats.SubMarkersPercent = subMarkerPercent;
                        markerStats.SubMarkersTimeMs = subMarkerTimeMs;
                        markerStats.SubMarkersGcMemory = subMarkerGcMemory;

                        subMarkerTotalTimeMs += markerFrameMs;
                        subMarkerTotalPercent += markerFramePercent;
                        subMarkerTotalGcMemory += markerFrameGcMemory;

                        // Link this marker to its parent so we can generate a callstack
                        markerStats.Parent = parentStats;

                        // We keep this marker since it has children that match known markers, markers we're interested in
                        allMarkerStats.Add(markerStats);

                        // Mark this child as fully explored
                        traversedChildren.Add(childItem);
                    }
                }
            }

            return bFoundAnyChildren;
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
            string markerStringToFind, int markerStringToFindHash, bool markerStringIsSubString, List<int> foundItems,
            int recursionLevels)
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

            if (recursionLevels > 0)
            {
                List<int> children = new List<int>();
                frameDataHierarchy.GetItemChildren(itemId, children);

                foreach (var childItem in children)
                {
                    FindHierarchyItemsIdByMarkerString(frameDataHierarchy, childItem, markerStringToFind,
                        markerStringToFindHash, markerStringIsSubString, foundItems, recursionLevels - 1);
                }
            }
        }

        public void CalculateFrameTimeStats(ref ProfileReport report, ProfileAnalyzerParams profileAnalyzerParams)
        {
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
                frameTimesMs[i] = report.FrameInfos[i].TotalTimeMs;
            }

            Array.Sort(frameTimesMs);

            // Cut off at given percentage of frames to calculate slow/fast frame times only for N-th percentile of fastest frames
            float percentile = profileAnalyzerParams.FastFramePercentile > 0f ? profileAnalyzerParams.FastFramePercentile : 0.9f;
            int percentileCutoffIndex = Math.Max(0, ((int)(report.NumFrames * percentile)) - 1);
            float percentileCutoffMs = frameTimesMs[percentileCutoffIndex];

            float maxPercentileFrameTime = 0;
            int percentileFrameCount = 0;
            for (int i = 0; i < report.NumFrames; ++i)
            {
                var ms = report.FrameInfos[i].TotalTimeMs;
                var fps = ms > 0 ? 1000f / ms : 0;

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

            report.PercentileCutoffFrameTime = percentileCutoffMs;
        }
    }
}
