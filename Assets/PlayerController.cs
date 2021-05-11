using UnityEngine;
using Valve.VR;

namespace Assets
{
	public class PlayerController : MonoBehaviour
	{
		public float timeBetweenMovements;
		public float movementAmount;
		public Transform eyes;

		private bool moving;
		private Vector2 currentMovingDirection;
		private float timeSinceLastMovement;

		public void Update()
		{
			/*foreach (var device in SteamVR_Controller.devices)
			{
				if (device.GetTouchDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
					moving = true;

				if (device.GetTouchUp(EVRButtonId.k_EButton_SteamVR_Touchpad))
					moving = false;
			}*/

			timeSinceLastMovement += Time.deltaTime;

			/*if (moving)
				foreach (var device in SteamVR_Controller.devices)
					if (device.GetTouch(EVRButtonId.k_EButton_SteamVR_Touchpad))
					{
						var dir = device.GetAxis();

						currentMovingDirection = new Vector2(dir.x > .5 ? 1 : dir.x < -.5 ? -1 : 0, dir.y > .5 ? 1 : dir.y < -.5 ? -1 : 0);
					}*/

			if (moving && timeSinceLastMovement >= timeBetweenMovements)
			{
				var forward = currentMovingDirection.y * movementAmount;
				var strafe = currentMovingDirection.x * movementAmount;

				var movement = eyes.forward * forward + eyes.right * strafe;
				transform.Translate(movement, Space.World);

				timeSinceLastMovement = 0f;
			}
		}
	}
}