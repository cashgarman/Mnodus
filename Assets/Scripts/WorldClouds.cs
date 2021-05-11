using UnityEngine;

namespace Assets.Scripts
{
	public class WorldClouds : MonoBehaviour
	{
		public BoxCollider bounds;
		public int numClouds;
		public GameObject worldCloudPrefab;
		public float minCloudSize;
		public float maxCloudSize;

		public void Start()
		{
			// For each world cloud to create
			for (var i = 0; i < numClouds; ++i)
			{
				// Create the world cloud object
				var cloudObject = Instantiate(worldCloudPrefab);

				// Position the world cloud
//				cloudObject.transform.position = new Vector3(Random.Range(-bounds.size.x, bounds.size.x),
//					Random.Range(-bounds.size.y, bounds.size.y),
//					Random.Range(-bounds.size.z, bounds.size.z));
				cloudObject.transform.position = Random.onUnitSphere * bounds.size.x;

				// Give the cloud a random scale
				cloudObject.transform.localScale *= Random.Range(minCloudSize, maxCloudSize);
			}
		}
	}
}
