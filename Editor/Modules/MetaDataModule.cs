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
            NewMetaData("Date and Time", DateTime.Now, onIssueFound);
            NewMetaData("Host Name", SystemInfo.deviceName, onIssueFound);
            NewMetaData("Host Platform", SystemInfo.operatingSystem, onIssueFound);
            NewMetaData("Company Name", Application.companyName, onIssueFound);
            NewMetaData("Product Name", Application.productName, onIssueFound);
            NewMetaData("Build Target", EditorUserBuildSettings.activeBuildTarget, onIssueFound);
            NewMetaData("Project Auditor Version", ProjectAuditor.PackageVersion, onIssueFound);
            NewMetaData("Unity Version", Application.unityVersion, onIssueFound);

            if (onComplete != null)
                onComplete();
        }

        void NewMetaData(string key, object value, Action<ProjectIssue> onIssueFound)
        {
            onIssueFound(new ProjectIssue(k_Descriptor, key, IssueCategory.MetaData, new object[(int)MetaDataProperty.Num] {value}));
        }
    }
}
