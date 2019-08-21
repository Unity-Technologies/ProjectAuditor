using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace Editor
{

    [Serializable]
    public class ProblemDefinition {
        public string type;
        public string method;
        public string value;
        public string customevaluator;
        public string area;
        public string problem;
        public string solution;
    }

    [Serializable]
    public class ProjectIssue
    {
        public ProblemDefinition def;
        public string category;
        public string url;
        public int line;
        public int column;
        public bool resolved;

        public string location
        {
            get
            {                
                return string.IsNullOrEmpty(url) ? String.Empty : $"{url}({line},{column})";
            }
        }
    }

    public delegate void IssueFound(ProjectIssue projectIssue);
    
    public class DefinitionDatabase
    {
        public List<ProblemDefinition> m_Definitions;

        public DefinitionDatabase(string name)
        {
            m_Definitions = new List<ProblemDefinition>();

            var fullPath = Path.GetFullPath($"Packages/com.unity.project-auditor/Data/{name}.json");
            var json = File.ReadAllText(fullPath);
            var result = JsonHelper.FromJson<ProblemDefinition>(json);

            m_Definitions.AddRange(result);            
        }
    }
}