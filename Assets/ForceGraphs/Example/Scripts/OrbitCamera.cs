// Credit: http://wiki.unity3d.com/index.php?title=MouseOrbitImproved

using UnityEngine;

namespace Assets.Scripts
{
	public class OrbitCamera : MonoBehaviour
	{
		private static OrbitCamera instance;

		public Transform target;
		public float distance = 5.0f;
		public float xSpeed = 120.0f;
		public float ySpeed = 120.0f;

		public float yMinLimit = -20f;
		public float yMaxLimit = 80f;

		public float distanceMin = .5f;
		public float distanceMax = 15f;

		private new Rigidbody rigidbody;

		float x = 0.0f;
		float y = 0.0f;
		public float zoomSensitivity = 15f;

		// Use this for initialization
		public void Start()
		{
			instance = this;

			Vector3 angles = transform.eulerAngles;
			x = angles.y;
			y = angles.x;

			rigidbody = GetComponent<Rigidbody>();

			// Make the rigid body not change rotation
			if (rigidbody != null)
			{
				rigidbody.freezeRotation = true;
			}
		}

		public void LateUpdate()
		{
			if (target)
			{
				if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
				{
					x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
					y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
				}

				y = ClampAngle(y, yMinLimit, yMaxLimit);

				Quaternion rotation = Quaternion.Euler(y, x, 0);

				distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity, distanceMin, distanceMax);

				var negDistance = new Vector3(0.0f, 0.0f, -distance);
				var position = rotation * negDistance + target.position;

				transform.rotation = rotation;
				transform.position = position;
			}
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			if (angle < -360F)
				angle += 360F;
			if (angle > 360F)
				angle -= 360F;
			return Mathf.Clamp(angle, min, max);
		}

		public static void FocusOn(Transform transform, float distance)
		{
			instance.target = transform;
			instance.distance = distance;
		}
	}
}