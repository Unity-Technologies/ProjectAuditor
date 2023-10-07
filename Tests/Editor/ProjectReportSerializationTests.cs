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
    class ProjectReportSerializationTests : TestFixtureBase
    {
        const string k_ReportPath = "report.json";

        [Test]
        public void ProjectReportSerialization_Report_CanSaveAndLoad()
        {
            var report = m_ProjectAuditor.Audit();

            report.Save(k_ReportPath);

            var loadedReport = ProjectReport.Load(k_ReportPath);

            Assert.AreEqual(report.Version, loadedReport.Version);
            Assert.AreEqual(report.NumTotalIssues, loadedReport.NumTotalIssues);
            Assert.IsTrue(report.IsValid());
            Assert.IsTrue(report.HasCategory(IssueCategory.Code));
            Assert.IsTrue(report.HasCategory(IssueCategory.ProjectSetting));
        }

        void AssertRequiredPropertyIsValid(JObject jobject, string propertyName)
        {
            Assert.IsTrue(jobject.ContainsKey(propertyName), $"Property '{propertyName}' is not found. JObject: {jobject}");
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
        public void ProjectReportSerialization_Report_CanSerialize()
        {
            var report = m_ProjectAuditor.Audit();
            report.Save(k_ReportPath);

            var serializedReport = File.ReadAllText(k_ReportPath);

            var reportDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedReport);

            // Check if top-level keys exist.
            Assert.IsTrue(reportDict.ContainsKey("version"));
            Assert.IsTrue(reportDict.ContainsKey("moduleMetadata"));
            Assert.IsTrue(reportDict.ContainsKey("issues"));
            Assert.IsTrue(reportDict.ContainsKey("descriptors"));

            if (reportDict["issues"] is JArray issues)
            {
                foreach (var property in issues)
                {
                    if (property is JObject issue)
                    {
                        AssertRequiredPropertyIsValid(issue, "category");
                        AssertRequiredPropertyIsValid(issue, "description");
                        AssertOptionalArrayIsValid(issue, "properties");

                        if (issue.ContainsKey("diagnosticID"))
                        {
                            AssertRequiredPropertyIsValid(issue, "severity");
                        }
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

                        AssertRequiredPropertyIsValid(descriptor, "defaultSeverity");
                        AssertRequiredArrayIsValid(descriptor, "areas");
                        AssertOptionalPropertyIsValid(descriptor, "messageFormat");
                        AssertOptionalPropertyIsValid(descriptor, "type");
                        AssertOptionalPropertyIsValid(descriptor, "method");
                        AssertOptionalPropertyIsValid(descriptor, "value");
                        AssertOptionalArrayIsValid(descriptor, "platforms");
                        AssertOptionalPropertyIsValid(descriptor, "documentationUrl");
                        AssertOptionalPropertyIsValid(descriptor, "minimumVersion");
                        AssertOptionalPropertyIsValid(descriptor, "maximumVersion");
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
