using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.ProjectAuditor.Editor.Core
{
    class DescriptorJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value);
            if (token.Type != JTokenType.Object)
            {
                token.WriteTo(writer);
                return;
            }

            // remove properties that are not needed in the report
            var removeProperties = new List<string>()
            {
                nameof(Descriptor.DefaultSeverity),
                nameof(Descriptor.IsEnabledByDefault),
                nameof(Descriptor.MessageFormat),
                nameof(Descriptor.Type),
                nameof(Descriptor.Method),
                nameof(Descriptor.Value),
                nameof(Descriptor.MinimumVersion),
                nameof(Descriptor.MaximumVersion)
            };

            var obj = (JObject)token;
            var newObj = new JObject();
            foreach (var property in obj.Properties())
            {
                if (removeProperties.Contains(property.Name))
                    continue;

                if (property.Value.Type == JTokenType.Null)
                    continue;

                if (property.Value.Type == JTokenType.String && string.IsNullOrEmpty(property.Value.ToString()))
                    continue;

                var camelCaseName = ConvertToCamelCase(property.Name);
                newObj[camelCaseName] = property.Value;
            }

            // Write the camelCase JObject to the writer
            newObj.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, objectType);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Descriptor);
        }

        string ConvertToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            {
                return name;
            }

            var camelCaseName = char.ToLowerInvariant(name[0]) + name.Substring(1);
            return camelCaseName;
        }
    }
}
