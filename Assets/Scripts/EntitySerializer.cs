using System;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Assets.Scripts
{
	public class EntitySerializer : BsonBaseSerializer
	{
		public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
		{
			var backingDocument = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), typeof(BsonDocument), null);
			return new Entity(backingDocument);
		}

		public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
		{
			var resource = (Entity)value;
			var backingDocument = resource.document;
			BsonDocumentSerializer.Instance.Serialize(bsonWriter, typeof(BsonDocument), backingDocument, null);
		}
	}
}