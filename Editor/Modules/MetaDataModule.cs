using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return null;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Config = config;
        }

        public override Task<IReadOnlyCollection<ProjectIssue>> AuditAsync(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var issues = new List<ProjectIssue>();
            NewMetaData("Date and Time", DateTime.Now, issues.Add);
            NewMetaData("Host Name", SystemInfo.deviceName, issues.Add);
            NewMetaData("Host Platform", SystemInfo.operatingSystem, issues.Add);
            NewMetaData("Company Name", Application.companyName, issues.Add);
            NewMetaData("Product Name", Application.productName, issues.Add);
            NewMetaData("Active Target", EditorUserBuildSettings.activeBuildTarget, issues.Add);
            NewMetaData("Analysis Target", projectAuditorParams.platform, issues.Add);
            NewMetaData("Compilation Mode", m_Config.CompilationMode, issues.Add);
            NewMetaData("Project Auditor Version", ProjectAuditor.PackageVersion, issues.Add);
            NewMetaData("Unity Version", Application.unityVersion, issues.Add);

            return Task.FromResult<IReadOnlyCollection<ProjectIssue>>(issues.AsReadOnly());
        }

        void NewMetaData(string key, object value, Action<ProjectIssue> onIssueFound)
        {
            onIssueFound(new ProjectIssue(key, IssueCategory.MetaData, new object[(int)MetaDataProperty.Num] {value}));
        }
    }
}
