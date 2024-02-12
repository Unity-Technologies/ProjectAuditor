using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    class BuildTargetJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is BuildTarget[] targets)
            {
                writer.WriteStartArray();
                foreach (var target in targets)
                {
                    writer.WriteValue(target.ToString());
                }
                writer.WriteEndArray();
            }
            else if (value is BuildTarget target)
            {
                writer.WriteValue(target.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var array = JArray.Load(reader);
                return array.Select(i => (BuildTarget)Enum.Parse(typeof(BuildTarget), i.ToString())).ToArray();
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return (BuildTarget)Enum.Parse(typeof(BuildTarget), reader.Value.ToString());
            }

            throw new JsonSerializationException("Unexpected token type: " + reader.TokenType);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BuildTarget) || objectType == typeof(BuildTarget[]);
        }
    }
}
