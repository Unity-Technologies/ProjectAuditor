using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class BuildObjects
    {
        public class Reference
        {
            public int ObjectId { get; }
            public string Property { get; }

            // Shared strings for property names to avoid creating a lot of duplicate strings in memory.
            static Dictionary<string, string> s_SharedStrings = new Dictionary<string, string>();

            public Reference(int objectId, string property)
            {
                ObjectId = objectId;

                if (s_SharedStrings.TryGetValue(property, out var sharedString))
                {
                    Property = sharedString;
                }
                else
                {
                    s_SharedStrings.Add(property, property);
                    Property = property;
                }
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
                return obj.ObjectId ^ obj.Property.GetHashCode();
            }
        }

        // Objects by id.
        Dictionary<int, SerializedObject> m_SerializedObjects = new Dictionary<int, SerializedObject>();
        public IReadOnlyCollection<SerializedObject> SerializedObjects => m_SerializedObjects.Values;

        public Dictionary<int, HashSet<Reference>> ReferencesTo = new Dictionary<int, HashSet<Reference>>();
        public Dictionary<int, HashSet<Reference>> ReferencesFrom = new Dictionary<int, HashSet<Reference>>();

        public void AddObject(SerializedObject obj)
        {
            m_SerializedObjects.Add(obj.Id, obj);
        }

        public void AddReference(int fromObjectId, int toObjectId, string property)
        {
            if (!ReferencesFrom.TryGetValue(fromObjectId, out var references))
            {
                references = new HashSet<Reference>(new ReferenceComparer());
                ReferencesFrom[fromObjectId] = references;
            }

            references.Add(new Reference(toObjectId, property));

            if (!ReferencesTo.TryGetValue(toObjectId, out references))
            {
                references = new HashSet<Reference>(new ReferenceComparer());
                ReferencesTo[toObjectId] = references;
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
