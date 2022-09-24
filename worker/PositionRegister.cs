using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class PositionRegister
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; }

    [BsonRepresentation(BsonType.Int32)]
    public int ID { get; set; }

    [BsonRepresentation(BsonType.String)]
    public string TAG { get; set; }
    
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal POS_X { get; set; }
    
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal POS_Y { get; set; }
    
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal POS_Z { get; set; }
    
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime INS_DATE { get; set; }
    
    [BsonIgnore]
    public bool SENT { get; set; }
}