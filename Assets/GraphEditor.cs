using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.ForceGraphs.Scripts;
using Assets.Scripts;
using Assets.Scripts.Cameras;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

namespace Assets
{
	public class GraphEditor : MonoBehaviour
	{
		public static GraphEditor instance;

		private MongoClient client;
		public GameObject objectPrefab;
		public Transform objectContainer;
		private readonly HashSet<EditorNode> selectedNodes = new HashSet<EditorNode>();
		private readonly List<Vector2> selectionBoxPoints = new List<Vector2>();
		private VectorLine selectionBox;
		private Vector2 selectionBoxStart;
		public Color32 selectionBoxColor;
		public float selectionBoxWidth = 2f;
		private readonly List<EditorNode> nodes = new List<EditorNode>();
		private VectorLine newWire;
		public DraggableNode wireStartNode;
		public Color32 invalidWireColor;
		public Color32 validWireColor;
		public Color32 unknownWireColor;
		public float wireWidth;
		public Texture wireTexture;
		public DraggableNode objectUnderMouse;
		private readonly List<Wire> wires = new List<Wire>();
		public RectTransform toolboxRect;
		public GameObject relationshipDropdownPrefab;
		public GameObject returnToEditorButton;
		private GameObject vectorCanvas;
		public GameObject searchContainer;
		public GameObject detailsPanel;
		public Texture2D lineTex;
		public Texture2D frontTex;
		public Texture2D backTex;
		public float wireEndOffset = 100f;
		public ScrollRect toolboxScroll;
		public Transform workspaceContainer;
		private readonly Dictionary<MongoCollection<Entity>, Dictionary<string, double>> cachedCollectionTotals = new Dictionary<MongoCollection<Entity>, Dictionary<string, double>>();
		private readonly Dictionary<MongoCollection<Entity>, Dictionary<string, double>> cachedCollectionMins = new Dictionary<MongoCollection<Entity>, Dictionary<string, double>>();
		private readonly Dictionary<MongoCollection<Entity>, Dictionary<string, double>> cachedCollectionMaxs = new Dictionary<MongoCollection<Entity>, Dictionary<string, double>>();
		public static bool CreatingWire { get { return instance.newWire != null; } }
		public Toggle removeOrphansToggle;
		public static Action<EditorNode> onEditorNodeAdded;

		public void Start ()
		{
			instance = this;

			// Create the selection box
			for(var i = 0; i < 5; ++i)
				selectionBoxPoints.Add(new Vector2());

			selectionBox = new VectorLine("Selection Box", selectionBoxPoints, wireTexture, selectionBoxWidth, LineType.Continuous)
			{
				active = false
			};
			selectionBox.SetColor(selectionBoxColor);

			VectorLine.SetEndCap("Arrow", EndCap.Both, lineTex, frontTex, backTex);

			toolboxScroll.scrollSensitivity = 5f;

			// Populate the toolbox
			PopulateToolboxObjects();

//			FlyCamera.instance.enabled = false;

			// HACK: Allows me to dismiss the development console early when demoing
			throw new Exception("Ignore this exception, it's to allow for early closing of the development console on demo builds");
		}

		private void PopulateToolboxObjects()
		{
			foreach (var objectType in Dataset.GetTypes())
			{
				var collectionObject = Instantiate(objectPrefab);	
				collectionObject.transform.SetParent(objectContainer);
				var editorNode = collectionObject.GetComponent<EditorNode>();
				editorNode.SetCollection(Dataset.GetCollectionByName(objectType));
				editorNode.objectType = objectType;
			}
		}

		public static void AddToSelection(DraggableNode draggableNode)
		{
			if (draggableNode == null)
				return;

			if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
				ClearSelection();

			var graphNode = draggableNode.GetComponent<EditorNode>();
			if (graphNode != null)
			{
				instance.selectedNodes.Add(graphNode);
				graphNode.OnSelected();
			}
		}

		private void AddToSelection(EditorNode graphNode)
		{
			if (graphNode == null)
				return;

			instance.selectedNodes.Add(graphNode);
			graphNode.OnSelected();
		}

		public void OnBackgroundClicked()
		{
			ClearSelection();
		}

		public static void ClearSelection()
		{
			foreach (var node in instance.selectedNodes)
				node.OnDeselected();

			instance.selectedNodes.Clear();
		}

		public void OnSelectionBoxStarted()
		{
			selectionBoxStart = Input.mousePosition;
			selectionBox.active = true;

			UpdateSelectionBox();
		}

		private void UpdateSelectionBox()
		{
			// Top left
			selectionBox.points2[0] = selectionBoxStart;

			// Top right
			selectionBox.points2[1] = new Vector3(Input.mousePosition.x, selectionBoxStart.y);

			// Bottom right
			selectionBox.points2[2] = Input.mousePosition;

			// Bottom left
			selectionBox.points2[3] = new Vector3(selectionBoxStart.x, Input.mousePosition.y);

			// Close the loop
			selectionBox.points2[4] = selectionBoxStart;

			// Draw the selection box
			selectionBox.Draw();
		}

		public void OnSelectionBoxUpdated()
		{
			UpdateSelectionBox();
		}

		public void OnSelectionBoxEnded()
		{
			selectionBox.active = false;

			// Create the selection rect
			var selectionRect = UnityUtil.CreateRectFromPoints(selectionBoxStart, Input.mousePosition);

			// Select any nodes contained in the selection
			foreach(var node in nodes)
				if(selectionRect.Contains(node.transform.position))
					AddToSelection(node);
		}

		public void AddNewNode(DraggableNode draggableNode)
		{
			var graphNode = draggableNode.GetComponent<EditorNode>();
			if (graphNode != null)
			{
				nodes.Add(graphNode);
				if (onEditorNodeAdded != null)
					onEditorNodeAdded(graphNode);
			}
		}

		public void OnNodeDragged(Vector2 delta)
		{
			foreach (var node in selectedNodes)
				node.transform.Translate(delta);
		}

		public void OnStartWire(DraggableNode draggableNode)
		{
			wireStartNode = draggableNode;

			newWire = new VectorLine("New Wire", new List<Vector2>
			{
				Input.mousePosition, draggableNode.transform.position
			}, wireTexture, wireWidth);
			newWire.endCap = "Arrow";
			newWire.SetColor(unknownWireColor);
			newWire.Draw();
			GameObject.Find("VectorCanvas").GetComponent<Canvas>().sortingOrder = -1;
		}

		public void OnUpdateWire()
		{
			newWire.points2[0] = Input.mousePosition;
			newWire.points2[1] = wireStartNode.transform.position;
			newWire.Draw();
		}

		public void OnEndWire()
		{
			VectorLine.Destroy(new []{newWire});
			newWire = null;

			if (objectUnderMouse != null)
			{
				if (GetPossibleRelationships(wireStartNode.GetComponent<EditorNode>().objectType,
											objectUnderMouse.GetComponent<EditorNode>().objectType).Any())
					ConnectNodes(wireStartNode.GetComponent<EditorNode>(), objectUnderMouse.GetComponent<EditorNode>());
			}
		}

		private void ConnectNodes(EditorNode startNode, EditorNode endNode)
		{
			if (startNode == endNode)
			{
				Debug.LogWarning("Tried to connect a node to itself");
				return;
			}

			// TODO: Allow more than one relationship between entities
			if (wires.Any(w => (w.start == startNode && w.end == endNode) || (w.end == startNode && w.start == endNode)))
			{
				Debug.LogWarning("Tried to connect more then one connection between two nodes");
				return;
			}

			Debug.Log(string.Format("Creating wire between {0} and {1}", startNode.name, endNode.name));

			var line = new VectorLine(string.Format("{0} > {1}", startNode.name, endNode.name),
					new List<Vector2>
					{
						startNode.transform.position,
						endNode.transform.position
					}, wireTexture, wireWidth);
			line.endCap = "Arrow";
			line.SetColor(validWireColor);
			line.Draw();

			// Add the relationship dropdown 
			var dropdownObject = Instantiate(relationshipDropdownPrefab);
			dropdownObject.transform.SetParent(startNode.transform);
			var dropdown = dropdownObject.GetComponent<Dropdown>();
			dropdown.options = new List<Dropdown.OptionData>();
			var possibleRelationships = GetPossibleRelationships(startNode.objectType, endNode.objectType);
			foreach (var relationship in possibleRelationships)
			{
				if (string.IsNullOrEmpty(dropdown.captionText.text))
					dropdown.captionText.text = relationship.Key;
				dropdown.options.Add(new Dropdown.OptionData(relationship.Key));
			}

			// Create the permenant wire
			var wire = new Wire
			{
				start = startNode,
				end = endNode,
				line = line,
				dropdownObject = dropdownObject
			};
			wires.Add(wire);
			wire.Update();
			startNode.wires.Add(wire);
			endNode.wires.Add(wire);

			// Handle deleting the connection
			dropdownObject.GetComponentInChildren<Button>().onClick.AddListener(() =>
			{
				Destroy(dropdownObject.gameObject);

				wire.start.wires.Remove(wire);
				wire.end.wires.Remove(wire);

				instance.wires.Remove(wire);
				Destroy(wire.dropdownObject);

				VectorLine.Destroy(new []{wire.line});

				startNode.resultsDirty = true;
				endNode.resultsDirty = true;

				startNode.UpdateQueries();
				endNode.UpdateQueries();
			});

			startNode.resultsDirty = true;
			endNode.resultsDirty = true;

			startNode.UpdateQueries();
			endNode.UpdateQueries();
		}

		public IEnumerable<KeyValuePair<string, Dataset.RelationshipData>> GetPossibleRelationships(string startType, string endType)
		{
			var possibleRelationships = Dataset.instance.datasetSpec.relationships.Where(r => r.Value.start == startType && r.Value.end == endType);
			Debug.Log(string.Format("Possible relationships: {0}", possibleRelationships.Count()));
			return possibleRelationships;
		}

		public void Update()
		{
			HandleKeys();

			foreach (var wire in wires)
				wire.Update();
		}

		private void HandleKeys()
		{
			if (selectedNodes.Any() && Input.GetKeyDown(KeyCode.Delete))
				DeleteSelection();

			if(Input.GetKeyDown(KeyCode.KeypadEnter))
				ExecuteGraph();
		}

		private void DeleteSelection()
		{
			foreach (var node in selectedNodes)
				DeleteNode(node);

			selectedNodes.Clear();
		}

		public static void DeleteNode(EditorNode node)
		{
			instance.nodes.Remove(node);

			var wiresToRemove = instance.wires.Where(w => w.start == node || w.end == node).ToArray();

			foreach (var wire in wiresToRemove)
			{
				wire.start.wires.Remove(wire);
				wire.end.wires.Remove(wire);

				instance.wires.Remove(wire);
				Destroy(wire.dropdownObject);
			}

			VectorLine.Destroy(wiresToRemove.Select(w => w.line).ToArray());

			Destroy(node.gameObject);
		}

		public void ExecuteGraph()
		{
			// Reset the camera
//			FlyCamera.FocusOn(Vector3.zero, 100);

			var entitiesToInclude = new List<Dataset.IncludedEntity>();
			foreach (var node in nodes)
			{
				var entityToInclude = new Dataset.IncludedEntity
				{
					entities = node.GetResults(),
					relationships = node.GetRelationships(),
				};

				if (node.scaleDropdown.value > 0)
				{
					var nodeClosure = node;
					entityToInclude.scaleProvider = (go, entity) =>
					{
						var metrics = Entity.GetMetrics(entity.Type).ToArray();
						var metric = metrics[nodeClosure.scaleDropdown.value - 1];
						var metricValue = entity[metric.fieldName].ToDouble();
						var minValue = GetCollectionMin(entity.collection, metric.fieldName);
						var maxValue = GetCollectionMax(entity.collection, metric.fieldName);
						var settings = Graph.instance.GetNodeSettings(entity);

						go.transform.localScale = Vector3.one * Mathf.Lerp(settings.minScale, settings.maxScale, (float)((metricValue - minValue) / maxValue));
					};
				}
				else
				{
					entityToInclude.scaleProvider = (go, entity) =>
					{
						var settings = Graph.instance.GetNodeSettings(entity);

						go.transform.localScale = Vector3.one * (settings.minScale + settings.maxScale) / 2f;
					};
				}

				if (node.colorDropdown.value > 0)
				{
					var nodeClosure = node;
					entityToInclude.colorProvider = (go, entity) =>
					{
						var metric = Entity.GetMetrics(entity.Type).ToArray()[nodeClosure.colorDropdown.value - 1];
						var metricValue = entity[metric.fieldName].ToDouble();
						var minValue = GetCollectionMin(entity.collection, metric.fieldName);
						var maxValue = GetCollectionMax(entity.collection, metric.fieldName);
						var settings = Graph.instance.GetNodeSettings(entity);

						go.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.Lerp(settings.minColor, settings.maxColor, (float)((metricValue - minValue) / maxValue)));
					};
				}
				else
				{
					entityToInclude.colorProvider = (go, entity) =>
					{
						var settings = Graph.instance.GetNodeSettings(entity);
						go.GetComponent<MeshRenderer>().material.SetColor("_Color", settings.color);
					};
				}

				entitiesToInclude.Add(entityToInclude);
			}

			Debug.Log("Number of entity types to include: " + entitiesToInclude.Count);

			// Create the graph!
			Graph.instance.CreateGraph(entitiesToInclude, removeOrphansToggle.isOn);

			gameObject.SetActive(false);
			vectorCanvas = GameObject.Find("VectorCanvas");
			if(vectorCanvas != null)
				vectorCanvas.SetActive(false);

			searchContainer.SetActive(true);
			removeOrphansToggle.gameObject.SetActive(false);
			returnToEditorButton.SetActive(true);

			StockDemoController.OnGraphExecute();
		}

		private double GetCollectionTotal(MongoCollection<Entity> collection, string fieldName)
		{
			if (cachedCollectionTotals.ContainsKey(collection) && cachedCollectionTotals[collection].ContainsKey(fieldName))
				return cachedCollectionTotals[collection][fieldName];

			// I still have no fucking clue how this works
			var totalResults = collection.Aggregate(new AggregateArgs
			{
				Pipeline = new[]
				{
					new BsonDocument 
					{ 
						{
							"$group", 
							new BsonDocument 
							{ 
								{ "_id", 0 },
								{ 
									"Total", new BsonDocument 
									{ 
										{ 
											"$sum", "$" + fieldName 
										} 
									} 
								} 
							} 
						} 
					}
				}
			});

			var result = totalResults.First()["Total"].ToDouble();
			if (!cachedCollectionTotals.ContainsKey(collection))
				cachedCollectionTotals[collection] = new Dictionary<string, double>();
			cachedCollectionTotals[collection][fieldName] = result;

			return result;
		}

		private double GetCollectionMin(MongoCollection<Entity> collection, string fieldName)
		{
			if (cachedCollectionMins.ContainsKey(collection) && cachedCollectionMins[collection].ContainsKey(fieldName))
				return cachedCollectionMins[collection][fieldName];

			// I still have no fucking clue how this works
			var totalResults = collection.Aggregate(new AggregateArgs
			{
				Pipeline = new[]
				{
					new BsonDocument 
					{ 
						{
							"$group", 
							new BsonDocument 
							{ 
								{ "_id", 0 },
								{ 
									"Min", new BsonDocument 
									{ 
										{ 
											"$min", "$" + fieldName 
										} 
									} 
								} 
							} 
						} 
					}
				}
			});

			var result = totalResults.First()["Min"].ToDouble();
			if (!cachedCollectionMins.ContainsKey(collection))
				cachedCollectionMins[collection] = new Dictionary<string, double>();
			cachedCollectionMins[collection][fieldName] = result;

			return result;
		}

		private double GetCollectionMax(MongoCollection<Entity> collection, string fieldName)
		{
			if (cachedCollectionMaxs.ContainsKey(collection) && cachedCollectionMaxs[collection].ContainsKey(fieldName))
				return cachedCollectionMaxs[collection][fieldName];

			// I still have no fucking clue how this works
			var totalResults = collection.Aggregate(new AggregateArgs
			{
				Pipeline = new[]
				{
					new BsonDocument
					{
						{
							"$group",
							new BsonDocument
							{
								{ "_id", 0 },
								{
									"Max", new BsonDocument
									{
										{
											"$max", "$" + fieldName
										}
									}
								}
							}
						}
					}
				}
			});

			var result = totalResults.First()["Max"].ToDouble();
			if (!cachedCollectionMaxs.ContainsKey(collection))
				cachedCollectionMaxs[collection] = new Dictionary<string, double>();
			cachedCollectionMaxs[collection][fieldName] = result;

			return result;
		}

		public void SaveGraph()
		{
			Debug.Log("Saving graph");

			File.WriteAllText("test_save.json", JsonConvert.SerializeObject(GetSerializedGraphData(), Formatting.Indented, new JsonSerializerSettings
			{
				ContractResolver = new WritablePropertiesOnlyResolver(),
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			}));
		}

		private EditorGraphData GetSerializedGraphData()
		{
//			asdf
				// Add filters to nodes
				// Add types of relationships

			return new EditorGraphData
			{
				nodes = nodes.Select(n => new EditorGraphData.EditorNodeData
				{
					id = n.id,
					objectType = n.objectType,
					position = n.transform.position,
					filters = n.GetFilters()
				}).ToArray(),

				links = wires.Select(w => new EditorGraphData.EditorLinkData
				{
					start = w.start.id,
					end = w.end.id
				}).ToArray()

			};
		}

		public void LoadGraph()
		{
			var editorGraphData = new EditorGraphData();
			Util.PopulateFromJSONObject(editorGraphData, "test_save.json");

			foreach (var node in editorGraphData.nodes)
			{
				node.editorNode = CreateNode(node.objectType, node.position);

				foreach (var savedFilter in node.filters)
				{
					var filter = node.editorNode.AddFilter(savedFilter.entityFilter);
					filter.SetValue(savedFilter);
				}

				node.editorNode.resultsDirty = true;
				node.editorNode.UpdateQueries();
				nodes.Add(node.editorNode);
			}

			foreach (var link in editorGraphData.links.ToArray())
				ConnectNodes(editorGraphData.nodes.FirstOrDefault(n => n.id == link.start).editorNode,
					editorGraphData.nodes.FirstOrDefault(n => n.id == link.end).editorNode);

			// HACK: I'm not sure why the canvas behaves differently during loading
			GameObject.Find("VectorCanvas").GetComponent<Canvas>().sortingOrder = -1;
		}

		private EditorNode CreateNode(string objectType, Vector2 position)
		{
			var nodeObject = Instantiate(objectPrefab);
			nodeObject.transform.position = position;
			nodeObject.transform.SetParent(workspaceContainer);

			var draggableObject = nodeObject.GetComponent<DraggableNode>();
			draggableObject.clone = false;

			var editorNode = nodeObject.GetComponent<EditorNode>();
			editorNode.objectType = objectType;
			editorNode.SetCollection(Dataset.GetCollectionByName(objectType));

			return editorNode;
		}

		public void OnBeginNodeDrag(EditorNode node)
		{
			if(!selectedNodes.Contains(node))
				ClearSelection();
		}

		public void ShowEditor()
		{
			Graph.instance.ClearGraph();
			searchContainer.SetActive(false);
			detailsPanel.SetActive(false);
			gameObject.SetActive(true);
			returnToEditorButton.SetActive(false);
			removeOrphansToggle.gameObject.SetActive(true);

			if (vectorCanvas != null)
				vectorCanvas.SetActive(true);

			FlyCamera.instance.enabled = false;
		}

		public void OnValidWireEndpoint()
		{
			if(newWire != null)
				newWire.SetColor(validWireColor);
		}

		public void OnInvalidWireEndpoint()
		{
			if (newWire != null)
				newWire.SetColor(invalidWireColor);
		}

		public void OnUnknownWireEndpoint()
		{
			if (newWire != null)
				newWire.SetColor(unknownWireColor);
		}

		public static void UpdateAllQueries()
		{
			instance.StartCoroutine(InternalUpdateAllQueries());
		}

		private static IEnumerator InternalUpdateAllQueries()
		{
			yield return new WaitForEndOfFrame();

			foreach (var node in instance.nodes)
				node.UpdateQueries();
		}
	}

	internal class EditorGraphData
	{
		public class EditorNodeData
		{
			public int id;
			public string objectType;
			public Vector2 position;

			[JsonIgnore] public EditorNode editorNode;
			public IEnumerable<Dataset.Filter> filters;
		}

		public class EditorLinkData
		{
			public int start;
			public int end;
		}

		public EditorNodeData[] nodes;
		public EditorLinkData[] links;
	}
}
