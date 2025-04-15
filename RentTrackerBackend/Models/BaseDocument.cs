using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RentTrackerBackend.Models
{
    public abstract class BaseDocument
    {
        static BaseDocument()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(BaseDocument)))
            {
                BsonClassMap.RegisterClassMap<BaseDocument>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                    cm.MapIdProperty(c => c.Id);
                    cm.MapProperty(c => c.TenantId).SetElementName("tenantId");
                });
            }
        }

        [BsonId]
        [JsonIgnore]
        public ObjectId Id { get; init; } = ObjectId.GenerateNewId();
        
        [BsonElement("tenantId")]
        public string TenantId { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("version")]
        public long Version { get; set; } = 1;

        [BsonIgnore]
        [JsonPropertyName("id")]
        public string FormattedId => Id.ToString();
    }
}