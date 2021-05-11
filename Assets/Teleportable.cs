using UnityEngine;

namespace Assets
{
	public class Teleportable : MonoBehaviour
	{
		private new Renderer renderer;
		private Color originalColor;
		private Vector3 originalScale;

		public void Start()
		{
			renderer = GetComponent<Renderer>();
			originalColor = renderer.material.color;
			originalScale = transform.localScale;
		}
		public void OnSelect()
		{
			renderer.material.color = Color.green;
			transform.localScale = originalScale * 1.5f;
		}

		public void OnUnselect()
		{
			renderer.material.color = originalColor;
			transform.localScale = originalScale;
		}
	}
}
