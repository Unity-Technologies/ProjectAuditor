using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public class MetaDataAuditor : IAuditor
    {
        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor
            (
            700000,
            "Meta Data",
            Area.BuildSize
            );

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.MetaData,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Meta Data"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.String, name = "Value"}
            }
        };

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return k_Descriptor;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
        }

        public bool IsSupported()
        {
            return true;
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            throw new NotImplementedException();
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            onIssueFound(new ProjectIssue(k_Descriptor, "Date and Time", IssueCategory.MetaData,
                new[] {DateTime.Now.ToString()}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Host Name", IssueCategory.MetaData,
                new[] {SystemInfo.deviceName}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Host Platform", IssueCategory.MetaData,
                new[] {SystemInfo.operatingSystem}));

            onIssueFound(new ProjectIssue(k_Descriptor, "Company Name", IssueCategory.MetaData,
                new[] {Application.companyName}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Product Name", IssueCategory.MetaData,
                new[] {Application.productName}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Build Target", IssueCategory.MetaData,
                new[] {EditorUserBuildSettings.activeBuildTarget.ToString()}));
#if UNITY_2019_3_OR_NEWER
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.project-auditor/Editor/Unity.ProjectAuditor.Editor.asmdef");
            onIssueFound(new ProjectIssue(k_Descriptor, "Project Auditor Version", IssueCategory.MetaData,
                new[] {packageInfo.version}));
#endif
            onIssueFound(new ProjectIssue(k_Descriptor, "Unity Version", IssueCategory.MetaData,
                new[] {Application.unityVersion}));

            onComplete();
        }
    }
}
