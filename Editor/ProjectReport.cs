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
            return m_Issues.Where(i => i.category == category).Count();  
        }
        
        public IEnumerable<ProjectIssue> GetIssues(IssueCategory category)
        {
            return m_Issues.Where(i => i.category == category);  
        }

        public void AddIssue(ProjectIssue projectIssue)
        {
            m_Issues.Add(projectIssue);
        }
        
        //TODO: change to export csv
        public void WriteToFile()
        {
            for(int i=0; i<(int)IssueCategory.NumCategories; i++)
            {
                var category = (IssueCategory) i;
                var json = JsonHelper.ToJson<ProjectIssue>(GetIssues(category).ToArray(), true);
                File.WriteAllText("Report_" + category.ToString() + ".json", json);
            }
        }
    }
}