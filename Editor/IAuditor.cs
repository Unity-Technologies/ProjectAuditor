using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public interface IAuditor
    {
        void LoadDatabase(string path);
        void Audit( ProjectReport projectReport);
    }
}