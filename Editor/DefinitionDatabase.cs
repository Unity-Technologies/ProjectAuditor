using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
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

    public class DefinitionDatabase
    {
        public List<ProblemDefinition> m_Definitions;

        public DefinitionDatabase(string name)
        {
            m_Definitions = new List<ProblemDefinition>();

            var fullPath = Path.GetFullPath(Path.Combine(ProjectAuditor.dataPath, name + ".json"));
            var json = File.ReadAllText(fullPath);
            var result = JsonHelper.FromJson<ProblemDefinition>(json);

            m_Definitions.AddRange(result);            
        }
    }
}