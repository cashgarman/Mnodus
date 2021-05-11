using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Assets.Scripts
{
	public class MongoObjectCollection : ObjectCollection<ObjectId>, IGraphObjectCollection
	{
		public class MongoGraphObject : IGraphObject<ObjectId>
		{
			public ObjectId ID { get; set; }
			public BsonDocument document;

			public object GetValue(string name)
			{
				BsonValue value;
				if(!document.TryGetValue(name, out value))
					throw new KeyNotFoundException(string.Format("Couldn't find value with name {0} is Mongo document", name));
				return value;
			}
		}

		private MongoCollection<BsonDocument> collection;
		private Dictionary<ObjectId, MongoGraphObject> objects;
		private static readonly Dictionary<string, MongoDatabase> databases = new Dictionary<string, MongoDatabase>();

		public MongoObjectCollection(Dataset.DataSource source)
		{
			collection = GetMongoCollection(source);
		}

		public override IGraphObject<ObjectId> GetByID(ObjectId id)
		{
			if (!objects.ContainsKey(id))
			{
				var document = collection.FindOneById(id);
				if (document == null)
					throw new KeyNotFoundException("Couldn't find document in Mongo collection with ID " + id);

				objects[id] = new MongoGraphObject
				{
					ID = id,
					document = document
				};
			}

			return objects[id];
		}

		public static MongoCollection<BsonDocument> GetMongoCollection(Dataset.DataSource source)
		{
			if (!source.metadata.ContainsKey("URL")) throw new KeyNotFoundException("MongoDB datasource is missing URL");
			if (!source.metadata.ContainsKey("DatabaseName")) throw new KeyNotFoundException("MongoDB datasource is missing database name");
			if (!source.metadata.ContainsKey("CollectionName")) throw new KeyNotFoundException("MongoDB datasource is missing collection name");

			var url = source.metadata["URL"];
			var databaseName = source.metadata["DatabaseName"];
			var collectionName = source.metadata["CollectionName"];

			if (!databases.ContainsKey(databaseName))
			{
				var client = new MongoClient(url).GetServer();

				if (client == null) throw new Exception("Couldn't connect to MongoDB on " + url + " with name " + databaseName);

				var db = client.GetDatabase(databaseName);

				if (db == null)
					throw new Exception("Couldn't MongoDB database called " + databaseName + " on " + url);

				databases.Add(databaseName, db);
			}

			var collection = databases[databaseName].GetCollection(collectionName);

			if (collection == null)
				throw new Exception("Couldn't find collection called " + collectionName + " on MongoDB " + databaseName);

			return collection;
		}

		public IEnumerable GetObjects()
		{
			return collection.FindAll();
		}
	}
}