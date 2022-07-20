using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum MetaDataProperty
    {
        Value = 0,
        Num
    }

    class MetaDataModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.MetaData,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Meta Data"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(MetaDataProperty.Value), format = PropertyFormat.String, name = "Value"}
            }
        };

        internal const string k_KeyDateAndTime = "Date and Time";
        internal const string k_KeyHostName = "Host Name";
        internal const string k_KeyHostPlatform = "Host Platform";
        internal const string k_KeyCompanyName = "Company Name";
        internal const string k_KeyProductName = "Product Name";
        internal const string k_KeyAnalysisTarget = "Analysis Target";
        internal const string k_KeyCompilationMode = "Compilation Mode";
        internal const string k_KeyProjectAuditorVersion = "Project Auditor Version";
        internal const string k_KeyUnityVersion = "Unity Version";

        ProjectAuditorConfig m_Config;

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Config = config;
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            NewMetaData(k_KeyDateAndTime, DateTime.Now, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyHostName, SystemInfo.deviceName, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyHostPlatform, SystemInfo.operatingSystem, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyCompanyName, Application.companyName, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyProductName, Application.productName, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyAnalysisTarget, projectAuditorParams.platform, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyCompilationMode, m_Config.CompilationMode, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyProjectAuditorVersion, ProjectAuditor.PackageVersion, projectAuditorParams.onIssueFound);
            NewMetaData(k_KeyUnityVersion, Application.unityVersion, projectAuditorParams.onIssueFound);

            if (projectAuditorParams.onComplete != null)
                projectAuditorParams.onComplete();
        }

        void NewMetaData(string key, object value, Action<ProjectIssue> onIssueFound)
        {
            var issue = ProjectIssue.Create(IssueCategory.MetaData, key)
                .WithCustomProperties(new object[(int)MetaDataProperty.Num] { value });
            onIssueFound(issue);
        }
    }
}
