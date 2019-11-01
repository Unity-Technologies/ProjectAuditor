using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unity.ProjectAuditor.Editor
{
    public static class ProblemDescriptorHelper
    {
        public static List<ProblemDescriptor> LoadProblemDescriptors(string path, string name)
        {
            var fullPath = Path.GetFullPath(Path.Combine(path, name + ".json"));
            var json = File.ReadAllText(fullPath);
            var result = JsonHelper.FromJson<ProblemDescriptor>(json);

//            int id = 1000;
//            foreach (var d in result)
//            {
//                if (d.id == 0)
//                {
//                    d.id = id++;
//                }
//            }
//
//            if (id > 1000)
//            {
//                json = JsonHelper.ToJson(result, true);
//                File.WriteAllText(fullPath, json);               
//            }
            
            return new List<ProblemDescriptor>(result);
        }
    }
}