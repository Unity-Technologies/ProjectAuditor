using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AnimatorControllerProperty
    {
        NumLayers,
        NumParameters,
        NumClips,
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
        Num
    }

    enum AvatarMaskProperty
    {
        NumTransforms,
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
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumLayers), format = PropertyFormat.Integer, name = "Num Layers", longName = "Number of Layers" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumParameters), format = PropertyFormat.Integer, name = "Num Params", longName = "Number of Parameters" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumClips), format = PropertyFormat.Integer, name = "Num Clips", longName = "Number of Animation Clips" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path" }
            }
        };

        static readonly IssueLayout k_AnimationClipLayout = new IssueLayout
        {
            category = IssueCategory.AnimationClip,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Clip Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsEmpty), format = PropertyFormat.Bool, name = "Is Empty", longName = "Contains no curves and no events" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.NumEvents), format = PropertyFormat.Integer, name = "Num Events", longName = "Number of Events" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.Framerate), format = PropertyFormat.String, name = "Frame Rate" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.Length), format = PropertyFormat.String, name = "Length" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.WrapMode), format = PropertyFormat.String, name = "Wrap Mode" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsLooping), format = PropertyFormat.Bool, name = "Is Looping" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasGenericRootTransform), format = PropertyFormat.Bool, name = "Has Generic Root Transform", longName = "Has animation on the root transform" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasMotionCurves), format = PropertyFormat.Bool, name = "Has Motion Curves", longName = "Has root motion curves" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasMotionFloatCurves), format = PropertyFormat.Bool, name = "Has Motion Float Curves", longName = "Has editor curves for its root motion" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasRootCurves), format = PropertyFormat.Bool, name = "Has Root Curves" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HumanMotion), format = PropertyFormat.Bool, name = "Is Human Motion", longName = "Contains curves that drive a humanoid rig" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsLegacy), format = PropertyFormat.Bool, name = "Is Legacy", longName = "Is this clip used with a Legacy Animation component?" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path" }
            }
        };

        static readonly IssueLayout k_AvatarLayout = new IssueLayout
        {
            category = IssueCategory.Avatar,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Avatar Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.IsValid), format = PropertyFormat.Bool, name = "Is Valid" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.IsHuman), format = PropertyFormat.Bool, name = "Is Human" },
#if PA_CAN_USE_AVATAR_HUMAN_DESCRIPTION
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.NumHumanBones), format = PropertyFormat.Integer, name = "Num Human Bones", longName = "Number of bones mappings" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.NumSkeletonBones), format = PropertyFormat.Integer, name = "Num Skeleton Bones", longName = "Number of bone transforms to include" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.UpperArmTwist), format = PropertyFormat.String, name = "Upper Arm Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.LowerArmTwist), format = PropertyFormat.String, name = "Lower Arm Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.UpperLegTwist), format = PropertyFormat.String, name = "Upper Leg Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.LowerLegTwist), format = PropertyFormat.String, name = "Lower Leg Twist" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.ArmStretch), format = PropertyFormat.String, name = "Arm Stretch" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.LegStretch), format = PropertyFormat.String, name = "Leg Stretch" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.FeetSpacing), format = PropertyFormat.String, name = "Feet Spacing" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarProperty.HasTranslationDoF), format = PropertyFormat.Bool, name = "Has Translation DoF" },
#endif
                new PropertyDefinition { type = PropertyType.Path, name = "Path" }
            }
        };

        static readonly IssueLayout k_AvatarMaskLayout = new IssueLayout
        {
            category = IssueCategory.AvatarMask,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Mask Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AvatarMaskProperty.NumTransforms), format = PropertyFormat.Integer, name = "Num Transforms", longName = "Number of Transforms" },
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
            ProcessAnimatorControllers(projectAuditorParams.onIncomingIssues, progress);
            ProcessAnimationClips(projectAuditorParams.onIncomingIssues, progress);
            ProcessAvatars(projectAuditorParams.onIncomingIssues, progress);
            ProcessAvatarMasks(projectAuditorParams.onIncomingIssues, progress);

            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void ProcessAnimatorControllers(Action<IEnumerable<ProjectIssue>> onIncomingIssues, IProgress progress)
        {
            var issues = new List<ProjectIssue>();

            var allControllerGuids = AssetDatabase.FindAssets("t:animatorcontroller, a:assets");
            progress?.Start("Finding Animator Controllers", "Search in Progress...", allControllerGuids.Length);
            foreach (var guid in allControllerGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
                if (controller == null)
                {
                    Debug.LogError(assetPath + " is not an Animator Controller.");

                    continue;
                }

                issues.Add(ProjectIssue.Create(k_AnimatorControllerLayout.category, controller.name)
                    .WithCustomProperties(new object[(int)AnimatorControllerProperty.Num]
                    {
                        controller.layers.Length,
                        controller.parameters.Length,
                        controller.animationClips.Length
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

            var allClipGuids = AssetDatabase.FindAssets("t:animationclip, a:assets");
            progress?.Start("Finding Animation Clips", "Search in Progress...", allClipGuids.Length);
            foreach (var guid in allClipGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip == null)
                {
                    Debug.LogError(assetPath + " is not an Animation Clip.");

                    continue;
                }

                issues.Add(ProjectIssue.Create(k_AnimationClipLayout.category, clip.name)
                    .WithCustomProperties(new object[(int)AnimationClipProperty.Num]
                    {
                        clip.empty,
                        clip.events.Length,
                        clip.frameRate,
                        clip.length,
                        clip.wrapMode,
                        clip.isLooping,
                        clip.hasGenericRootTransform,
                        clip.hasMotionCurves,
                        clip.hasMotionFloatCurves,
                        clip.hasRootCurves,
                        clip.humanMotion,
                        clip.legacy
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

            var allAvatarGuids = AssetDatabase.FindAssets("t:avatar, a:assets");
            progress?.Start("Finding Avatars", "Search in Progress...", allAvatarGuids.Length);
            foreach (var guid in allAvatarGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
                if (avatar == null)
                {
                    Debug.LogError(assetPath + " is not an Avatar.");

                    continue;
                }

                issues.Add(ProjectIssue.Create(k_AvatarLayout.category, avatar.name)
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
                        avatar.humanDescription.hasTranslationDoF
#endif
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

            var allMaskGuids = AssetDatabase.FindAssets("t:avatarmask, a:assets");
            progress?.Start("Finding Avatar Masks", "Search in Progress...", allMaskGuids.Length);
            foreach (var guid in allMaskGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(assetPath);
                if (mask == null)
                {
                    Debug.LogError(assetPath + " is not an Avatar Mask.");

                    continue;
                }

                issues.Add(ProjectIssue.Create(k_AvatarMaskLayout.category, mask.name)
                    .WithCustomProperties(new object[(int)AvatarMaskProperty.Num]
                    {
                        mask.transformCount
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
