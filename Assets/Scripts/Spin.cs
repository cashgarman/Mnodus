using UnityEngine;

namespace Assets
{
	public class Spin : MonoBehaviour
	{
		public bool x;
		public bool y;
		public bool z;
		public float minRate;
		public float maxRate;

		private float rate;

		public void Start()
		{
			rate = Random.Range(minRate, maxRate);
		}

		public void Update()
		{
			if (x) transform.Rotate(rate * Time.deltaTime, 0, 0, Space.Self);
			if (y) transform.Rotate(0, rate * Time.deltaTime, 0, Space.Self);
			if (z) transform.Rotate(0, 0, rate * Time.deltaTime, Space.Self);
		}
	}
}
