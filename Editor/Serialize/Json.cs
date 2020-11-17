using System;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Serialize
{
    static class Json
    {
        public static T[] From<T>(string json)
        {
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string To<T>(T[] array)
        {
            var wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string To<T>(T[] array, bool prettyPrint)
        {
            var wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        class Wrapper<T>
        {
            public T[] Items;
        }
    }
}
