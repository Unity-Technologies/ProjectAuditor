using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class BuildObjects
    {
        // Objects by id.
        Dictionary<int, SerializedObject> m_SerializedObjects = new Dictionary<int, SerializedObject>();
        IReadOnlyCollection<SerializedObject> SerializedObjects => m_SerializedObjects.Values;

        public void AddObject(SerializedObject obj)
        {
            m_SerializedObjects.Add(obj.Id, obj);
        }

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
