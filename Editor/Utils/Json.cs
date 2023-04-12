using System;
using System.IO;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class Json
    {
        internal static T[] From<T>(string json)
        {
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        internal static T[] FromFile<T>(string fileName)
        {
            var fullPath = Path.GetFullPath(fileName);
            var json = File.ReadAllText(fullPath);
            var items = Json.From<T>(json);

            return items;
        }

        internal static string To<T>(T[] array)
        {
            var wrapper = new Wrapper<T> {Items = array};
            return JsonUtility.ToJson(wrapper);
        }

        internal static string To<T>(T[] array, bool prettyPrint)
        {
            var wrapper = new Wrapper<T> {Items = array};
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        class Wrapper<T>
        {
            public T[] Items;
        }
    }
}
