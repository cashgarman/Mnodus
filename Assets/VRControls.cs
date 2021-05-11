using JetBrains.Annotations;
using UnityEngine;

namespace Assets
{
	public class VRControls : MonoBehaviour
	{
		//public SteamVR_ControllerManager controllerManager;
		public Transform playarea;
		public Transform controller1;
		public Transform controller2;
		public GameObject controller1Beam;
		public GameObject controller2Beam;
		public float teleportDistanceFactor;
		private Teleportable selectedNode;

		public void Start()
		{
			controller1Beam.SetActive(false);
			controller2Beam.SetActive(false);

			//Debug.Log(string.Format("Device index 1 is {0}", controllerManager.objects[0].name));
			//Debug.Log(string.Format("Device index 2 is {0}", controllerManager.objects[1].name));
		}

		public void Update ()
		{
			Debug.DrawLine(transform.position, transform.position + transform.forward * 1000f, Color.red);

			/*if (SteamVR_Controller.Input(1).GetPress(SteamVR_Controller.ButtonMask.Trigger))
			{
				OnTriggerDown(controller1, controller1Beam);
			}
			else
			{
				controller1Beam.SetActive(false);
			}
			if (SteamVR_Controller.Input(2).GetPress(SteamVR_Controller.ButtonMask.Trigger))
			{
				OnTriggerDown(controller2, controller2Beam);
			}
			else
			{
				controller2Beam.SetActive(false);
			}

			if (!SteamVR_Controller.Input(1).GetPress(SteamVR_Controller.ButtonMask.Trigger) &&
			    !SteamVR_Controller.Input(2).GetPress(SteamVR_Controller.ButtonMask.Trigger))
			{
				OnTriggerUp();
			}*/
		}

		private void OnTriggerUp()
		{
			if (selectedNode != null)
			{
				TeleportTo(selectedNode);
				selectedNode.OnUnselect();
				selectedNode = null;
			}
		}

		private void TeleportTo(Teleportable node)
		{
			if (Vector3.Distance(transform.position, node.transform.position) <= teleportDistanceFactor * node.transform.localScale.magnitude)
				return;

			playarea.transform.position = node.transform.position - transform.localPosition * 20 + transform.forward * -teleportDistanceFactor * node.transform.localScale.magnitude;
		}

		private void OnTriggerDown(Transform controller, GameObject beam)
		{
			beam.SetActive(true);

			if (selectedNode != null)
			{
				beam.transform.localScale = new Vector3(beam.transform.localScale.x,
					Vector3.Distance(selectedNode.transform.position, controller.position) / 40f,
					beam.transform.localScale.z);
				Debug.Log(beam.transform.localScale.y);
			}

			Debug.DrawLine(controller.position, controller.position + controller.forward * 1000f, Color.red);

			RaycastHit hit;
			if (Physics.Raycast(new Ray(controller.position, controller.forward), out hit))
			{
				var node = hit.collider.GetComponent<Teleportable>();
				if (node != null)
				{
					if(selectedNode != null)
						selectedNode.OnUnselect();

					selectedNode = node;
					node.OnSelect();
				}
			}
			else
			{
				if (selectedNode != null)
				{
					selectedNode.OnUnselect();
					selectedNode = null;
				}
				else
					beam.transform.localScale = new Vector3(0.01f, 1000f, 0.01f);
			}

			beam.transform.localPosition = new Vector3(0, 0, beam.transform.localScale.y);
		}
	}
}
