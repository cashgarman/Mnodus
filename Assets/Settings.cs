using UnityEngine;

namespace Assets
{
	public class Settings : MonoBehaviour
	{
		public static Settings instance;
		public string datasetSpecPath;
		public int maxEntityResults = 1000;
		public double maxQueryTime = 30.0;

		public void Awake()
		{
			instance = this;
		}
	}
}