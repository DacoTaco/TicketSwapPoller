using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketSwapPoller.Models;

namespace TicketSwapPoller.Json
{
    public class TicketSwapNodeJsonConverter : JsonConverter<TicketSwapNode>
    {
        private readonly Dictionary<string, Type> _nodeTypes = new();
        public override bool CanConvert(Type typeToConvert) => typeof(TicketSwapNode).IsAssignableFrom(typeToConvert);

        public override TicketSwapNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            LoadNodeTypes();
            var oldReader = reader;
            string? nodeType;
            while (true)
            {
                if(!reader.Read())
                    throw new JsonException();

                if(reader.TokenType != JsonTokenType.PropertyName)
                {
                    reader.Skip();
                    continue;
                }

                var propertyName = reader.GetString();
                if(propertyName != "__typename")
                {
                    reader.Skip();
                    continue;
                }

                reader.Read();
                nodeType = reader.GetString();
                break;
            }

            if (string.IsNullOrWhiteSpace(nodeType) || !_nodeTypes.ContainsKey(nodeType))
                throw new JsonException();

            var modelType = _nodeTypes[nodeType];
            var model = (TicketSwapNode?)Activator.CreateInstance(modelType);
            if (model == null)
                throw new JsonException();

            var modelProperties = modelType.GetProperties();
            reader = oldReader;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    if (string.IsNullOrWhiteSpace(propertyName))
                        continue;

                    var propertyInfo = modelProperties.SingleOrDefault(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)) ??
                                       modelProperties.SingleOrDefault(property => Attribute.IsDefined(property, typeof(JsonPropertyNameAttribute)) && (property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? "").Equals(propertyName, StringComparison.OrdinalIgnoreCase));

                    if (propertyInfo == null || !propertyInfo.CanWrite)
                    {
                        reader.Skip();
                        continue;
                    }

                    object? value = null;
                    switch(reader.TokenType)
                    {
                        case JsonTokenType.String:
                            value = reader.GetString();
                            break;
                        case JsonTokenType.Number:
                            value = reader.GetInt32();
                            break;
                        case JsonTokenType.StartObject:
                            value = JsonDocument.ParseValue(ref reader).Deserialize(propertyInfo.PropertyType, options);
                            break;
                        case JsonTokenType.StartArray:
                            var argumentType = propertyInfo.PropertyType.GetGenericArguments().Single();
                            var listType = typeof(List<>).MakeGenericType(argumentType);
                            var addMethod = listType.GetMethod("Add");
                            var list = Activator.CreateInstance(listType);

                            var document = JsonDocument.ParseValue(ref reader);
                            foreach(var item in document.RootElement.EnumerateArray())
                                addMethod?.Invoke(list, new[] { item.Deserialize(argumentType, options) });


                            value = list;
                            break;
                        case JsonTokenType.True:
                        case JsonTokenType.False:
                            value = reader.TokenType == JsonTokenType.True;
                            break;
                        case JsonTokenType.Null:
                            value = null;
                            break;
                        default:
                            throw new JsonException($"Unexpected jsonTokenType {reader.TokenType}");
                    }

                    propertyInfo.SetValue(model, value);
                }                    
            }

            return model;
        }

        public override void Write(Utf8JsonWriter writer, TicketSwapNode value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        private void LoadNodeTypes()
        {
            if (_nodeTypes.Any())
                return;

            var types = Assembly.GetExecutingAssembly().GetTypes().Where(type => CanConvert(type));

            foreach(var type in types)
            {
                if (type.IsAbstract)
                    continue;

                var model = (TicketSwapNode?)Activator.CreateInstance(type);
                if (model == null)
                    continue;

                _nodeTypes.Add(model.TypeName, type);
            }
        }
    }
}
