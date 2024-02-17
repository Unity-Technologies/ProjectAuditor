using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class ReportSerializationTests : TestFixtureBase
    {
        const string k_ReportPath = "report.json";

        [Test]
        public void ReportSerialization_Report_CanSaveAndLoad()
        {
            Build();

            var report = m_ProjectAuditor.Audit();

            report.Save(k_ReportPath);

            var loadedReport = Report.Load(k_ReportPath);

            Assert.AreEqual(report.Version, loadedReport.Version);
            Assert.AreEqual(report.SessionInfo.ProjectAuditorVersion, loadedReport.SessionInfo.ProjectAuditorVersion);
            Assert.AreEqual(report.SessionInfo.UnityVersion, loadedReport.SessionInfo.UnityVersion);

            Assert.AreEqual(report.SessionInfo.CompanyName, loadedReport.SessionInfo.CompanyName);
            Assert.AreEqual(report.SessionInfo.ProjectId, loadedReport.SessionInfo.ProjectId);
            Assert.AreEqual(report.SessionInfo.ProjectName, loadedReport.SessionInfo.ProjectName);
            Assert.AreEqual(report.SessionInfo.ProjectRevision, loadedReport.SessionInfo.ProjectRevision);

            Assert.AreEqual(report.SessionInfo.DateTime, loadedReport.SessionInfo.DateTime);

            Assert.AreEqual(report.SessionInfo.Platform, loadedReport.SessionInfo.Platform);

            Assert.AreEqual(report.NumTotalIssues, loadedReport.NumTotalIssues);

            Assert.IsTrue(report.IsValid());
            Assert.IsTrue(report.HasCategory(IssueCategory.Code));
            Assert.IsTrue(report.HasCategory(IssueCategory.ProjectSetting));

            Assert.AreEqual(report.GetNumIssues(IssueCategory.Code), loadedReport.GetNumIssues(IssueCategory.Code));
            Assert.AreEqual(report.GetNumIssues(IssueCategory.ProjectSetting), loadedReport.GetNumIssues(IssueCategory.ProjectSetting));
        }

        void AssertForbiddenProperty(JObject jobject, string propertyName)
        {
            Assert.IsFalse(jobject.ContainsKey(propertyName),
                $"Property '{propertyName}' is found. JObject: {jobject}");
        }

        void AssertRequiredProperty(JObject jobject, string propertyName)
        {
            Assert.IsTrue(jobject.ContainsKey(propertyName),
                $"Property '{propertyName}' is not found. JObject: {jobject}");
        }

        void AssertRequiredPropertyIsValid(JObject jobject, string propertyName)
        {
            AssertRequiredProperty(jobject, propertyName);

            Assert.IsNotNull(jobject[propertyName].Value<string>(), $"Property '{propertyName}' cannot be null. JObject: {jobject}");
            Assert.IsFalse(jobject[propertyName].Value<string>().Equals(""), $"Property '{propertyName}' cannot be empty. JObject: {jobject}");
        }

        void AssertRequiredArrayIsValid(JObject jobject, string propertyName)
        {
            Assert.IsTrue(jobject.ContainsKey(propertyName), $"Property '{propertyName}' is not found");

            var values = jobject[propertyName].Values<string>().ToArray();
            Assert.IsNotNull(values, $"Property '{propertyName}' cannot be null. JObject: {jobject}");
            Assert.Positive(values.Length, $"Property '{propertyName}' cannot be empty. JObject: {jobject}");
            Assert.IsFalse(values.Any(p => string.IsNullOrEmpty(p)), $"Property '{propertyName}' elements cannot be empty. JObject: {jobject}");
        }

        void AssertOptionalPropertyIsValid(JObject jobject, string propertyName)
        {
            if (!jobject.ContainsKey(propertyName))
                return;

            Assert.IsTrue(!jobject[propertyName].Value<string>().Equals(""), $"Property '{propertyName}' should not be an empty string. JObject: {jobject}");
        }

        void AssertOptionalArrayIsValid(JObject jobject, string propertyName)
        {
            if (!jobject.ContainsKey(propertyName))
                return;

            var values = jobject[propertyName].Values<string>().ToArray();
            Assert.IsNotNull(values, $"Property '{propertyName}' cannot be null. JObject: {jobject}");
            Assert.Positive(values.Length, $"Property '{propertyName}' cannot be empty. JObject: {jobject}");
            Assert.IsFalse(values.Any(p => string.IsNullOrEmpty(p)), $"Property '{propertyName}' elements cannot be empty. JObject: {jobject}");
        }

        [Test]
        public void ReportSerialization_Report_CanSerialize()
        {
            var report = m_ProjectAuditor.Audit(new AnalysisParams
            {
                Categories = new[]
                {
                    IssueCategory.Code,
                    IssueCategory.ProjectSetting,
                    IssueCategory.AssetIssue,
                }
            });

            report.Save(k_ReportPath);

            var serializedReport = File.ReadAllText(k_ReportPath);

            var reportDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedReport);

            // Check if top-level keys exist.
            Assert.IsTrue(reportDict.ContainsKey("version"));
            Assert.IsTrue(reportDict.ContainsKey("moduleMetadata"));
            Assert.IsTrue(reportDict.ContainsKey("issues"));
            Assert.IsTrue(reportDict.ContainsKey("descriptors"));

            if (reportDict["insights"] is JArray insights)
            {
                foreach (var property in insights)
                {
                    if (property is JObject insight)
                    {
                        AssertRequiredPropertyIsValid(insight, "category");

                        AssertRequiredPropertyIsValid(insight, "description");
                        AssertOptionalArrayIsValid(insight, "properties");

                        AssertForbiddenProperty(insight, "descriptorId");
                        AssertForbiddenProperty(insight, "severity");
                        AssertRequiredProperty(insight, "location");
                        AssertRequiredPropertyIsValid(insight["location"] as JObject, "path");
                    }
                    else
                    {
                        Assert.Fail($"{property} is null or not a JObject");
                    }
                }
            }
            else
            {
                Assert.Fail("'insights' is null or not a JArray");
            }

            if (reportDict["issues"] is JArray issues)
            {
                foreach (var property in issues)
                {
                    if (property is JObject issue)
                    {
                        AssertRequiredPropertyIsValid(issue, "category");
                        AssertRequiredPropertyIsValid(issue, "description");
                        AssertRequiredProperty(issue, "descriptorId");
                        AssertOptionalArrayIsValid(issue, "properties");

                        AssertRequiredProperty(issue, "location");
                        AssertRequiredPropertyIsValid(issue["location"] as JObject, "path");
                        AssertRequiredPropertyIsValid(issue, "severity");
                    }
                    else
                    {
                        Assert.Fail($"{property} is null or not a JObject");
                    }
                }
            }
            else
            {
                Assert.Fail("'issues' is null or not a JArray");
            }

            if (reportDict["descriptors"] is JArray descriptors)
            {
                foreach (var property in descriptors)
                {
                    if (property is JObject descriptor)
                    {
                        Assert.IsTrue(descriptor.ContainsKey("id"));

                        AssertForbiddenProperty(descriptor, "defaultSeverity");
                        AssertRequiredArrayIsValid(descriptor, "areas");
                        AssertForbiddenProperty(descriptor, "messageFormat");
                        AssertForbiddenProperty(descriptor, "type");
                        AssertForbiddenProperty(descriptor, "method");
                        AssertForbiddenProperty(descriptor, "value");
                        if (descriptor.ContainsKey("platforms"))
                        {
                            // if present, check sanity
                            AssertRequiredArrayIsValid(descriptor, "platforms");
                        }
                        AssertOptionalPropertyIsValid(descriptor, "documentationUrl");
                        AssertForbiddenProperty(descriptor, "minimumVersion");
                        AssertForbiddenProperty(descriptor, "maximumVersion");
                    }
                    else
                    {
                        Assert.Fail($"{property} is null or not a JObject");
                    }
                }
            }
            else
            {
                Assert.Fail("'descriptors' is null or not a JArray");
            }
        }
    }
}
