using UnityEngine;

namespace Assets
{
	public class InitController : MonoBehaviour
	{
		//public SteamVR_ControllerManager controllerManager;
		public int deviceIndex;

		public void Awake()
		{
			//controllerManager.left.GetComponent<SteamVR_TrackedObject>().SetDeviceIndex(deviceIndex);
			Debug.Log("Setting " + name + " to device index " + deviceIndex);
		}
	}
}
