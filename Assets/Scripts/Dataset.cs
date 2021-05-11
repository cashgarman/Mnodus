using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ForceGraphs.Scripts;
using MongoDB.Bson.Serialization;
using UnityEngine;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Assets.Scripts
{
	public class Dataset : MonoBehaviour
	{
		public class DatasetSpecification
		{
			public Dictionary<string, EntitySpecification> entities;
			public Dictionary<string, RelationshipData> relationships;
		}

		public class EntitySpecification
		{
			public DataSource dataSource;
			public EntityFilter[] filters;
			public Metric[] metrics;
			public bool hidden;
		}

		[Serializable]
		public class EntityFilter
		{
			[Serializable]
			public enum FilterType
			{
				String,
				Int,
				Boolean
			}

			public string name;
			public FilterType type;
			public string fieldName;
			public string trueLabel;
			public string falseLabel;
		}

		[Serializable]
		public class Filter
		{
			public EntityFilter entityFilter;

			public string stringValue;

			public int intValue;

			public bool booleanValue;
			public bool greaterThan;
			public bool lessThan;
			public bool equalTo;
		}

		public class Metric
		{
			public string name;
			public string type;
			public string fieldName;
		}

		public class IncludedEntity
		{
			public IEnumerable<Entity> entities; 
			public IEnumerable<Relationship> relationships;
			public Action<GameObject, Entity> scaleProvider;
			public Action<GameObject, Entity> colorProvider;
		}

		public class RelationshipData
		{
			public string start;
			public string end;
			public string sourceEntity;
			public string targetEntity;
			public Dictionary<string, string> metadata;
		}

		public class DataSource
		{
			public DataSourceType type;
			public Dictionary<string, string> metadata;
		}

		public enum DataSourceType
		{
			File,
			MongoDB
		}

		public static Dataset instance;

		private MongoClient client;

		internal class RelationshipCollection
		{
		}

		public DatasetSpecification datasetSpec;
		private readonly Dictionary<string, IObjectCollection> objectCollections = new Dictionary<string, IObjectCollection>();
		private readonly Dictionary<string, RelationshipCollection> relationshipCollections = new Dictionary<string, RelationshipCollection>();
		private readonly Dictionary<string, MongoCollection<Entity>> collections = new Dictionary<string, MongoCollection<Entity>>();

		public void Start()
		{
			instance = this;

			BsonSerializer.RegisterSerializer(typeof(Entity), new EntitySerializer());

			datasetSpec = new DatasetSpecification();
			Util.PopulateFromJSONObject(datasetSpec, Application.dataPath + Settings.instance.datasetSpecPath);

			client = new MongoClient("mongodb://localhost:27017/");

			var db = client.GetServer().GetDatabase("test_dataset");

			foreach (var entityType in datasetSpec.entities)
			{
				collections[entityType.Key] = db.GetCollection<Entity>(entityType.Value.dataSource.metadata["CollectionName"]);
				Debug.Log(string.Format("Found {0} {1} in the database", collections[entityType.Key].Count(), entityType.Key));
			}

			LoadDatasetObjects();
//			LoadDatasetRelationships();
		}

		private void LoadDatasetObjects()
		{
			foreach(var datasetObject in datasetSpec.entities)
				objectCollections.Add(datasetObject.Key, LoadObjectFromDatasource(datasetObject.Value));
		}

		private IObjectCollection LoadObjectFromDatasource(EntitySpecification entitySpecification)
		{
			switch (entitySpecification.dataSource.type)
			{
				case DataSourceType.MongoDB:
				{
					return new MongoObjectCollection(entitySpecification.dataSource)
					{
						entitySpecification = entitySpecification
					};
				}

				default:
					throw new Exception("Unhandled datasource type " + entitySpecification.dataSource.type);
			}
		}

		public static Entity GetEntityByID(string type, string id)
		{
			return GetEntityByID(type, new ObjectId(id));
		}

		public static Entity GetEntityByID(string type, ObjectId id)
		{
			try
			{
//				asdf
					// Reintroduce cached collections
				return instance.collections[type].FindOneById(id);
			}
			catch (KeyNotFoundException e)
			{
				throw new KeyNotFoundException(string.Format("Couldn't find {0} with ID {1}", type, id));
			}
		}

		public static IEnumerable<string> GetObjectCollectionNames()
		{
			return instance.objectCollections.Keys;
		}

		public static Dictionary<string, MongoCollection<Entity>>.ValueCollection GetObjectCollections()
		{
			return instance.collections.Values;
		}

		public static IEnumerable<string> GetTypes()
		{
			return instance.datasetSpec.entities.Where(e => !e.Value.hidden).Select(e => e.Key);
		}

		public static MongoCollection<Entity> GetCollectionByName(string name)
		{
			return instance.collections[name];
		}

		public class Relationship
		{
			public RelationshipData data;
			public MongoCursor<Entity> sourceResults;
			public MongoCursor<Entity> targetResults;
		}
	}
}
