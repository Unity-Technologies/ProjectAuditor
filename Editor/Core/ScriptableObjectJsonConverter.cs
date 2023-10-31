using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    public class ScriptableObjectJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ProjectAuditorRules);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Create a new instance using ScriptableObject.CreateInstance<T>
            var instance = ScriptableObject.CreateInstance(objectType);
            if (instance == null)
            {
                throw new JsonSerializationException($"Cannot create instance of {objectType.FullName}");
            }

            // Populate the instance's fields using the default serializer
            var obj = JObject.Load(reader);
            serializer.Populate(obj.CreateReader(), instance);

            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Use the default serialization implementation
            serializer.Serialize(writer, value);
        }
    }
}
