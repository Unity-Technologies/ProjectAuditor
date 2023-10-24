using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AnimatorControllerProperty
    {
        NumLayers,
        NumParameters,
        NumClips,
        SizeOnDisk,
        Num
    }

    enum AnimationClipProperty
    {
        IsEmpty,
        NumEvents,
        Framerate,
        Length,
        WrapMode,
        IsLooping,
        HasGenericRootTransform,
        HasMotionCurves,
        HasMotionFloatCurves,
        HasRootCurves,
        HumanMotion,
        IsLegacy,
        SizeOnDisk,
        Num
    }

    enum AvatarProperty
    {
        IsValid,
        IsHuman,
#if PA_CAN_USE_AVATAR_HUMAN_DESCRIPTION
        NumHumanBones,
        NumSkeletonBones,
        UpperArmTwist,
        LowerArmTwist,
        UpperLegTwist,
        LowerLegTwist,
        ArmStretch,
        LegStretch,
        FeetSpacing,
        HasTranslationDoF,
#endif
        SizeOnDisk,
        Num
    }

    enum AvatarMaskProperty
    {
        NumTransforms,
        SizeOnDisk,
        Num
    }

    class AnimationModule : ProjectAuditorModuleWithAnalyzers<AnimationAnalyzer>
    {
        static readonly IssueLayout k_AnimatorControllerLayout = new IssueLayout
        {
            category = IssueCategory.AnimatorController,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Controller Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumLayers), format = PropertyFormat.Integer, name = "Layers", longName = "Number of Layers" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumParameters), format = PropertyFormat.Integer, name = "Params", longName = "Number of Parameters" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumClips), format = PropertyFormat.Integer, name = "Clips", longName = "Number of Animation Clips" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.SizeOnDisk), format = PropertyFormat.Bytes, name = "Size", longName = "Controller Size" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path" }
            }
        };

        static readonly IssueLayout k_AnimationClipLayout = new IssueLayout
        {
            category = IssueCategory.AnimationClip,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Clip Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsEmpty), format = PropertyFormat.Bool, name = "Empty?", longName = "Contains no curves and no events" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.NumEvents), format = PropertyFormat.Integer, name = "Events", longName = "Number of Events" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.Framerate), format = PropertyFormat.String, name = "Frame Rate" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.Length), format = PropertyFormat.String, name = "Length" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.WrapMode), format = PropertyFormat.String, name = "Wrap Mode" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsLooping), format = PropertyFormat.Bool, name = "Looping?" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasGenericRootTransform), format = PropertyFormat.Bool, name = "Generic Root Transform?", longName = "Has animation on the root transform" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasMotionCurves), format = PropertyFormat.Bool, name = "Motion Curves?", longName = "Has root motion curves" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasMotionFloatCurves), format = PropertyFormat.Bool, name = "Motion Float Curves?", longName = "Has editor curves for its root motion" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasRootCurves), format = PropertyFormat.Bool, name = "Root Curves?" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HumanMotion), format = PropertyFormat.Bool, name = "Human Motion?", longName = "Contains curves that drive a humanoid rig" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsLegacy), format = PropertyFormat.Bool, name = "Legacy?", longName = "Is this clip used with a Legacy Animation component?" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.SizeOnDisk), format = PropertyFormat.Bytes, name = "Size", longName = "Clip Size" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path" }
            }
        };

        static readonly IssueLayout k_AvatarLayout = new IssueLayout
        {
            category = IssueCategory.Avatar,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Avatar Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.IsValid), format = PropertyFormat.Bool, name = "Valid?" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.IsHuman), format = PropertyFormat.Bool, name = "Human?" },
#if PA_CAN_USE_AVATAR_HUMAN_DESCRIPTION
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.NumHumanBones), format = PropertyFormat.Integer, name = "Human Bones", longName = "Number of bones mappings" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.NumSkeletonBones), format = PropertyFormat.Integer, name = "Skeleton Bones", longName = "Number of bone transforms to include" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.UpperArmTwist), format = PropertyFormat.String, name = "Upper Arm Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.LowerArmTwist), format = PropertyFormat.String, name = "Lower Arm Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.UpperLegTwist), format = PropertyFormat.String, name = "Upper Leg Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.LowerLegTwist), format = PropertyFormat.String, name = "Lower Leg Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.ArmStretch), format = PropertyFormat.String, name = "Arm Stretch" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.LegStretch), format = PropertyFormat.String, name = "Leg Stretch" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.FeetSpacing), format = PropertyFormat.String, name = "Feet Spacing" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.HasTranslationDoF), format = PropertyFormat.Bool, name = "Translation DoF?" },
#endif
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.SizeOnDisk), format = PropertyFormat.Bytes, name = "Size", longName = "Avatar Size" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path" }
            }
        };

        static readonly IssueLayout k_AvatarMaskLayout = new IssueLayout
        {
            category = IssueCategory.AvatarMask,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Mask Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarMaskProperty.NumTransforms), format = PropertyFormat.Integer, name = "Transforms", longName = "Number of Transforms" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarMaskProperty.SizeOnDisk), format = PropertyFormat.Bytes, name = "Size", longName = "Mask Size" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path" }
            }
        };

        public override string name => "Animation";

        public override bool isEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_AnimatorControllerLayout,
            k_AnimationClipLayout,
            k_AvatarLayout,
            k_AvatarMaskLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            ProcessAnimatorControllers(projectAuditorParams.OnIncomingIssues, progress);
            ProcessAnimationClips(projectAuditorParams.OnIncomingIssues, progress);
            ProcessAvatars(projectAuditorParams.OnIncomingIssues, progress);
            ProcessAvatarMasks(projectAuditorParams.OnIncomingIssues, progress);

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }

        void ProcessAnimatorControllers(Action<IEnumerable<ProjectIssue>> onIncomingIssues, IProgress progress)
        {
            var issues = new List<ProjectIssue>();

            var assetPaths = GetAssetPathsByFilter("t:animatorcontroller, a:assets");
            progress?.Start("Finding Animator Controllers", "Search in Progress...", assetPaths.Length);
            foreach (var assetPath in assetPaths)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
                if (controller == null)
                {
                    Debug.LogError(assetPath + " is not an Animator Controller.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(controller);

                issues.Add(ProjectIssue.CreateWithoutDiagnostic(k_AnimatorControllerLayout.category, controller.name)
                    .WithCustomProperties(new object[(int)AnimatorControllerProperty.Num]
                    {
                        controller.layers.Length,
                        controller.parameters.Length,
                        controller.animationClips.Length,
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                onIncomingIssues(issues);

            progress?.Clear();
        }

        void ProcessAnimationClips(Action<IEnumerable<ProjectIssue>> onIncomingIssues, IProgress progress)
        {
            var issues = new List<ProjectIssue>();
            var assetPaths = GetAssetPathsByFilter("t:animationclip, a:assets");

            progress?.Start("Finding Animation Clips", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip == null)
                {
                    Debug.LogError(assetPath + " is not an Animation Clip.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(clip);

                issues.Add(ProjectIssue.CreateWithoutDiagnostic(k_AnimationClipLayout.category, clip.name)
                    .WithCustomProperties(new object[(int)AnimationClipProperty.Num]
                    {
                        clip.empty,
                        clip.events.Length,
                        Formatting.FormatFramerate(clip.frameRate),
                        Formatting.FormatLengthInSeconds(clip.length),
                        clip.wrapMode,
                        clip.isLooping,
                        clip.hasGenericRootTransform,
                        clip.hasMotionCurves,
                        clip.hasMotionFloatCurves,
                        clip.hasRootCurves,
                        clip.humanMotion,
                        clip.legacy,
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                onIncomingIssues(issues);

            progress?.Clear();
        }

        void ProcessAvatars(Action<IEnumerable<ProjectIssue>> onIncomingIssues, IProgress progress)
        {
            var issues = new List<ProjectIssue>();
            var assetPaths = GetAssetPathsByFilter("t:avatar, a:assets");

            progress?.Start("Finding Avatars", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
                if (avatar == null)
                {
                    Debug.LogError(assetPath + " is not an Avatar.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(avatar);

                issues.Add(ProjectIssue.CreateWithoutDiagnostic(k_AvatarLayout.category, avatar.name)
                    .WithCustomProperties(new object[(int)AvatarProperty.Num]
                    {
                        avatar.isValid,
                        avatar.isHuman,
#if PA_CAN_USE_AVATAR_HUMAN_DESCRIPTION
                        avatar.humanDescription.human.Length,
                        avatar.humanDescription.skeleton.Length,
                        avatar.humanDescription.upperArmTwist,
                        avatar.humanDescription.lowerArmTwist,
                        avatar.humanDescription.upperLegTwist,
                        avatar.humanDescription.lowerLegTwist,
                        avatar.humanDescription.armStretch,
                        avatar.humanDescription.legStretch,
                        avatar.humanDescription.feetSpacing,
                        avatar.humanDescription.hasTranslationDoF,
#endif
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                onIncomingIssues(issues);

            progress?.Clear();
        }

        void ProcessAvatarMasks(Action<IEnumerable<ProjectIssue>> onIncomingIssues, IProgress progress)
        {
            var issues = new List<ProjectIssue>();
            var assetPaths = GetAssetPathsByFilter("t:avatarmask, a:assets");

            progress?.Start("Finding Avatar Masks", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(assetPath);
                if (mask == null)
                {
                    Debug.LogError(assetPath + " is not an Avatar Mask.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(mask);

                issues.Add(ProjectIssue.CreateWithoutDiagnostic(k_AvatarMaskLayout.category, mask.name)
                    .WithCustomProperties(new object[(int)AvatarMaskProperty.Num]
                    {
                        mask.transformCount,
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                onIncomingIssues(issues);

            progress?.Clear();
        }
    }
}
