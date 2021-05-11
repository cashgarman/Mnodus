using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Assets.Scripts
{
	public class Entity : ForceGraph<Entity, Relationship, ObjectId>.IGUID
	{
		public BsonDocument document;

		protected IEnumerable<ObjectId> cachedConnectedNodeIDs;

		public MongoCollection<Entity> collection;

		public BsonValue this[string key]
		{
			get { return GetValue(key); }
			set { SetValue(key, value); }
		}

		private void SetValue(string key, BsonValue value)
		{
			document[key] = value;
		}

		private BsonValue GetValue(string key)
		{
			return document[key];
		}

		public Entity(BsonDocument _document)
		{
			document = _document;
			collection = Dataset.GetCollectionByName(GetValue("type").ToString());
		}

		public override string ToString()
		{
			return this["name"].ToString();
		}

		public string Name
		{
			get { return ToString(); }
		}

		public ObjectId ID
		{
			get { return ObjectId.Parse(document.GetValue("_id").ToString()); }
		}

		public string Type
		{
			get { return this["type"].ToString(); }
		}

		// TODO: Decide this somehow in scripting logic
		public bool AlwaysShow { get { return false; } }

		public virtual IEnumerable<ObjectId> GetConnectedNodeIDs()
		{
//			throw new NotImplementedException();
			return null;
		}

		public virtual string GetDetails()
		{
//			throw new NotImplementedException();
			return null;
		}

		public static IEnumerable<Dataset.EntityFilter> GetFilters(string name)
		{
			return Dataset.instance.datasetSpec.entities[name].filters;
		}

		public static IEnumerable<Dataset.Metric> GetMetrics(string name)
		{
			return Dataset.instance.datasetSpec.entities[name].metrics;
		}

		public static string BuildEntityHyperlink(Entity entity)
		{
			return String.Format("<b><color=cyan><a href={0}|{1}>{2}</a></color></b>", entity.ID, entity.GetType().Name, entity);
		}

		public bool ContainsKey(string key)
		{
			return document.Contains(key);
		}
	}
}