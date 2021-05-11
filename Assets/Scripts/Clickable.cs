using System;
using UnityEngine;

namespace Assets.Scripts
{
	public class Clickable : MonoBehaviour
	{
		public double doubleClickTime = 0.5f;
		public Action onLeftClick;
		public Action onDoubleLeftClick;
		public Action onRightClick;
		public Action onDoubleRightClick;

		private double timeOfLastLeftClick;
		private double timeOfLastRightClick;

		public void OnMouseDown()
		{
			if (Input.GetMouseButton(0))
			{
				if (Time.realtimeSinceStartup - timeOfLastLeftClick <= doubleClickTime)
				{
					if (onDoubleLeftClick != null)
						onDoubleLeftClick();
				}
				else
				{
					if (onLeftClick != null)
						onLeftClick();
				}	

				timeOfLastLeftClick = Time.realtimeSinceStartup;
			}
		}

		public void OnMouseOver()
		{
			if (Input.GetMouseButtonDown(1))
			{
				if (Time.realtimeSinceStartup - timeOfLastRightClick <= doubleClickTime)
				{
					if (onDoubleRightClick != null)
						onDoubleRightClick();
				}
				else
				{
					if (onRightClick != null)
						onRightClick();
				}

				timeOfLastRightClick = Time.realtimeSinceStartup;
			}
		}
	}
}