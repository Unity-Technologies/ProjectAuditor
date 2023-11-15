using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Analyzer.Util
{
    public class IdProvider<Key>
    {
        private Dictionary<Key, int> m_Ids;
        private Dictionary<int, Key> m_IdToKey = null;

        public IdProvider(bool bidirectional = false, IEqualityComparer<Key> comparer = null)
        {
            m_Ids = new Dictionary<Key, int>(comparer ?? EqualityComparer<Key>.Default);

            if (bidirectional)
            {
                m_IdToKey = new Dictionary<int, Key>();
            }
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

            if (m_IdToKey != null)
            {
                m_IdToKey.Add(id, key);
            }

            return id;
        }

        public Key GetKey(int id)
        {
            if (m_IdToKey == null)
            {
                throw new InvalidOperationException("Not a bidirectional IdProvider");
            }

            return m_IdToKey[id];
        }
    }

    public class ObjectIdProvider : IdProvider<(int fileId, long pathId)>
    {
        public ObjectIdProvider(bool bidirectional = false) : base(bidirectional) {}
    }
}
