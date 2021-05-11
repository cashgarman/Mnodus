using System;
using System.Linq;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using GraphNode = Assets.ForceGraph<Assets.Scripts.Entity, Assets.Scripts.Relationship, MongoDB.Bson.ObjectId>.Node;
using Random = UnityEngine.Random;

namespace Assets
{
	public class StockDemoController : MonoBehaviour
	{
		public static StockDemoController instance;

		public static float timeDilation;
		public float maxTimeRate = 1f;
		public Text timeText;
		private DateTime currentTime;
		public float stockChangeRate = 0.01f;
		private float currentTimeSeconds;

		public void Awake()
		{
			instance = this;
		}

		public void Start()
		{
			currentTime = DateTime.Now.Subtract(TimeSpan.FromDays(1.0));
		}

		public static void OnGraphExecute()
		{
			foreach (var company in Graph.graph.nodes.Where(_node => _node.Value.entity.Type == "Company"))
			{
				company.Value.entity["share_value"] = 0.5f;
				company.Value.entity["stock_change_rate"] = Random.Range(0.5f, 1.5f);
				company.Value.entity["stock_offset"] = Random.Range(0f, 1f);
			}

			// HACK: DO THIS PROPERLY
			foreach (var node in Graph.graph.nodes.Where(_node => _node.Value.entity.Type == "Company"))
			{
				node.Value.colorOverrider = (n, entity) =>
				{
					var cycle = Mathf.Sin(instance.currentTimeSeconds * ((float) entity["stock_change_rate"].ToDouble() + (float)entity["stock_offset"].ToDouble()));
					entity["share_value"] = Mathf.Lerp(0.25f, .75f, cycle);
					var newColor = Color.Lerp(Color.red, Color.green, (float) entity["share_value"].ToDouble());
					n.GetComponent<Renderer>().material.color = newColor;
				};
			}
		}

		public void Update()
		{
			//var leftTrigger = SteamVR_Controller.devices[SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost)].GetAxis(EVRButtonId.k_EButton_SteamVR_Trigger).x;
			//var rightTrigger = SteamVR_Controller.devices[SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost)].GetAxis(EVRButtonId.k_EButton_SteamVR_Trigger).x;

			//timeDilation = leftTrigger * -1f + rightTrigger;

			if (timeDilation != 0f)
			{
				var difference = maxTimeRate * timeDilation;
				currentTimeSeconds += difference;
				currentTime = currentTime.Add(TimeSpan.FromMinutes(difference));

//				UpdateStockValues();
			}

			timeText.text = currentTime.ToString();
		}
	}
}
