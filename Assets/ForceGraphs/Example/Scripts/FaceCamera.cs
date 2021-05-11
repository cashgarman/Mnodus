using UnityEngine;

namespace Assets.Scripts
{
	[ExecuteInEditMode]
	public class FaceCamera : MonoBehaviour
	{
		public void LateUpdate()
		{
			// Orientate this object to face the camera
			if(Camera.main != null)
				transform.LookAt(Camera.main.transform.position, Vector3.up);
		}
	}
}
