using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ForceGraphs.Scripts;
using Assets.Scripts;
using MongoDB.Bson;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets
{
	[Serializable]
	public class ForceGraphPhysicsSettings
	{
		public float repulsionConstant = 1f;
		public float desiredNodeSeperation = 1f;
		public float centerAttractionForce = 1f;
		public float groupRepulsionConstant = 1f;
		public float dampingConstant = 0.9f;
		public float accelerationLimit = 1000;
		public float settleSpeed;
		public float groupAttract;
		public int numUpdatesPerFrame = 1;
	}

	public class ForceGraph<T,U,K> where T : ForceGraph<T,U,K>.IGUID
	{
		public interface IGUID
		{
			K ID { get; }
			IEnumerable<ObjectId> GetConnectedNodeIDs();
		}
		
		// These must match the size and order of the matching structs in the compute shader
		public struct NodeData
		{
			public Vector3 position;
			public Vector3 prevPosition;
			public Vector3 acceleration;
			public Vector3 velocity;
			public Vector3 force;
			public float mass;
			public int group;
			public int isStatic;
		}

		public struct LinkData
		{
			public int a;
			public int b;
			public float attractFactor;
		}

		public class Link
		{
			public LinkData linkData;
			public K startID;
			public K endID;
			public float attractFactor;
			public U entity;
			public GameObject representation;
			public GameObject startRepresentation;
			public GameObject endRepresentation;
			public T startEntity;
			public T endEntity;
			public float width;
			public Func<U, float> widthProvider;
			public Action<GameObject, U> colorOverrider;
			public Node startNode;
			public Node endNode;
		}

		public class Node
		{
			public T entity;
			public NodeData nodeData;
			public GameObject representation;
			public Action<GameObject, T> colorOverrider;
			public Action<GameObject, T> scaleOverrider;
			public ForceGraph<T, U, K> graph;
			public List<Node> linkedNodes = new List<Node>(); 

			public void Freeze()
			{
				// Ugly, but structs should be immutable
				graph.nodes[entity.ID].nodeData = new NodeData
				{
					position = graph.nodes[entity.ID].nodeData.position,
					prevPosition = graph.nodes[entity.ID].nodeData.prevPosition,
					acceleration = graph.nodes[entity.ID].nodeData.acceleration,
					velocity = graph.nodes[entity.ID].nodeData.velocity,
					force = graph.nodes[entity.ID].nodeData.force,
					mass = graph.nodes[entity.ID].nodeData.mass,
					group = graph.nodes[entity.ID].nodeData.group,
					isStatic = 1
				};
			}

			public void Unfreeze()
			{
				// Ugly, but structs should be immutable
				graph.nodes[entity.ID].nodeData = new NodeData
				{
					position = graph.nodes[entity.ID].nodeData.position,
					prevPosition = graph.nodes[entity.ID].nodeData.prevPosition,
					acceleration = graph.nodes[entity.ID].nodeData.acceleration,
					velocity = graph.nodes[entity.ID].nodeData.velocity,
					force = graph.nodes[entity.ID].nodeData.force,
					mass = graph.nodes[entity.ID].nodeData.mass,
					group = graph.nodes[entity.ID].nodeData.group,
					isStatic = 0
				};
			}

			public IEnumerable<Node> GetLinkedNodes()
			{
				return linkedNodes;
			}

			public IEnumerable<Node> GetLinkedNodes<Q>()
			{
				return linkedNodes.Where(n => n.entity is Q);
			}

			public Node GetFirstLinkedNode()
			{
				return linkedNodes.FirstOrDefault();
			}

			public Node GetFirstLinkedNode<Q>()
			{
				return linkedNodes.FirstOrDefault(n => n.entity is Q);
			}
		}

		private struct Group
		{
			public int id;
			public Vector3 center;
		}

		public Dictionary<K, Node> nodes = new Dictionary<K, Node>();
		private readonly List<Link> links = new List<Link>();
		private List<Group> groups = new List<Group>();

		private readonly ComputeShader shader;
		private readonly ForceGraphPhysicsSettings physics;
		private readonly Transform container;
		private bool initialized;

		private ComputeBuffer nodesBuffer;
		private ComputeBuffer nodeLinksBuffer;
		private ComputeBuffer groupsBuffer;
		private bool nodeBatching;
		private bool linkBatching;
		private List<Group> groupCenters;
		public bool debugDrawingEnabled;

		public Vector3 center;
		public float totalKineticEnergy;
		public float averageKineticEnergy;
		private bool allNodesFrozen;
		public bool forcePhysics;

		private const int BLOCK_SIZE = 512;

		public ForceGraph(ComputeShader computeShader, ForceGraphPhysicsSettings physicsSettings, Transform nodeContainer = null)
		{
			shader = computeShader;
			physics = physicsSettings;
			container = nodeContainer;
		}

		public Dictionary<K, Node> Nodes { get { return nodes; } }
		public int NumNodes { get { return nodes.Count; } }
		public int NumStaticNodes { get { return nodes.Count(n => n.Value.nodeData.isStatic == 1); } }

		public void Init()
		{
			initialized = true;

			RebuildNodeBuffer(false);
			RebuildLinksBuffer(false);
			RebuildGroupsBuffers(false);

			SetupComputeShader();
		}

		public Node AddNode(T nodeEntity, GameObject representation, Vector3 position, float mass, int groupID = -1)
		{
//			Debug.Log(string.Format("Adding {0} of type {1}", nodeEntity, nodeEntity.GetType().Name));

			// Don't add nodes that's already exist
			if (nodes.ContainsKey(nodeEntity.ID))
				return null;

			// If the node has a new group
			if (groupID != -1 && groups.All(g => g.id != groupID))
			{
				// Add the group
				groups.Add(new Group{id = groupID});
				RebuildGroupsBuffers();
			}

			// Create the node
			var nodeMapping = new Node
			{
				graph = this,
				entity = nodeEntity,
				representation = representation,
				nodeData = new NodeData
				{
					position = position,
					mass = mass,
					group = groupID,
				}
			};
			nodes.Add(nodeEntity.ID, nodeMapping);

			// Put the representation in the node container
			if(container != null)
				representation.transform.parent = container;

			// Position the representation
			representation.transform.position = position;

			// Name the representation
			representation.name = nodeEntity.ToString();

			// Rebuild the node buffer
			if(!nodeBatching)
				RebuildNodeBuffer();

			return nodeMapping;
		}

		public Link AddLink(U linkEntity, GameObject representation, float width, T startEntity, T endEntity, float? attractFactor = null, bool allowDuplicates = false)
		{
			var startNode = FindNodeWithEntity(startEntity);
			if (startNode == null)
				throw new Exception("Couldn't find a node for the entity " + startEntity);

			var endNode = FindNodeWithEntity(endEntity);
			if (endNode == null)
				throw new Exception("Couldn't find a node for the entity " + endNode);

			return AddLink(linkEntity, representation, width, startNode, endNode, attractFactor, allowDuplicates);
		}

		public Link AddLink(U linkEntity, GameObject representation, float width, Node startNode, Node endNode, float? attractFactor = null, bool allowDuplicates = false)
		{
			if (startNode == null)
			{
				GameObject.Destroy(representation);
				throw new Exception("Node A was null while trying to create a link");
			}

			if (endNode == null)
			{
				GameObject.Destroy(representation);
				throw new Exception("Node B was null while trying to create a link");
			}

			if (!allowDuplicates && links.Any(l => (l.startEntity.Equals(startNode.entity) && l.endEntity.Equals(endNode.entity))
				|| (l.startEntity.Equals(endNode.entity) && l.endEntity.Equals(startNode.entity))))
			{
//				Debug.LogWarning(string.Format("Tried to form a duplicate link between {0} and {1}", startNode, endNode));
				GameObject.Destroy(representation);
				return null;
			}

			// Create the link
			var linkMapping = new Link
			{
				startID = startNode.entity.ID,
				endID = endNode.entity.ID,
				startNode = startNode,
				endNode = endNode,
				entity = linkEntity,
				representation = representation,
				startRepresentation = startNode.representation,
				endRepresentation = endNode.representation,
				startEntity = startNode.entity,
				endEntity = endNode.entity,
				width = width,
				attractFactor = attractFactor.HasValue ? attractFactor.Value : 1f
			};
			links.Add(linkMapping);

			// Keep track of nodes' linked nodes
			startNode.linkedNodes.Add(endNode);
			endNode.linkedNodes.Add(startNode);

			// Rebuild the link buffer
			if(!linkBatching)
				RebuildLinksBuffer();

			return linkMapping;
		}

		private void RebuildNodeBuffer(bool updateShader = true)
		{
			if (nodes.Count == 0)
				return;	

			nodesBuffer = new ComputeBuffer(nodes.Count, sizeof(float) * (3 + 3 + 3 + 3 + 3 + 1) + sizeof(int) * 2, ComputeBufferType.Default);
			nodesBuffer.SetData(nodes.Values.Select(n => n.nodeData).ToArray());

			if(updateShader)
				SetupComputeShader();
		}

		private void RebuildLinksBuffer(bool updateShader = true)
		{
			var linkData = CreateLinkData();

			if (linkData.Length == 0)
				return;

			nodeLinksBuffer = new ComputeBuffer(linkData.Length, sizeof(int) * 2 + sizeof(float), ComputeBufferType.Default);
			nodeLinksBuffer.SetData(linkData);

			if (updateShader)
				SetupComputeShader();
		}

		private LinkData[] CreateLinkData()
		{
			var nodeList = nodes.Values.ToList();

			return links.Select(link => new LinkData
			{
				a = nodeList.IndexOf(link.startNode),
				b = nodeList.IndexOf(link.endNode),
				attractFactor = link.attractFactor
			}).ToArray();
		}

		private void RebuildGroupsBuffers(bool updateShader = true)
		{
			// if (groups.Count == 0)
			// 	return;

			groupsBuffer = new ComputeBuffer(groups.Count, sizeof(int) + sizeof(float) * 3, ComputeBufferType.Default);
			groupsBuffer.SetData(groups.ToArray());

			if (updateShader)
				SetupComputeShader();
		}

		public void Cleanup()
		{
			nodesBuffer.Release();
			nodesBuffer.Dispose();
			nodesBuffer = null;

			nodeLinksBuffer.Release();
			nodeLinksBuffer.Dispose();
			nodeLinksBuffer = null;
		}

		private void SetupComputeShader()
		{
			// Setup the shader data
			var kernelIndex = shader.FindKernel("ForceDirectedGraph");
			if (nodesBuffer != null)
				shader.SetBuffer(kernelIndex, "nodes", nodesBuffer);
			if(nodeLinksBuffer != null)
				shader.SetBuffer(kernelIndex, "nodeLinks", nodeLinksBuffer);
			if (groupsBuffer != null)
				shader.SetBuffer(kernelIndex, "groups", groupsBuffer);
			shader.SetFloat("desiredNodeDistance", physics.desiredNodeSeperation);
			shader.SetFloat("repulsionConstant", physics.repulsionConstant);
			shader.SetFloat("centerAttract", physics.centerAttractionForce);
			shader.SetFloat("groupRepulsionConstant", physics.groupRepulsionConstant);
			shader.SetFloat("damping", physics.dampingConstant);
			shader.SetFloat("maxAccel", physics.accelerationLimit);
			shader.SetFloat("settleSpeed", physics.settleSpeed);
			shader.SetFloat("nodeRelaxationDivisor", 2f);
			shader.SetFloat("groupAttract", physics.groupAttract);
			shader.SetVector("center", Vector3.zero);

			kernelIndex = shader.FindKernel("CalculateNodeCenters");
			if (nodesBuffer != null)
				shader.SetBuffer(kernelIndex, "nodes", nodesBuffer);
			if (groupsBuffer != null)
				shader.SetBuffer(kernelIndex, "groups", groupsBuffer);
		}	

		public void Update(float deltaTime, bool updateRepresentations = true)
		{
			if(!initialized)
				throw new Exception("Tried to update a force graph that hasn't been initialized yet, called Init() first");

			if (allNodesFrozen && !forcePhysics)
				return;

			if(forcePhysics)
				SetupComputeShader();

			// Update the compute shader's delta time
			shader.SetFloat("deltaTime", deltaTime);
			shader.SetInt("numUpdatesPerFrame", physics.numUpdatesPerFrame);

			// Run the compute shader
			var kernelIndex = shader.FindKernel("ForceDirectedGraph");
			var numberOfGroups = Mathf.CeilToInt((float)nodes.Count / BLOCK_SIZE);
			shader.Dispatch(kernelIndex, numberOfGroups, 1, 1);

			// Recalculate all the group centers
			kernelIndex = shader.FindKernel("CalculateNodeCenters");
			shader.Dispatch(kernelIndex, 1, 1, 1);

			if (debugDrawingEnabled)
			{
				// Get the new group data from the GPU
				GetGroupsFromGPU();

				foreach (var group in groups)
					UnityUtil.DrawDebugPoint(group.center, 1, Color.red);

				DrawDebugLinks(new Color(0, 1, 0, 0.25f));
			}

			if(updateRepresentations)
				UpdateRepresentations();
		}

		public void UpdateRepresentations()
		{
			// Get the new node data from the GPU
			GetNodesFromGPU();

			// Update the positions of all the node representations
			UpdateNodeRepresentations();

			// Update the positions and scales of all the link representations
			UpdateLinkRepresentations();
		}

		private void UpdateNodeRepresentations()
		{
			center = Vector3.zero;
			foreach (var node in nodes.Values)
			{
				node.representation.transform.position = nodes[node.entity.ID].nodeData.position;
				center += node.representation.transform.position;

				// Set the node's scale if there is a scale provider
				if (node.scaleOverrider != null)
					node.scaleOverrider(node.representation, node.entity);

				// Allow any color overrider to do its work
				if (node.colorOverrider != null)
					node.colorOverrider(node.representation, node.entity);
			}

			center /= nodes.Count;
		}

		private void UpdateLinkRepresentations()
		{
			foreach (var link in links)
			{
				var linkTransform = link.representation.transform;
				var startTransform = link.startRepresentation.transform;
				var endTransform = link.endRepresentation.transform;

				// Position the link at the midpoint between its start and end nodes
				linkTransform.position = (startTransform.position + endTransform.position) / 2f;

				// Align the link to connect the start and end nodes
				var lookRotation = (startTransform.position - endTransform.position).normalized;
				if (lookRotation != Vector3.zero)
					linkTransform.rotation = Quaternion.LookRotation(lookRotation) *
					Quaternion.AngleAxis(90, new Vector3(0, 0, 1)) * Quaternion.AngleAxis(90, new Vector3(1, 0, 0));

				// Calculate the length of the link
				var length = Vector3.Distance(startTransform.position, endTransform.position) / 2f;

				// Set the link's scale
				var width = link.width;
				if (link.widthProvider != null)
					width = link.widthProvider(link.entity);

				// Allow any color overrider to do its work
				if (link.colorOverrider != null)
					link.colorOverrider(linkTransform.gameObject, link.entity);
				
				linkTransform.localScale = new Vector3(width, length, width);
			}
		}

		public void DrawDebugLinks(Color color)
		{
			foreach (var link in links)
				Debug.DrawLine(nodes[link.startID].nodeData.position, nodes[link.endID].nodeData.position, color);
		}

		private void GetNodesFromGPU()
		{
			var array = new NodeData[nodes.Count];
			nodesBuffer.GetData(array);
			totalKineticEnergy = averageKineticEnergy = 0;
			for (var i = 0; i < array.Length; ++i)
			{
				nodes[nodes.Keys.ElementAt(i)].nodeData = array[i];
				totalKineticEnergy += .5f * array[i].mass * Mathf.Pow(array[i].velocity.magnitude, 2);
			}
			averageKineticEnergy = totalKineticEnergy / nodes.Count;
		}

		private void GetGroupsFromGPU()
		{
			var array = new Group[groups.Count];
			groupsBuffer.GetData(array);
			groups = array.ToList();
		}

		public void BeginNodeBatch()
		{
			if (nodeBatching)
			{
				Debug.LogError("Tried to start another node batch add while already batch adding, use EndNodeBatch() first.");
				return;
			}

			nodeBatching = true;
		}

		public void EndNodeBatch()
		{
			if (!nodeBatching)
			{
				Debug.LogError("Tried to end a node batch without starting one, use BeginNodeBatch() first.");
				return;
			}

			RebuildNodeBuffer();
			nodeBatching = false;
		}

		public void BeginLinkBatch()
		{
			if (linkBatching)
			{
				Debug.LogError("Tried to start another link batch add while already batch adding, use EndLinkBatch() first.");
				return;
			}

			linkBatching = true;
		}

		public void EndLinkBatch()
		{
			if (!linkBatching)
			{
				Debug.LogError("Tried to end a link batch without starting one, use BeginLinkBatch() first.");
				return;
			}

			RebuildLinksBuffer();
			linkBatching = false;
		}

		public void FreezeAllNodes()
		{
			if (allNodesFrozen)
				return;

			foreach(var node in nodes.Values)
				node.Freeze();
			RebuildNodeBuffer();

			allNodesFrozen = true;
		}

		public void UnfreezeAllNodes()
		{
			if (!allNodesFrozen)
				return;

			foreach (var node in nodes.Values)
				node.Unfreeze();
			RebuildNodeBuffer();

			allNodesFrozen = false;
		}

		public void ForcePhysicsSettingsUpdate()
		{
			SetupComputeShader();
		}

		public List<Node> GetUnlinkedNodes()
		{
			var unlinkedNodes = nodes.Values.Where(n => !n.linkedNodes.Any()).ToList();

			Debug.Log("Found " + unlinkedNodes.Count + " unlinked nodes");

			return unlinkedNodes;
		}

		public void RemoveNodes(IEnumerable<Node> nodesToRemove)
		{
			foreach (var node in nodesToRemove.ToArray())
				RemoveNode(node, false);

			RebuildNodeBuffer();
			RebuildLinksBuffer();
		}

		public void RemoveNode(Node nodeToRemove, bool rebuildBuffers = true)
		{
			nodes.Remove(nodeToRemove.entity.ID);
			Object.DestroyImmediate(nodeToRemove.representation);

			foreach (var link in links.Where(l => l.startEntity.Equals(nodeToRemove.entity) || l.endEntity.Equals(nodeToRemove.entity)).ToArray())
			{
				links.Remove(link);
				Object.DestroyImmediate(link.representation);

				if (link.startNode != null)
					link.startNode.linkedNodes.Remove(link.endNode);

				if (link.endNode != null)
					link.endNode.linkedNodes.Remove(link.startNode);
			}

			if (rebuildBuffers)
			{
				RebuildNodeBuffer();
				RebuildLinksBuffer();
			}
		}

		public void RemoveAllNodes()
		{
			foreach(var node in nodes.Values.ToArray())
				RemoveNode(node, false);

			RebuildNodeBuffer();
			RebuildLinksBuffer();

			allNodesFrozen = false;
		}

		public void RemoveUnlinkedNodes()
		{
			foreach (var unlinkedNode in GetUnlinkedNodes())
				RemoveNode(unlinkedNode);
		}

		public Node GetRandomNode<Q>()
		{
			return Util.RandomElement(nodes.Values.Where(n => n.entity is Q));
		}

		public Node FindNodeWithEntity(T entity)
		{
			return nodes.Values.FirstOrDefault(n => n.entity.ID.Equals(entity.ID));
		}
	}
}