using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ProjectReportTest {

		[Test]
		public void UninitializedTestPasses()
		{
			var uninitialised = new ProjectReport();
			Assert.AreEqual(0, uninitialised.NumIssues);
			Assert.AreEqual(0, uninitialised.GetIssues(IssueCategory.ApiCalls));
			Assert.AreEqual(0, uninitialised.GetIssues(IssueCategory.ProjectSettings));
		}

		[Test]
		public void AddIssueTestPasses()
		{
			var projectReport = new ProjectReport();
			
			projectReport.AddIssue(new ProjectIssue
			{
				category = IssueCategory.ApiCalls								
			});
			
			Assert.AreEqual(1, projectReport.NumIssues);
			Assert.AreEqual(1, projectReport.GetIssues(IssueCategory.ApiCalls).Count);
			Assert.AreEqual(0, projectReport.GetIssues(IssueCategory.ProjectSettings).Count);
		}

	}	
}

