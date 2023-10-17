using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class Json
    {
        static readonly JsonSerializerSettings k_JsonSerializerSettings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"
        };

        /// <summary>
        /// Serializes a DateTime object to a JSON-formatted string using a UTC format.
        /// </summary>
        /// <param name="dateTime">The DateTime object to serialize.</param>
        /// <returns>A JSON-formatted string representing the DateTime object in UTC.</returns>
        public static string SerializeDateTime(DateTime dateTime)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(dateTime.ToUniversalTime(), k_JsonSerializerSettings);
        }

        /// <summary>
        /// Deserializes a JSON-formatted string representing a DateTime in UTC to a DateTime object.
        /// </summary>
        /// <param name="utcDateTime">The JSON-formatted string representing the DateTime object in UTC.</param>
        /// <returns>A DateTime object converted to local time.</returns>
        public static DateTime DeserializeDateTime(string utcDateTime)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(utcDateTime, k_JsonSerializerSettings).ToLocalTime();
        }

        public static T[] From<T>(string json)
        {
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static T[] FromFile<T>(string fileName)
        {
            var fullPath = Path.GetFullPath(fileName);
            var json = File.ReadAllText(fullPath);
            var items = Json.From<T>(json);

            return items;
        }

        public static string To<T>(T[] array)
        {
            var wrapper = new Wrapper<T> {Items = array};
            return JsonUtility.ToJson(wrapper);
        }

        public static string To<T>(T[] array, bool prettyPrint)
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
