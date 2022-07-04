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
            NewMetaData("Date and Time", DateTime.Now, projectAuditorParams.onIssueFound);
            NewMetaData("Host Name", SystemInfo.deviceName, projectAuditorParams.onIssueFound);
            NewMetaData("Host Platform", SystemInfo.operatingSystem, projectAuditorParams.onIssueFound);
            NewMetaData("Company Name", Application.companyName, projectAuditorParams.onIssueFound);
            NewMetaData("Product Name", Application.productName, projectAuditorParams.onIssueFound);
            NewMetaData("Active Target", EditorUserBuildSettings.activeBuildTarget, projectAuditorParams.onIssueFound);
            NewMetaData("Analysis Target", projectAuditorParams.platform, projectAuditorParams.onIssueFound);
            NewMetaData("Compilation Mode", m_Config.CompilationMode, projectAuditorParams.onIssueFound);
            NewMetaData("Project Auditor Version", ProjectAuditor.PackageVersion, projectAuditorParams.onIssueFound);
            NewMetaData("Unity Version", Application.unityVersion, projectAuditorParams.onIssueFound);

            if (projectAuditorParams.onComplete != null)
                projectAuditorParams.onComplete();
        }

        void NewMetaData(string key, object value, Action<ProjectIssue> onIssueFound)
        {
            onIssueFound(new ProjectIssue(key, IssueCategory.MetaData, new object[(int)MetaDataProperty.Num] {value}));
        }
    }
}
