using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum MetaDataProperty
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
        internal const string k_KeyRoslynAnalysis = "Roslyn Analysis";
        internal const string k_KeyProjectAuditorVersion = "Project Auditor Version";
        internal const string k_KeyUnityVersion = "Unity Version";

        public override string name => "MetaData";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_IssueLayout
        };

        public override void Initialize(ProjectAuditorConfig config)
        {
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var issues = new List<ProjectIssue>();
            NewMetaData(k_KeyDateAndTime, Formatting.FormatDateTime(DateTime.Now), issues);
            NewMetaData(k_KeyHostName, SystemInfo.deviceName, issues);
            NewMetaData(k_KeyHostPlatform, SystemInfo.operatingSystem, issues);
            NewMetaData(k_KeyCompanyName, Application.companyName, issues);
            NewMetaData(k_KeyProductName, Application.productName, issues);
            NewMetaData(k_KeyAnalysisTarget, projectAuditorParams.platform, issues);
            NewMetaData(k_KeyCompilationMode, projectAuditorParams.compilationMode, issues);
            NewMetaData(k_KeyRoslynAnalysis, UserPreferences.useRoslynAnalyzers, issues);
            NewMetaData(k_KeyProjectAuditorVersion, ProjectAuditor.s_PackageVersion, issues);
            NewMetaData(k_KeyUnityVersion, Application.unityVersion, issues);

            projectAuditorParams.onIncomingIssues(issues);
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void NewMetaData(string key, object value, IList<ProjectIssue> issues)
        {
            var issue = ProjectIssue.CreateWithoutDiagnostic(IssueCategory.MetaData, key)
                .WithCustomProperties(new object[(int)MetaDataProperty.Num] { value });
            issues.Add(issue);
        }
    }
}
