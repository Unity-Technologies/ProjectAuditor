using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.BuildData.Util
{
    public class IdProvider<Key>
    {
        private Dictionary<Key, int> m_Ids;

        public IdProvider(IEqualityComparer<Key> comparer = null)
        {
            m_Ids = new Dictionary<Key, int>(comparer ?? EqualityComparer<Key>.Default);
        }

        public int GetId(Key key)
        {
            int id;

            if (m_Ids.TryGetValue(key, out id))
            {
                return id;
            }

            id = m_Ids.Count;
            m_Ids.Add(key, id);

            return id;
        }
    }

    public class ObjectIdProvider : IdProvider<(int fileId, long pathId)>
    {
    }
}
