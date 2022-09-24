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
    public decimal POSX { get; set; }
    
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal POSY { get; set; }
    
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal POSZ { get; set; }
    
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime DATA { get; set; }
    
    [BsonIgnore]
    public bool ENVIADO { get; set; }
}