using System.Collections;
using MongoDB.Driver;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets
{
	public class ButtonTest : MonoBehaviour
	{
		public TextWithEvents text;

		public void OnLinkPressed()
		{
			Debug.Log(EventSystem.current.currentSelectedGameObject.name);

			foreach (Transform child in transform)
				DestroyImmediate(child.gameObject);

			StartCoroutine(ReplaceText(text.font, text.fontSize, text.fontStyle, text.alignment));
		}

		private IEnumerator ReplaceText(Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment)
		{
			Destroy(GetComponent<TextWithEvents>());

			yield return new WaitForEndOfFrame();

			text = gameObject.AddComponent<TextWithEvents>();
			text.font = font;
			text.fontSize = fontSize;
			text.fontStyle = fontStyle;
			text.alignment = alignment;
			text.text = "<color=blue><a href=NowHere>Now here</a></color>!";

			var linkButton = transform.Find("NowHere").GetComponent<TextButton>();
			linkButton.onClick.AddListener(() => Debug.Log("new link clicked!"));
		}
	}
}
