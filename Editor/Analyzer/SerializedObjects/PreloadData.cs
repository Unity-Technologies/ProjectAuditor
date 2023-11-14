using System.Collections.Generic;
using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class PreloadData
    {
        private List<PPtr> m_Assets;
        public IReadOnlyList<PPtr> Assets => m_Assets;

        public PreloadData(RandomAccessReader reader)
        {
            var m_Assets = new List<PPtr>(reader["m_Assets"].GetArraySize());

            foreach (var pptr in reader["m_Assets"])
            {
                m_Assets.Add(new PPtr(pptr));
            }
        }
    }
}
