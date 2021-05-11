using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets
{
	public class FadeByCameraDistance : MonoBehaviour
	{
		public float startThreshold = 100f;
		public float fadeTime = 0.5f;
		public Text text;
		private bool shown;
		public bool alwaysShow;

		public void Start()
		{
			if (alwaysShow || Vector3.Distance(Camera.main.transform.position, transform.position) < startThreshold)
			{
				text.gameObject.SetActive(true);
				text.canvasRenderer.SetAlpha(1);
			}
			else
			{
				text.gameObject.SetActive(false);
				text.canvasRenderer.SetAlpha(0);
			}
		}

		public void Update ()
		{
			if (alwaysShow || Vector3.Distance(Camera.main.transform.position, transform.position) < startThreshold)
			{
				if (!shown)
				{
					StopCoroutine(DisableText());
					text.gameObject.SetActive(true);
					text.canvasRenderer.SetAlpha(0); // HACK: Credit: http://answers.unity3d.com/questions/881620/uigraphiccrossfadealpha-only-works-for-decrementin.html
					text.CrossFadeAlpha(1, fadeTime, false);
					shown = true;
				}
			}
			else
			{
				if (shown)
				{
					text.CrossFadeAlpha(0, fadeTime, false);
					StartCoroutine(DisableText());
					shown = false;
				}
			}
		}

		private IEnumerator DisableText()
		{
			yield return new WaitForSeconds(fadeTime);

			if (Vector3.Distance(Camera.main.transform.position, transform.position) > startThreshold)
				text.gameObject.SetActive(false);
		}
	}
}
