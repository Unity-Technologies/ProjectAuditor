using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProjectReport
    {
        [SerializeField] private List<ProjectIssue> m_Issues = new List<ProjectIssue>();

        public int NumTotalIssues
        {
            get
            {
                return m_Issues.Count;
                
            }
        }
        
        public int GetNumIssues(IssueCategory category)
        {
            return m_Issues.Count(i => i.category == category);  
        }
        
        public IEnumerable<ProjectIssue> GetIssues(IssueCategory category)
        {
            return m_Issues.Where(i => i.category == category);  
        }

        public void AddIssue(ProjectIssue projectIssue)
        {
            m_Issues.Add(projectIssue);
        }
        
        public void Export()
        {
            for (int i = 0; i < (int) IssueCategory.NumCategories; i++)
            {
                var category = (IssueCategory) i;
                var issues = GetIssues(category).ToArray();

                StreamWriter writer = new StreamWriter("ProjectAuditor_Report_" + category.ToString() + ".csv");
                writer.WriteLine("Area,Type,Method,CallingMethod,Problem,Recommendation,Path,Line");

                foreach (var issue in issues)
                {
                    writer.WriteLine(issue.descriptor.area + "," +
                                     issue.descriptor.type + "," +
                                     issue.descriptor.method + "," +
                                     issue.name + "," +
                                     issue.descriptor.problem.Replace(",", "") + "," +
                                     issue.descriptor.solution.Replace(",", "") + "," +
                                     issue.relativePath + "," +
                                     issue.line);
                }

                writer.Flush();
                writer.Close();
            }
        }
    }
}