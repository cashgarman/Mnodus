using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ForceGraphs.Scripts;
using MongoDB.Bson;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Assets
{
	public class PopulateForceGraph : MonoBehaviour
	{
		private ForceGraph<Employee, Relationship, Guid> graph;

		public bool showLinks = true;
		public float spawnDistance = 1f;
		private bool paused;

		public int numPeople = 100;
		public float containerSize = 10f;
		private readonly List<Employee> people = new List<Employee>();

		public Text fpsText;
		public Text numNodesText;
		public Text kineticEnergyText;
		public ComputeShader computeShader;
		public GameObject nodePrefab;
		public GameObject linkPrefab;
		public ForceGraphPhysicsSettings physicsSettings;
		public float nodeMass;
		public bool prewarmNodes;
		public float numPrewarmFrames;
		public float linkWidth = 1f;
		private Color[] ageGroupColors =
		{
			Color.red,
			Color.green,
			Color.yellow,
			Color.blue,
			Color.white,
		};

		private float timeSinceVisualUpdate = Mathf.Infinity;
		public float visualUpdateFramerate = 10f;

		public class Employee : ForceGraph<Employee, Relationship, Guid>.IGUID
		{
			public Guid id;
			public string name;
			public int ageGroup;

			public override string ToString()
			{
				return name;
			}

			public Guid ID { get { return id; } }

			public IEnumerable<ObjectId> GetConnectedNodeIDs()
			{
				return null;
			}
		}

		public class Relationship
		{
			public string type;
			public Employee a;
			public Employee b;

			public override string ToString()
			{
				return type;
			}
		}

		public void Awake()
		{
			// Create the force graph
			graph = new ForceGraph<Employee, Relationship, Guid>(computeShader, physicsSettings);
			graph.Init();

			// Load the dataset
			GeneratePeople();

			graph.BeginNodeBatch();
			foreach (var person in people)
			{
				var nodeObject = Instantiate(nodePrefab);
				var node = graph.AddNode(person, nodeObject, Random.insideUnitSphere * containerSize, nodeMass, person.ageGroup);

				if (node != null)
				{
//					node.scaleOverrider = (gameObject, p) => { gameObject.transform.localScale = Vector3.one * (p.ageGroup + 1f); };
//					node.colorOverrider = (gameObject, p) => gameObject.GetComponent<MeshRenderer>().material.SetColor("_TintColor", ageGroupColors[p.ageGroup]);
				}
			}
			graph.EndNodeBatch();

			graph.BeginLinkBatch();
			foreach (var person in people)
			{
				var linkObject = Instantiate(linkPrefab);
				var otherPerson = Util.RandomElement(people.Where(p => p != person && (person.ageGroup == p.ageGroup || Util.Chance(0.1f))));
				var link = graph.AddLink(new Relationship{type = Random.Range(0, 2) == 1 ? "Friend" : "Enemy", a = person, b = otherPerson}, linkObject, linkWidth, person, otherPerson);

				if(link != null)
				{
					link.widthProvider = relationship => (relationship.a.ageGroup + relationship.b.ageGroup + 2) / 5f;
//					link.colorOverrider = (gameObject, relationship) =>
//					{
//						gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", relationship.type == "Friend" ? Color.green : Color.red);
//					};
				}
				else
					Destroy(linkObject);
			}
			graph.EndLinkBatch();

			if(prewarmNodes)
				for(var i = 0; i < numPrewarmFrames; ++i)
					graph.Update(Time.fixedDeltaTime);
		}

		private void GeneratePeople()
		{
			for (var i = 0; i < numPeople; ++i)
				people.Add(new Employee { id = Guid.NewGuid(), name = "Bob", ageGroup = Random.Range(0, 5) });
		}

		public void Update()
		{
			if (Input.GetKeyDown(KeyCode.Return))
				AddNodes(100);

			if (Input.GetKeyDown(KeyCode.P))
				paused = !paused;

			fpsText.text = string.Format("FPS: {0:F1}", 1f / Time.deltaTime);
			numNodesText.text = string.Format("Nodes: {0}", graph.NumNodes);
			kineticEnergyText.text = string.Format("Energy: {0}", graph.totalKineticEnergy);

			if (!paused)
			{
				graph.Update(Time.deltaTime * (Input.GetKey(KeyCode.F) ? 2f : 1f));

				timeSinceVisualUpdate += Time.deltaTime;
				if (timeSinceVisualUpdate >= 1f / visualUpdateFramerate)
				{
					graph.UpdateRepresentations();
					timeSinceVisualUpdate = 0f;
				}
			}
		}

		public void FixedUpdate()
		{
				
		}

		private void AddNodes(int number)
		{
			graph.BeginNodeBatch();
			graph.BeginLinkBatch();

			for (var i = 0; i < number; ++i)
			{
				var person = new Employee {name = "Bob", ageGroup = Random.Range(0, 5)};
				people.Add(person);

				var nodeObject = Instantiate(nodePrefab);
				nodeObject.GetComponent<MeshRenderer>().material.SetColor("_TintColor", Color.green);
				graph.AddNode(person, nodeObject, Random.insideUnitSphere * containerSize, nodeMass, person.ageGroup);

				var otherPerson = Util.RandomElement(people.Where(p => p != person && (person.ageGroup == p.ageGroup || Util.Chance(0.1f))));
				if(graph.AddLink(new Relationship { type = "Friend" }, Instantiate(linkPrefab), linkWidth, person, otherPerson) == null)
					Destroy(nodeObject);
			}

			graph.EndLinkBatch();
			graph.EndNodeBatch();

//			graph.UnfreezeAllNodes();
		}

		public void OnDestroy()
		{
			graph.Cleanup();
		}
	}
}
