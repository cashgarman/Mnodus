using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ForceGraphs.Scripts;
using Assets.Scripts;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using UnityEngine;
using UnityEngine.UI;

namespace Assets
{
	public class EditorNode : MonoBehaviour
	{
		public int id;
		private static int _nextEditorNodeID;

		public Color unselectedColor;
		public Color selectedColor;
		public string objectType;
		public GameObject settingsContainer;
		public GameObject filtersContainer;
		private bool settingsPopulated;
		public GameObject headerObject;
		public GameObject addFilterObject;
		public Dropdown filterDropdown;
		public Dropdown scaleDropdown;
		public Dropdown colorDropdown;

		public GameObject stringFilterPrefab;
		public GameObject intFilterPrefab;
		public GameObject toggleFilterPrefab;
		public GameObject choiceFilterPrefab;
		public Text labelText;
		public readonly List<Wire> wires = new List<Wire>();
		public MongoCollection<Entity> collection;
		private MongoCursor<Entity> results;
		private IEnumerable<IMongoQuery> queries;
		public bool resultsDirty;

		public void Awake()
		{
			id = _nextEditorNodeID++;
		}

		public void Start()
		{
			SetColor(unselectedColor);
			filterDropdown.ClearOptions();
		}

		public void OnSelected()
		{
			SetColor(selectedColor);

			ShowFilters();
		}

		public void OnDeselected()
		{
			SetColor(unselectedColor);

//			UpdateQueries();

			HideFilters();
		}

		public void UpdateQueries()
		{
			if (!resultsDirty)
				return;

			var filterQueries = GetFilters().Select(CreateMongoQuery);
			var relationshipQueries = GetRelationships().Select(CreateMongoQuery);

			queries = filterQueries.Concat(relationshipQueries);

			Debug.Log(string.Format("Current queries: {0}", string.Join(",", queries.Select(q => q.ToJson()).ToArray())));

			UpdateResults();

			resultsDirty = false;
		}

		private void UpdateResults()
		{
			if (queries.Any())
			{
				results = collection.Find(Query.And(queries));
				Debug.Log(string.Format("Found {0} results", results.Count()));
			}
			else
			{
				results = collection.FindAll();
				Debug.Log("Found no results");
			}

			// TODO: Re-add these after testing
//			results.SetMaxTime(TimeSpan.FromSeconds(Settings.instance.maxQueryTime));
//			results.SetLimit(Settings.instance.maxEntityResults);

			UpdateLabel();
		}

		public IEnumerable<Entity> GetResults()
		{
			return results.ToList();
		}

		public long GetResultsCount()
		{
			return results.Count();
		}

		private IMongoQuery CreateMongoQuery(Dataset.Filter filter)
		{
			switch (filter.entityFilter.type)
			{
				case Dataset.EntityFilter.FilterType.String:
//					return from result in results.AsQueryable() where result[filter.entityFilter.fieldName].AsString == filter.stringValue select result;
					return Query.Matches(filter.entityFilter.fieldName, new BsonRegularExpression(string.Format("/{0}/i", filter.stringValue)));

				case Dataset.EntityFilter.FilterType.Int:
				{
					if (filter.greaterThan)
					{
						if (filter.equalTo)
							return Query.GTE(filter.entityFilter.fieldName, filter.intValue);
//							return from result in results.AsQueryable()
//									where result[filter.entityFilter.fieldName].AsInt32 >= filter.intValue
//									select result;

						return Query.GT(filter.entityFilter.fieldName, filter.intValue);
//						return from result in results.AsQueryable()
//							where result[filter.entityFilter.fieldName].AsInt32 > filter.intValue
//							select result;
					}

					if (filter.lessThan)
					{
						if (filter.equalTo)
							return Query.LTE(filter.entityFilter.fieldName, filter.intValue);

						return Query.LT(filter.entityFilter.fieldName, filter.intValue);
					}

					return Query.EQ(filter.entityFilter.fieldName, filter.intValue);
//					return from result in results.AsQueryable()
//						   where result[filter.entityFilter.fieldName].AsInt32 == filter.intValue
//						   select result;
				}

				case Dataset.EntityFilter.FilterType.Boolean:
					return Query.EQ(filter.entityFilter.fieldName, filter.booleanValue);
//					return from result in results.AsQueryable()
//						   where result[filter.entityFilter.fieldName].AsBoolean == filter.booleanValue
//						   select result;

				default:
					throw new Exception("Unknown filter type: " + filter.entityFilter.type);
			}
		}

		private IMongoQuery CreateMongoQuery(Dataset.Relationship relationship)
		{
			if (relationship.data.metadata.ContainsKey("ContainsTargetField"))
			{
//				asdf
					// Make this work with the new array indicies somehow

				var results = relationship.sourceResults.SelectMany(r => r[relationship.data.metadata["SourceField"]].AsBsonArray);
				var query = Query.In(relationship.data.metadata["ContainsTargetField"], results);

//				var query = Query.Or(relationship.sourceResults.Where(r => r.ContainsKey(relationship.data.metadata["SourceField"]))
//					.Select(r => Query.In(relationship.data.metadata["ContainsTargetField"],
//					r[relationship.data.metadata["SourceField"]].AsBsonArray)));

				return query;
			}

			return Query.In(relationship.data.metadata["SourceField"],
				relationship.targetResults.Select(e => e[relationship.data.metadata["TargetField"]]));
		}

		private void ShowFilters()
		{
			if (!settingsPopulated)
				PopulateSettings();

			ShowFilterOptions();

			settingsContainer.SetActive(true);
		}

		private void HideFilters()
		{
			settingsContainer.SetActive(false);
		}

		private void PopulateSettings()
		{
			// Populate filters
			var filters = Entity.GetFilters(objectType);
			if (filters == null)
				return;

			filterDropdown.ClearOptions();
			filterDropdown.AddOptions(new List<string> { "Add Filter" }.Concat(filters.Select(f => f.name)).ToList());

			headerObject.transform.SetAsFirstSibling();
			addFilterObject.transform.SetAsLastSibling();
			filterDropdown.transform.SetAsLastSibling();

			// Populate appearance settings
			var metrics = Entity.GetMetrics(objectType);
			scaleDropdown.ClearOptions();
			colorDropdown.ClearOptions();
			if (metrics != null)
			{
				scaleDropdown.AddOptions(new List<string> { "Nothing" }.Concat(metrics.Select(m => m.name)).ToList());
				colorDropdown.AddOptions(new List<string> { "Nothing" }.Concat(metrics.Select(m => m.name)).ToList());
			}
			else
			{
				scaleDropdown.AddOptions(new List<string> { "Nothing" });
				colorDropdown.AddOptions(new List<string> { "Nothing" });
			}

			settingsPopulated = true;
		}

		public BaseFilter AddFilter(Dataset.EntityFilter filterType)
		{
			Debug.Log("Adding filter: " + filterType.name + " of type " + filterType.type + " for the first time");

			BaseFilter filter = null;
			switch (filterType.type)
			{
				case Dataset.EntityFilter.FilterType.String:
				{
					var filterObject = Instantiate(stringFilterPrefab);
					filterObject.transform.SetParent(filtersContainer.transform);
					filter = filterObject.GetComponent<StringFilter>();
					filter.SetFilter(filterType, this);
					break;
				}

				case Dataset.EntityFilter.FilterType.Int:
				{
					var filterObject = Instantiate(intFilterPrefab);
					filterObject.transform.SetParent(filtersContainer.transform);
					filter = filterObject.GetComponent<IntFilter>();
					filter.SetFilter(filterType, this);
					break;
				}

				case Dataset.EntityFilter.FilterType.Boolean:
				{
					if (!string.IsNullOrEmpty(filterType.trueLabel) && !string.IsNullOrEmpty(filterType.falseLabel))
					{
						var filterObject = Instantiate(choiceFilterPrefab);
						filterObject.transform.SetParent(filtersContainer.transform);
						filterObject.GetComponent<ChoiceFilter>().SetFilter(filterType, this);

						var choiceFilter = filterObject.GetComponent<ChoiceFilter>();
						choiceFilter.trueToggle.GetComponentInChildren<Text>().text = filterType.trueLabel;
						choiceFilter.falseToggle.GetComponentInChildren<Text>().text = filterType.falseLabel;
						filter = choiceFilter;
					}
					else
					{
						var filterObject = Instantiate(toggleFilterPrefab);
						filterObject.transform.SetParent(filtersContainer.transform);
						filter = filterObject.GetComponent<ToggleFilter>();
						filter.SetFilter(filterType, this);
					}
					
					break;
				}
			}

			return filter;
		}

		private void SetColor(Color color)
		{
			GetComponentInChildren<Image>().color = color;
		}

		public void ShowFilterOptions()
		{
			addFilterObject.SetActive(false);
			filterDropdown.gameObject.SetActive(true);
		}

		public void OnFilterOptionSelected()
		{
			if (filterDropdown.value == 0)
				return;

//			filterDropdown.gameObject.SetActive(false);
//			addFilterObject.SetActive(true);

			AddFilter(Entity.GetFilters(objectType).ToArray()[filterDropdown.value - 1]);

			filterDropdown.transform.SetAsLastSibling();
			filterDropdown.value = 0;
		}

		public void SetName(string name)
		{
			labelText.text = name;
			gameObject.name = name;
		}

		public IEnumerable<Dataset.Filter> GetFilters()
		{
			var filters = new List<Dataset.Filter>();
			foreach (Transform filterObject in filtersContainer.transform)
			{
				var stringFilter = filterObject.GetComponent<StringFilter>();
				if (stringFilter != null)
				{
					Debug.Log("Found string filter for " + objectType);
					filters.Add(new Dataset.Filter
					{
						entityFilter = stringFilter.filter,
						stringValue = stringFilter.inputField.text
					});
				}

				var intFilter = filterObject.GetComponent<IntFilter>();
				if (intFilter != null)
				{
					Debug.Log("Found int filter for " + objectType);
					var value = intFilter.inputField.text.Trim();
					filters.Add(new Dataset.Filter
					{
						entityFilter = intFilter.filter,
						intValue = Convert.ToInt32(value.Replace("<", "").Replace("=", "").Replace(">", "")),
						lessThan = value.Contains("<"),
						greaterThan = value.Contains(">"),
						equalTo = value.Contains("=")
					});
				}

				var toggleFilter = filterObject.GetComponent<ToggleFilter>();
				if (toggleFilter != null)
				{
					Debug.Log("Found toggle filter for " + objectType);
					filters.Add(new Dataset.Filter
					{
						entityFilter = toggleFilter.filter,
						booleanValue = toggleFilter.toggle.isOn
					});
				}

				var choiceFilter = filterObject.GetComponent<ChoiceFilter>();
				if (choiceFilter != null)
				{
					Debug.Log("Found choide filter for " + objectType);
					filters.Add(new Dataset.Filter
					{
						entityFilter = choiceFilter.filter,
						booleanValue = choiceFilter.trueToggle.isOn
					});
				}
			}

			return filters;
		}

		public IEnumerable<Dataset.Relationship> GetRelationships()
		{
			return (from wire in wires
				let possibleRelationships = GraphEditor.instance.GetPossibleRelationships(wire.start.objectType, wire.end.objectType)
				let relationship = possibleRelationships.ElementAt(filterDropdown.value).Value
				where
					(relationship.start == relationship.sourceEntity && objectType == relationship.sourceEntity) ||
					(relationship.start == relationship.targetEntity && objectType == relationship.targetEntity)
				select new Dataset.Relationship
				{
					data = relationship,
					sourceResults = wire.start.objectType == relationship.sourceEntity ? wire.start.results : wire.end.results,
					targetResults = wire.start.objectType == relationship.targetEntity ? wire.start.results : wire.end.results,
				});
		}

		public void SetCollection(MongoCollection<Entity> _collection)
		{
			collection = _collection;
			results = collection.FindAll();
			UpdateLabel();
		}

		private void UpdateLabel()
		{
			SetName(string.Format("{0}\n<size=13>{1}</size>", Util.Capitalize(collection.Name), Util.FormatLargeNumber(GetResultsCount())));
		}

		public void OnMouseOver()
		{
			if (GraphEditor.CreatingWire)
			{
				if (GraphEditor.instance.GetPossibleRelationships(
					GraphEditor.instance.wireStartNode.GetComponent<EditorNode>().objectType,
					objectType).Any())
					GraphEditor.instance.OnValidWireEndpoint();
				else
					GraphEditor.instance.OnInvalidWireEndpoint();
			}
		}

		public void OnMouseExit()
		{
			if (GraphEditor.CreatingWire)
				GraphEditor.instance.OnUnknownWireEndpoint();
		}
	}
}