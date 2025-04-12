using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace RentTrackerBackend.Extensions
{
    public class ObjectIdJsonConverter : JsonConverter<ObjectId>
    {
        public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();
                if (string.IsNullOrEmpty(value) || value == "00000000-0000-0000-0000-000000000000")
                {
                    return ObjectId.Empty;
                }
                return ObjectId.Parse(value);
            }
            
            throw new JsonException("Expected string value for ObjectId");
        }

        public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}