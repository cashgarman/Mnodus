using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
	public class Globe : MonoBehaviour
	{
		private static Globe instance;

		public struct Path
		{
			public Vector3[] vertices;
		}

		public class City
		{
			public string name;
			public float latitude;
			public float longitude;
			public Vector3 position;
			public string countryCode;
			public int population;
		}

		public string mapName;
		public string citiesName;
		public Path[] paths;
		public City[] cities;
		public double globeRadius = 10.0;
		public int maxCities = -1;
		public GameObject cityPrefab;
		public bool isGlobe;
		public float mapWidth;
		public float mapHeight;
		public GameObject globeMesh;
		public float borderWidth;
		public float sourceMapWidth;
		public float sourceMapHeight;
		public Material borderlineMaterial;
		public Material gridMaterial;

		private float citySurfaceOffset = 1f;
		private float minCityPopulation = 100000f;
		private int maxCityPopulation;
		private float minCitySize = 0.1f;
		private float maxCitySize = 1f;
		private float xMin;
		private float xMax;
		private float yMin;
		private float yMax;

		public void Awake()
		{
			if (instance == null)
				instance = this;
		}

		public void Start()
		{
			// Get all the points from the map
			GetMapPaths(Resources.Load<TextAsset>(mapName).text);

			// Map the points onto a 3D globe
			CreateMap();

			// Load the cities
//			LoadCities();

			// Add the cities to the globe
//			AddCitiesToGlobe();
		}

		private void GetMapPaths(string mapData)
		{
			// Split the map into paths
			var pathsData = mapData.Split('|');
			Debug.Log("Found " + pathsData.Length + " paths");

			paths = new Path[pathsData.Length];
			for (var i = 0; i < pathsData.Length; ++i)
			{
				var verticesData = pathsData[i].Split('\n');
				paths[i] = new Path
				{
					vertices = new Vector3[verticesData.Length - 1]	// -1 because the last vertex in the path is blank
				};

				for (var v = 0; v < verticesData.Length - 1; ++v)
				{
					var parts = verticesData[v].Split(',');
					var x = Convert.ToSingle(parts[0]);
					var y = Convert.ToSingle(parts[1]);

					if (x < xMin) xMin = x;
					if (y < yMin) yMin = y;
					if (x > xMax) xMax = x;
					if (y > yMax) yMax = y;

					paths[i].vertices[v] = new Vector3(x, y, 0);
				}
			}

			sourceMapWidth = xMax - xMin;
			sourceMapHeight = yMax - yMin;
			Debug.Log("Globe map width: " + sourceMapWidth + " and height: " + sourceMapHeight);
		}

		private void CreateMap()
		{
			/* Credit: "comingstorm" at http://stackoverflow.com/questions/12732590/how-map-2d-grid-points-x-y-onto-sphere-as-3d-points-x-y-z
			Given a "mapping sphere" of radius R,
			the Mercator projection (x,y) of a given latitude and longitude is:
				x = R * longitude
				y = R * log( tan( (latitude + pi/2)/2 ) )

			and the inverse mapping of a given map location (x,y) is:
				longitude = x / R
				latitude = 2 * atan(exp(y/R)) - pi/2
			*/

			/* Credit: http://swardsonstudios.com/?p=65
			tx = radiusOfSphere * cos(latitude) * cos(longitude);
			ty = radiusOfSphere * -sin(latitude);
			tz = radiusOfSphere * cos(latitude) * sin(longitude);
			 */

			var gridIncrements = 10;

			// Create the north/south grid lines
			for (var x = 0; x < 360; x += gridIncrements)
			{
				var longLat = new Vector3[360 / gridIncrements];
				var latLong = new Vector3[360 / gridIncrements];
				for (var y = 0; y < 360; y += gridIncrements)
				{
					longLat[y / gridIncrements] = GetSurfacePointDegrees(x, y);
					latLong[y / gridIncrements] = GetSurfacePointDegrees(y, x);
				}
				GLRenderer.instance.AddLine(longLat, gridMaterial);
				GLRenderer.instance.AddLine(latLong, gridMaterial);
			}

			foreach (var path in paths)
			{
				var vertices = new Vector3[path.vertices.Length];
				for (var i = 0; i < path.vertices.Length; ++i)
				{
					if (isGlobe)
					{
						var longitude = path.vertices[i].x / sourceMapWidth * 2.0 * Math.PI;
						var latitude = (path.vertices[i].y / sourceMapHeight * Math.PI) - Math.PI / 2.0;

						var position = GetSurfacePointRadians(longitude, latitude);
						vertices[i] = position;
					}
					else
						vertices[i] = new Vector3((path.vertices[i].x - sourceMapWidth / 2f) / sourceMapWidth * mapWidth,
							((sourceMapHeight - path.vertices[i].y) - sourceMapHeight / 2f) / sourceMapHeight * mapHeight, 0f);
				}

				if (vertices.Length > 0)
					GLRenderer.instance.AddLine(vertices, borderlineMaterial);
			}
		}

		private Vector3 GetSurfacePointDegrees(double longitude, double latitude)
		{
			longitude *= Math.PI / 180.0;
			latitude *= Math.PI / 180.0;
			return new Vector3((float) (globeRadius * Math.Cos(latitude) * Math.Cos(longitude)),
			                   (float) (globeRadius * -Math.Sin(latitude)),
			                   (float) (globeRadius * Math.Cos(latitude) * Math.Sin(longitude)));
		}

		private Vector3 GetSurfacePointRadians(double longitude, double latitude)
		{
			return new Vector3((float) (globeRadius * Math.Cos(latitude) * Math.Cos(longitude)),
			                   (float) (globeRadius * -Math.Sin(latitude)),
			                   (float) (globeRadius * Math.Cos(latitude) * Math.Sin(longitude)));
		}

//		private void LoadCities()
//		{
//			var data = Resources.Load<TextAsset>(citiesName).text;
//			Debug.Log("Read " + data.Length + " bytes from the cities file");
//
//			var citiesData = data.Split('\n');
//			Debug.Log("Found " + citiesData.Length + " cities");
//
//			cities = new City[citiesData.Length];
//			for (var i = 0; i < cities.Length; ++i)
//			{
//				var parts = citiesData[i].Split(',');
//				cities[i] = new City
//				{
//					name = parts[0],
//					latitude = Convert.ToSingle(parts[1]),
//					longitude = Convert.ToSingle(parts[2]),
//					countryCode = parts[3],
//					population = Convert.ToInt32(parts[4]),
//				};
//
//				if (cities[i].population > maxCityPopulation)
//					maxCityPopulation = cities[i].population;
//			}
//		}

//		private void AddCitiesToGlobe()
//		{
//			var numCities = maxCities != -1 ? maxCities : cities.Length;
//			for (var i = 0; i < numCities; ++i)
//			{
//				if (maxCities != -1 && i > maxCities)
//					return;
//
//				var city = cities[i];
//
//				// Calculate the city's location on the surface
//				Vector3 position;
//				if (isGlobe)
//					// HACK: Why does this have to be around 170 degree offset and not a 180?? I suspect the source map is shit
//					position = GetSurfacePointDegrees(city.longitude + 170.0, -city.latitude);
//				else
//					position = new Vector3(((city.longitude - 10f) / 360f) * mapWidth, (city.latitude / 180f) * mapHeight, 0f);
//
//				// Calculate the size of the city
//				var amount = (city.population - minCityPopulation) / (maxCityPopulation - minCityPopulation);
//				var size = Mathf.Lerp(minCitySize, maxCitySize, amount);
//
//				var locationObject = Instantiate(Global.instance.locationPrefab, position, Quaternion.identity) as GameObject;
//				var location = locationObject.GetComponent<Location>();
//				location.indicator = city;
//				location.transform.up = isGlobe ? GetSurfaceNormal(position) : new Vector3(0, 0, 1);
//				location.transform.localScale *= size;
////				location.transform.parent = transform;
//			}
//		}

		private Vector3 GetSurfaceNormal(Vector3 position)
		{
			RaycastHit hit;
			var dir = (transform.position - position).normalized;
			if (Physics.Raycast(new Ray(position - dir * 100f, dir), out hit))
				return hit.normal;
			return Vector3.zero;
		}

//		public static Vector3 GetRandomSurfaceLocation(Vector3 center, float radius, out Vector3 normal)
//		{
//			if (instance.isGlobe)
//			{
//				var directionTowardsCenter = center - Camera.main.transform.position;
//				RaycastHit hit;
//				if (Physics.Raycast(Camera.main.transform.position, directionTowardsCenter, out hit, 1000f,
//				                    ~(int) Global.LayerMask.IgnoreRaytrace))
//				{
//					if (hit.collider.name != "WaterMesh")
//					{
//						// Get a random location within the radius of the center
//						normal = hit.normal;
//						return RandomPointOnPlane(hit.point, hit.normal, radius);
//					}
//
//					Debug.LogError("Somehow tried to place a child in the ocean");
//				}
//				else
//					Debug.LogError("Somehow managed to miss the whole world while placing a child :|");
//
//				normal = Vector3.zero;
//				return new Vector3();
//			}
//
//			normal = new Vector3(0, 0, 1);
//			var delta = Random.insideUnitCircle * radius;
//			return center + new Vector3(delta.x, delta.y);
//		}

		// Credit to: Razieln64 at http://forum.unity3d.com/threads/101956-Random-point-on-a-plane
		private static Vector3 RandomPointOnPlane(Vector3 position, Vector3 normal, float radius)
		{
			Vector3 randomPoint;

			do
			{
				randomPoint = Vector3.Cross(Random.insideUnitSphere, normal);
			} while (randomPoint == Vector3.zero);

			randomPoint.Normalize();
			randomPoint *= Random.Range(0f, radius * 2f); // HACK: Normalize the circle mesh so it has a radius of 1
			randomPoint += position;

			return randomPoint;
		}
	}
}
