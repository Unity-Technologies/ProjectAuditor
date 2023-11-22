using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using UnityEditor.Experimental.GraphView;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class BuildObjects
    {
        public class Reference
        {
            public int ObjectId { get; }
            public string Property { get; }

            // Shared strings for property names to avoid creating a lot of duplicate strings in memory.
            static HashSet<string> s_SharedStrings = new HashSet<string>();

            public Reference(int objectId, string property)
            {
                ObjectId = objectId;

                if (s_SharedStrings.TryGetValue(property, out var sharedString))
                {
                    Property = sharedString;
                }

                s_SharedStrings.Add(property);
                Property = property;
            }
        }

        private class ReferenceComparer : EqualityComparer<Reference>
        {
            public override bool Equals(Reference a, Reference b)
            {
                // We can use ReferenceEquals because all identical strings are shared.
                return a.ObjectId == b.ObjectId && ReferenceEquals(a.Property, b.Property);
            }

            public override int GetHashCode(Reference obj)
            {
                return HashCode.Combine(obj.ObjectId, obj.Property);
            }
        }

        // Objects by id.
        Dictionary<int, SerializedObject> m_SerializedObjects = new Dictionary<int, SerializedObject>();
        public IReadOnlyCollection<SerializedObject> SerializedObjects => m_SerializedObjects.Values;

        Dictionary<int, HashSet<Reference>> m_ReferencesTo = new Dictionary<int, HashSet<Reference>>();
        Dictionary<int, HashSet<Reference>> m_ReferencesFrom = new Dictionary<int, HashSet<Reference>>();

        public void AddObject(SerializedObject obj)
        {
            m_SerializedObjects.Add(obj.Id, obj);
        }

        public void AddReference(int fromObjectId, int toObjectId, string property)
        {
            if (!m_ReferencesFrom.TryGetValue(fromObjectId, out var references))
            {
                references = new HashSet<Reference>(new ReferenceComparer());
                m_ReferencesFrom[fromObjectId] = references;
            }

            references.Add(new Reference(toObjectId, property));

            if (!m_ReferencesTo.TryGetValue(toObjectId, out references))
            {
                references = new HashSet<Reference>(new ReferenceComparer());
                m_ReferencesTo[toObjectId] = references;
            }

            references.Add(new Reference(fromObjectId, property));
        }

        // This is costly and allocates a new list, cache the result.
        public List<T> GetObjects<T>() where T : SerializedObject
        {
            var objects = new List<T>();

            foreach (var obj in m_SerializedObjects.Values)
            {
                var typedObj = obj as T;
                if (typedObj != null)
                    objects.Add(typedObj);
            }

            return objects;
        }
    }
}
