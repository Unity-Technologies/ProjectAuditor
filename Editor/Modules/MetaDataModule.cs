using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public enum MetaDataProperty
    {
        Value = 0,
        Num
    }

    class MetaDataModule : IProjectAuditorModule
    {
        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor
            (
            700000,
            "Meta Data"
            );

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.MetaData,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Meta Data"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(MetaDataProperty.Value), format = PropertyFormat.String, name = "Value"}
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

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
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
            onIssueFound(new ProjectIssue(k_Descriptor, "Project Auditor Version", IssueCategory.MetaData,
                new[] { ProjectAuditor.PackageVersion}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Unity Version", IssueCategory.MetaData,
                new[] {Application.unityVersion}));

            if (onComplete != null)
                onComplete();
        }
    }
}
