using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Cameras
{
	public class FlyCamera : MonoBehaviour
	{
		public static FlyCamera instance;

		public float forwardThrust = 125;
		public float strafeThrust = 50;
		public float verticalThrust = 50;
		public float yawThrust = 2;
		public float pitchThrust = 4;
		public float rollThrust = 0.06f;
		public float stability = 0.03f;
		public float correctionSpeed = 1;
		public float movementDrag = 2;
		public float angularDrag = 3;

		private Vector3 acceleration;
		private bool lockCameraControls;
		public static bool movementActive;
		public Rigidbody rigidBody;

		private Transform lockedTarget;
		public float lockedTargetForce = 1;
		public float movementSpeedMultiplier = 1;
		public float boostFactor = 5;
		public Transform travelTarget;
		private Vector3 currentGoal;
		public bool travellingDownConnection;
		public float travelDistance;
		public float distanceToTarget;
		private Transform leavingTarget;
		public bool drifting;
		private float driftPhase;
		public float driftSpeed;
		public float driftThrust = 1f;
		private static Vector3 prevDirection;
		public float touchSensitivity = 0.33f;
		public float rollCorrectionAnglePerSecond = 25f;

		public void Awake()
		{
			instance = this;
		}

		public void Start()
		{
			rigidBody = GetComponent<Rigidbody>();
		}

		public void FixedUpdate()
		{
			var prevTimeScale = Time.timeScale;
			Time.timeScale = 1f;

			if (Input.GetMouseButton(1) || lockCameraControls || Input.touchCount > 0)
			{
				movementActive = true;
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			else
			{
				movementActive = false;
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}

			if (movementActive)
			{
				// Apply yaw and pitch
				float yawTorque, pitchTorque;
				if (Input.touchCount > 0)
				{
					var touchDelta = Input.touches[0].deltaPosition;
					yawTorque = touchDelta.x * touchSensitivity * yawThrust;
					pitchTorque = -touchDelta.y * touchSensitivity * pitchThrust;
				}
				else
				{
					yawTorque = Input.GetAxis("Mouse X") * yawThrust;
					pitchTorque = -Input.GetAxis("Mouse Y") * pitchThrust;
				}

				rigidBody.AddTorque(transform.up * yawTorque, ForceMode.Force);
				rigidBody.AddTorque(transform.right * pitchTorque, ForceMode.Force);
			}
			else
			{
				transform.rotation = Quaternion.RotateTowards(transform.rotation,
					Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0), rollCorrectionAnglePerSecond * Time.deltaTime);
			}

			// Calculate any boost
			var boostFactor = 1f;
			if (Input.GetKey(KeyCode.LeftShift))
				boostFactor = this.boostFactor;
			
			// Apply roll
			var rollTorque = 0f;
			if (Input.GetKey(KeyCode.Q))
				rollTorque = 1f;
			if (Input.GetKey(KeyCode.E))
				rollTorque = -1f;
			rigidBody.AddTorque(transform.forward * rollTorque, ForceMode.Force);

			var forward = Input.GetAxis("Vertical") * boostFactor;
			var strafe = Input.GetAxis("Horizontal") * boostFactor;
			var vertical = 0f;
			if(Input.GetKey(KeyCode.Space))
				vertical = 1 * boostFactor;
			if(Input.GetKey(KeyCode.C))
				vertical = -1 * boostFactor;

			// Two finger touch to go forwards
			if (Input.touchCount == 2)
				forward = 1f;

			// Three finger touch to go backwards
			if (Input.touchCount == 3)
				forward = -1f;

			// Apply forward / backward thrust
			rigidBody.AddForce(transform.forward * forward * forwardThrust * movementSpeedMultiplier, ForceMode.Force);

			// Apply strafing thrust
			rigidBody.AddForce(transform.right * strafe * strafeThrust * movementSpeedMultiplier, ForceMode.Force);

			// Apply vertical thrust
			rigidBody.AddForce(transform.up * vertical * verticalThrust * movementSpeedMultiplier, ForceMode.Force);

			// If the stop button is pressed
			if (Input.GetKey(KeyCode.X))
			{
				// Apply heavy drag
				rigidBody.drag = movementDrag * 10f;
				rigidBody.angularDrag = angularDrag * 10f;
			}
			else
			{
				// Apply normal drag
				rigidBody.drag = movementDrag;
				rigidBody.angularDrag = angularDrag;
			}

			if (drifting)
			{
				driftPhase += driftSpeed * Time.deltaTime;
				rigidBody.AddForce(transform.right * Mathf.Cos(driftPhase) * driftThrust, ForceMode.Force);
				rigidBody.AddForce(transform.up * Mathf.Sin(driftPhase) * driftThrust, ForceMode.Force);
			}

			Time.timeScale = prevTimeScale;
		}

		public void OnCollsionEnter(Collision collision)
		{
			Debug.Log("Camera collided with " + collision.collider.name);
		}

		public static void TeleportTo(Vector3 postition, Quaternion rotation)
		{
			instance.transform.position = postition;
			instance.transform.rotation = rotation;
		}

		public static void FocusOn(Vector3 position, float distance, bool randomDirection = false)
		{
			var direction = randomDirection || prevDirection == Vector3.zero ? Random.onUnitSphere : prevDirection;
			prevDirection = direction;

			var location = position + direction * distance;
			TeleportTo(location, Quaternion.LookRotation((position - location).normalized));
		}
	}
}
