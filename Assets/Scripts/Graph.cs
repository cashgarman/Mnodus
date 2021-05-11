using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Scripts.Cameras;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

// Shortcut for otherwise large type definitions everywhere
using GraphNode = Assets.ForceGraph<Assets.Scripts.Entity, Assets.Scripts.Relationship, MongoDB.Bson.ObjectId>.Node;

namespace Assets.Scripts
{
	public class Relationship
	{
		public string type;

		public override string ToString()
		{
			return type;
		}
	}

	[Serializable]
	public class NodeSettings
	{
		public GameObject prefab;
		public float mass = 1f;
		public float minScale = 1f;
		public float maxScale = 3f;
		public float linkWidth = 0.1f;
		public float focusDistance = 1f;
		public float linkDistance = .25f;
		public Color linkColorOverride;
		public Color minColor = Color.red;
		public Color maxColor = Color.green;
		public Color color = Color.white;
	}

	public class Graph : MonoBehaviour
	{
		public static Graph instance;

		public ComputeShader computeShader;
		public ForceGraphPhysicsSettings physicsSettings;
		public float worldSize = 50f;
		public Canvas worldCanvas;
		public Transform centerTransform;
		public Transform nodeContainer;
		public Text fpsText;
		public Text nodesText;
		public Text kineticEnergyText;
		public Text numSearchResultsText;
		public GameObject buttonNodePrefab;
		public float buttonOffset;
		public float focusDistance = 15f;
		public GameObject detailsPanel;
		public TextWithEvents detailsText;
		public Text detailsName;

		public NodeSettings countrySettings;
		public NodeSettings industrySettings;
		public NodeSettings companySettings;
		public NodeSettings siteSettings;
		public NodeSettings personSettings;
		public NodeSettings emailSettings;
		public NodeSettings buttonSettings;

		public GameObject linkPrefab;

		public float companyIndustryLinkWidth;

		public static ForceGraph<Entity, Relationship, ObjectId> graph;
		private bool paused;
		private GraphNode selectedNode;
		public InputField searchField;
		public Button[] searchResultButtons;
		private float timeSinceLastSearchInputChange;
		private bool searchInputChanged;
		public float searchDelay = .25f;
		private List<Entity> currentSearchResults = new List<Entity>();
		private bool clickingOnSearchResult;
		private readonly List<GraphNode> nodesPendingConnections = new List<GraphNode>();
		public float averageKineticEnergyThresholdBeforeSettle;
		private bool simulationStarted;
		private bool forcePhysics;

		public void Awake()
		{
			instance = this;
		}

		public void Start()
		{
			graph = new ForceGraph<Entity, Relationship, ObjectId>(computeShader, physicsSettings, nodeContainer);
			graph.Init();

			ClearSearchResults();
		}

		private GraphNode AddNodeToGraph(Entity entity, NodeSettings settings, Vector3? position = null, float? spawnDistance = null, int group = -1)
		{
			paused = false;

			var node = graph.AddNode(entity, Instantiate(settings.prefab),
					!position.HasValue ? Random.insideUnitSphere * worldSize :
					position.Value + Random.onUnitSphere * (spawnDistance.HasValue ? spawnDistance.Value : 1f), settings.mass, group);

			if (node == null)
				return null;

			node.representation.transform.Find("Name").GetComponent<Text>().text = entity.ToString();

//			node.colorOverrider = (gameObject, e) => gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", settings.color);
//			node.scaleOverrider = (gameObject, e) => { gameObject.transform.localScale = Vector3.one * (settings.minScale + settings.maxScale) / 2f; };

			node.representation.GetComponent<Clickable>().onLeftClick = () => OnNodeLeftClicked(node);
			node.representation.GetComponent<Clickable>().onDoubleLeftClick = () => OnNodeDoubleLeftClicked(node);

//			HandlePendingConnections();

			return node;
		}

		private void OnNodeLeftClicked(GraphNode node)
		{
			SelectNode(node);
		}

		private void SelectNode(GraphNode node)
		{
			if (selectedNode != null)
				UnselectNode(selectedNode);

			selectedNode = node;

			if (selectedNode != null)
			{
//				OrbitCamera.FocusOn(node.representation.transform, node.representation.transform.localScale.magnitude * GetSettings(node.entity).focusDistance);
				
				node.representation.GetComponentInChildren<FadeByCameraDistance>().alwaysShow = true;
//				detailsPanel.SetActive(true);
				detailsName.text = node.entity.Name;
				StartCoroutine(ReplaceDetailsText(node.entity.GetDetails(), detailsText.font, detailsText.fontSize, detailsText.fontStyle, detailsText.alignment));
			}
		}

		private IEnumerator ReplaceDetailsText(string newText, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment)
		{
			var detailsObject = detailsText.gameObject;
			Destroy(detailsText);

			yield return new WaitForEndOfFrame();

			detailsText = detailsObject.AddComponent<TextWithEvents>();
			detailsText.font = font;
			detailsText.fontSize = fontSize;
			detailsText.fontStyle = fontStyle;
			detailsText.alignment = alignment;
			detailsText.text = newText;

			// Hook up any hyperlink buttons that might have been created for the selection
			var buttons = detailsText.GetComponentsInChildren<TextButton>();
			foreach (var button in buttons)
				button.onClick.AddListener(OnHyperlinkClicked);
		}

		private void OnHyperlinkClicked()
		{	
			// Find the ID and type of the entity the hyperlink that was clicked represented
			var parts = EventSystem.current.currentSelectedGameObject.name.Split(new []{'|'});
			var ID = parts[0];
			var type = parts[1];

			var entity = Dataset.GetEntityByID(type, ID);

			DrillDownToEntity(entity);
		}

		private void UnselectNode(GraphNode node)
		{
			if (node == null)
				return;

			node.representation.GetComponentInChildren<FadeByCameraDistance>().alwaysShow = node.entity.AlwaysShow;
			detailsPanel.SetActive(false);

			// Remove any hyperlink buttons from the last selection
			foreach(Transform button in detailsText.transform)
				Destroy(button.gameObject);
		}

		public NodeSettings GetNodeSettings(Entity entity)
		{
			// TODO: Move all this bullshit into a config script somewhere

			// Fake dataset
			//			if (entity["type"] == "Country") return countrySettings;
			//			if (entity["type"] == "Industry") return industrySettings;
			//			if (entity["type"] == "Company") return companySettings;
			//			if (entity["type"] == "Site") return siteSettings;
			//			if (entity["type"] == "Person") return personSettings;
			//			if (entity["type"] == "Email") return emailSettings;

			// Mining dataset 2 (All bullshit)
			if (entity["type"] == "City") return industrySettings;
			if (entity["type"] == "Industry") return companySettings;
			if (entity["type"] == "Company") return siteSettings;
			if (entity["type"] == "Person") return personSettings;

			// OMDB
//			if (entity["type"] == "Genres") return industrySettings;
//			if (entity["type"] == "Movies") return siteSettings;
//			if (entity["type"] == "Cast") return personSettings;
//			if (entity["type"] == "Directors") return companySettings;

			// TODO: Define the physics settings in the dataset specification file
			return countrySettings;

			throw new Exception("Unknown entity type while trying to get physics settings");
		}

		private void RefreshWorldCanvas()
		{
			// HACK: Workaround for text sometimes not being displayed after being set
			worldCanvas.enabled = false;
			worldCanvas.enabled = true;
		}

		private void OnNodeDoubleLeftClicked(GraphNode node)
		{
			Debug.Log("Double clicked " + node.entity.Name);
			FlyCamera.FocusOn(node.representation.transform.position, 10f);
		}

		public void Update()
		{
			HandleKeys();

			if (!paused && simulationStarted)
			{
				graph.ForcePhysicsSettingsUpdate();
				graph.Update(Time.fixedDeltaTime);

				if(graph.averageKineticEnergy < averageKineticEnergyThresholdBeforeSettle && !forcePhysics)
					graph.FreezeAllNodes();

				if(forcePhysics)
					graph.UnfreezeAllNodes();

				if(!float.IsNaN(graph.center.x))
					centerTransform.position = graph.center;
			}

			fpsText.text = string.Format("{0:F0} fps {1}", 1f / Time.deltaTime, paused ? "(Paused)" : "");
			nodesText.text = string.Format("{0} Nodes ({1})", graph.NumNodes, graph.NumStaticNodes);
			kineticEnergyText.text = string.Format("Average Energy: {0}", graph.averageKineticEnergy);

			timeSinceLastSearchInputChange += Time.unscaledDeltaTime;
			if (timeSinceLastSearchInputChange > searchDelay && searchInputChanged)
				UpdateSearchResults();
		}

		private void HandleKeys()
		{
			if (!searchField.isFocused)
			{
				if (Input.GetKeyDown(KeyCode.P))
					paused = !paused;

				if(Input.GetKeyDown(KeyCode.Escape))
					UnselectNode(selectedNode);
			}

			if (Input.GetKeyDown(KeyCode.Return))	
			{
				searchField.Select();
				searchField.ActivateInputField();
			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				// Clear any search results
				searchField.text = "";
				ClearSearchResults();
				OrbitCamera.FocusOn(centerTransform, 100f);
			}

			if(Input.GetKeyDown(KeyCode.F12))
				SceneManager.LoadScene(0);

			if(Input.GetKeyDown(KeyCode.Tab))
				forcePhysics = graph.forcePhysics = !graph.forcePhysics;
		}

		public void OnSearchFieldChanged()
		{
			timeSinceLastSearchInputChange = 0f;
			searchInputChanged = true;
		}

		public void OnSearchSubmitted()
		{
			if (!Input.GetKey(KeyCode.Return))
				return;

			if (currentSearchResults.Any() && searchField.text != "" && !clickingOnSearchResult)
			{
				// Drill down into the first result
				var result = currentSearchResults.First();
				DrillDownToEntity(result);
				searchField.text = result.Name;
				searchInputChanged = false;
				DeselectSearchFieldText();
				ClearSearchResults();

				FlyCamera.FocusOn(graph.FindNodeWithEntity(result).representation.transform.position, 10f);
			}
		}

		private void DeselectSearchFieldText()
		{
			searchField.ActivateInputField();
			searchField.Select();

			StartCoroutine(MoveTextEnd_NextFrame());
		}

		// Credit: http://answers.unity3d.com/questions/1103287/how-to-deselect-text-in-an-inputfield.html
		IEnumerator MoveTextEnd_NextFrame()
		{
			yield return 0; // Skip the first frame in which this is called.
			searchField.MoveTextEnd(false); // Do this during the next frame.
		}

		public void UpdateSearchResults()
		{
			ClearSearchResults();

			if (searchField.text == "")
				return;

			var results = new List<Entity>();
			foreach(MongoCollection<Entity> collection in Dataset.GetObjectCollections())
				results.AddRange(SearchFor(collection, searchField.text));

			currentSearchResults = results;
			numSearchResultsText.text = currentSearchResults.Count() == 1 ?
				string.Format("1 result") : string.Format("{0} results", currentSearchResults.Count);

			for (var i = 0; i < Math.Min(searchResultButtons.Length, results.Count()); ++i)
			{
				searchResultButtons[i].gameObject.SetActive(true);
				searchResultButtons[i].GetComponentInChildren<Text>().text = results[i].Name;
				var result = results[i];
				searchResultButtons[i].onClick.AddListener(() =>
				{
					clickingOnSearchResult = true;
					searchField.text = result.Name;
					DrillDownToEntity(result);
					clickingOnSearchResult = false;
					searchInputChanged = false;
					ClearSearchResults();
				});	
			}

			searchInputChanged = false;
		}

		private void DrillDownToEntity(Entity entity)
		{
			var node = graph.FindNodeWithEntity(entity);
			if (node == null)
			{
				node = AddNodeToGraph(entity, GetNodeSettings(entity));
				nodesPendingConnections.Add(node);
//				HandlePendingConnections();
			}

			SelectNode(node);

			nodesPendingConnections.Add(node);
//			HandlePendingConnections();

			RefreshWorldCanvas();
		}

		private void HandlePendingConnections()
		{
			foreach (var node in nodesPendingConnections.ToList())
			{
				var pendingConnectedNodeIDs = node.entity.GetConnectedNodeIDs().ToList();

				foreach (var ID in pendingConnectedNodeIDs.ToList())
				{
					if (graph.nodes.ContainsKey(ID))
					{
//						Debug.Log(string.Format("Found a pending node connection for {0}: {1}", node.entity, graph.nodes[ID].entity));

						graph.AddLink(null, Instantiate(linkPrefab), .1f, node, graph.nodes[ID], 0.2f);
						pendingConnectedNodeIDs.Remove(ID);
					}
				}

				if (!pendingConnectedNodeIDs.Any())
					nodesPendingConnections.Remove(node);
			}
		}

		private void ClearSearchResults()
		{
			foreach (var searchResultButton in searchResultButtons)
			{
				searchResultButton.gameObject.SetActive(false);
				searchResultButton.onClick.RemoveAllListeners();
			}
		}

		private IEnumerable<Entity> SearchFor(MongoCollection<Entity> collection, string query)
		{
			return collection.Find(Query.Matches("name", new BsonRegularExpression(query, "i"))).AsEnumerable();
		}

		public void CreateGraph(List<Dataset.IncludedEntity> entitiesToInclude, bool removeOrphans)
		{
			var addedEntities = new Dictionary<ObjectId, Entity>();
			var timer = Stopwatch.StartNew();
			foreach (var entityType in entitiesToInclude)
			{
				var entities = entityType.entities.Where(entity => !addedEntities.ContainsKey(entity.ID));
				AddNodesToGraph(entities, entityType.scaleProvider, entityType.colorProvider);

				// Add all the non-duplicate entities to add
				foreach (var entity in entities)
					addedEntities.Add(entity.ID, entity);
			}
			Debug.Log("Added all nodes to the graph in " + timer.ElapsedMilliseconds + "ms");

			// Link up all the entities
			timer = Stopwatch.StartNew();
			graph.BeginLinkBatch();

			foreach (var entityType in entitiesToInclude.Where(e => e.relationships != null))
				foreach (var relationship in entityType.relationships)
					AddRelationshipToGraph(entityType.entities, relationship.data, addedEntities);

			graph.EndLinkBatch();
			Debug.Log("Added all relationships to the graph in " + timer.ElapsedMilliseconds + "ms");

			if(removeOrphans)
				foreach (var node in graph.nodes.Values.Where(node => !node.linkedNodes.Any()).ToArray())
					graph.RemoveNode(node);

			simulationStarted = true;

//			FlyCamera.instance.enabled = true;
		}

		private List<GraphNode> AddNodesToGraph(IEnumerable<Entity> entities, Action<GameObject, Entity> scaleProvider, Action<GameObject, Entity> colorProvider)
		{
			var nodes = new List<GraphNode>();

			graph.BeginNodeBatch();

			foreach (var node in entities.Select(entity => AddNodeToGraph(entity, GetNodeSettings(entity))).Where(node => node != null))
			{
				nodes.Add(node);
				nodesPendingConnections.Add(node);

				node.scaleOverrider = scaleProvider;
				node.colorOverrider = colorProvider;
			}

			graph.EndNodeBatch();

//			HandlePendingConnections();

			return nodes;
		}

		private void AddRelationshipToGraph(IEnumerable<Entity> entities, Dataset.RelationshipData relationship, Dictionary<ObjectId, Entity> allEntities)
		{
			foreach (var entity in entities)
			{
				if (relationship.metadata.ContainsKey("ContainsTargetField"))
				{
					var sourceFieldValue = entity[relationship.metadata["ContainsTargetField"]];
					foreach (var otherEntity in allEntities.Values.Where(e => e.Type == relationship.sourceEntity &&
																			e.ContainsKey(relationship.metadata["SourceField"]) &&
																			e[relationship.metadata["SourceField"]] is BsonArray &&
																			(e[relationship.metadata["SourceField"]] as BsonArray).Contains(sourceFieldValue)))
					{
						AddLink(entity, otherEntity);
					}
				}
				else
				{
					var sourceFieldValue = entity[relationship.metadata["SourceField"]];

					if (sourceFieldValue is BsonArray)
						foreach (var element in sourceFieldValue as BsonArray)
							TryAddLink(entity, ObjectId.Parse(element.ToString()), relationship, allEntities);
					else
						TryAddLink(entity, ObjectId.Parse(sourceFieldValue.ToString()), relationship, allEntities);
				}
				
			}
		}

		private void TryAddLink(Entity sourceEntity, ObjectId sourceFieldValue, Dataset.RelationshipData relationship, Dictionary<ObjectId, Entity> allEntities)
		{
			if (!allEntities.ContainsKey(sourceFieldValue))
			{
//				Debug.Log(string.Format("Couldn't find entity with ID {0} in the graph's entities", sourceFieldValue));
				return;
			}

			var targetEntity = allEntities[sourceFieldValue];

			if (targetEntity == null)
			{
				Debug.Log("Couldn't find target entity with ID " + sourceFieldValue);
				return;
			}

			if (targetEntity.Type != relationship.targetEntity)
				return;

			ObjectId? targetFieldValue = null;

			if(relationship.metadata.ContainsKey("TargetField"))
				targetFieldValue = ObjectId.Parse(targetEntity[relationship.metadata["TargetField"]].ToString());

			else if (relationship.metadata.ContainsKey("ContainsTargetField"))
				targetFieldValue = ObjectId.Parse(targetEntity[relationship.metadata["ContainsTargetField"]].ToString());

			if (targetFieldValue.HasValue && sourceFieldValue.Equals(targetFieldValue.Value))
				AddLink(sourceEntity, targetEntity);
		}

		private void AddLink(Entity sourceEntity, Entity targetEntity)
		{
			var startNode = graph.nodes[sourceEntity.ID];
			var endNode = graph.nodes[targetEntity.ID];

//			Debug.Log(string.Format("{0} is linked to {1}", startNode.entity.Name, endNode.entity.Name));
			var settings = GetNodeSettings(startNode.entity);
			graph.AddLink(null, Instantiate(linkPrefab), settings.linkWidth, startNode, endNode, settings.linkDistance);
		}

		public void ClearGraph()
		{
			graph.RemoveAllNodes();
			selectedNode = null;
//			OrbitCamera.FocusOn(centerTransform, 100f);
			simulationStarted = false;
		}
	}

	public class PendingEntities
	{
		public IEnumerable<Entity> entities;
		public IEnumerable<Dataset.RelationshipData> relationships;
	}
}
