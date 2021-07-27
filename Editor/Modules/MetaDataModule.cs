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

    class MetaDataModule : ProjectAuditorModule
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

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return k_Descriptor;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
        {
            onIssueFound(new ProjectIssue(k_Descriptor, "Date and Time", IssueCategory.MetaData,
                new object[] {DateTime.Now}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Host Name", IssueCategory.MetaData,
                new object[] {SystemInfo.deviceName}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Host Platform", IssueCategory.MetaData,
                new object[] {SystemInfo.operatingSystem}));

            onIssueFound(new ProjectIssue(k_Descriptor, "Company Name", IssueCategory.MetaData,
                new object[] {Application.companyName}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Product Name", IssueCategory.MetaData,
                new object[] {Application.productName}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Build Target", IssueCategory.MetaData,
                new object[] {EditorUserBuildSettings.activeBuildTarget.ToString()}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Project Auditor Version", IssueCategory.MetaData,
                new object[] { ProjectAuditor.PackageVersion}));
            onIssueFound(new ProjectIssue(k_Descriptor, "Unity Version", IssueCategory.MetaData,
                new object[] {Application.unityVersion}));

            if (onComplete != null)
                onComplete();
        }
    }
}
