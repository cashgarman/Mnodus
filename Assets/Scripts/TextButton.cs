using UnityEngine;
using UnityEngine.UI;
using System;
[AddComponentMenu("UI/TextButton", 10),Serializable,RequireComponent(typeof(RectTransform),typeof(CanvasRenderer),typeof(Image))]
public class TextButton :Button,ICanvasRaycastFilter
{
		public TextWithEvents targetText;
		int hoverId = -1;

	#if UNITY_EDITOR
		protected override void OnValidate ()
		{
				base.OnValidate ();
				WrapperForValidation ();
		}
		public void WrapperForValidation ()
		{
				if (targetText == null)
						targetText = GetComponentInParent<TextWithEvents> ();
				if (targetText.charsIdForClass.ContainsKey (name))
						DoStateTransition (interactable ? SelectionState.Normal : SelectionState.Disabled, true);
		}
	#endif

		//it must be Start due to fact that TextWithEvents must prepare some infos (on Awake) before button
		//it could be done with script execution order though if u need change this
		void Start ()
		{
				targetText = GetComponentInParent<TextWithEvents> ();
		}
		//old implementation and deprected. someone might find it useful due to fact this is algorithm to check if point is inside convex polygon2D
		/*private bool CheckIfPointInPolygon (Vector2[] polygon, Vector2 point)
		{
				bool check = false;
				var lastVector = polygon.Length - 1;
				for (var i=0; i<polygon.Length; i++) {
						Vector2 tpi = polygon [i];
						Vector2 tpj = polygon [lastVector];

						if (tpi.y < point.y && tpj.y >= point.y || tpj.y < point.y && tpi.y >= point.y)
						if (tpi.x + (point.y - tpi.y) / (tpj.y - tpi.y) * (tpj.x - tpi.x) < point.x)
								check = !check;
						lastVector = i;
				}
				return check;
		}*/
		private string ColorToHex (Color32 color)
		{
				return color.r.ToString ("X2") + color.g.ToString ("X2") + color.b.ToString ("X2") + color.a.ToString ("X2");
		}
		//change state like button but for link only
		protected override void DoStateTransition (SelectionState state, bool instant)
		{
#if !UNITY_EDITOR
				if (hoverId == -1 && state != SelectionState.Normal && state != SelectionState.Disabled)
						return;
		else if (hoverId == -1 || targetText.charsIdForClass.ContainsKey (name) || targetText.charsIdForClass [name] [hoverId] [0] == 0 || targetText.text.ToCharArray () [targetText.charsIdForClass [name] [hoverId] [0] - 1] != '>')
						return;

#else
				if (targetText == null || !targetText.charsIdForClass.ContainsKey (name))
						return;
				#endif
				//set this bool to true for avoid unnecesary rebuild interactable area due to fact that only color has changed
				targetText.onlyColorChanged = true;
				for (int linkId=0; linkId<targetText.charsIdForClass[name].Count; linkId++)
						InternalDoStateTransition (linkId, (state == SelectionState.Disabled || hoverId == linkId) ? state : SelectionState.Normal, instant);
		}

		private void InternalDoStateTransition (int id, SelectionState state, bool instant)
		{
			return;

				var colorStartAt = targetText.charsIdForClass [name] [id] [0] - 9;
				var colorInHex = "";
				var tmpStr = targetText.text.Remove (colorStartAt, 8);
				switch (state) {
				case SelectionState.Normal:
						colorInHex = ColorToHex (colors.normalColor);
						break;
				case SelectionState.Pressed:
						colorInHex = ColorToHex (colors.pressedColor);
						break;
				case SelectionState.Highlighted:
						colorInHex = ColorToHex (colors.highlightedColor);
						break;
				case SelectionState.Disabled:
						colorInHex = ColorToHex (colors.disabledColor);
						break;
				}
				//take color from string via substring
				targetText.text = tmpStr.Insert (colorStartAt, colorInHex);
		}
		//trying to achive fadein/out like unity button but no luck without any help
		/*private IEnumerator TweenTextColor (int id, SelectionState state, Color startColor, Color targetColor, bool instant)
		{
				yield return new WaitForEndOfFrame ();
				if (!instant)
						for (float step=0; step<colors.fadeDuration; step+=Time.deltaTime) {
								//switch inside loop is intended because if fade duration is loong 
						
								// Color.Lerp (startColor, targetColor, step / colors.fadeDuration)
								yield return new WaitForEndOfFrame ();	
						}
				else {
						//target color
						yield break;
				} 

		}*/
		public bool IsRaycastLocationValid (Vector2 sp, Camera eventCamera)
		{
				Vector2 lp;
				RectTransformUtility.ScreenPointToLocalPointInRectangle (targetText.rectTransform, sp, eventCamera, out lp);
				//check if supported text contain any link with href=myname. if no disable button for prevent waste resource		
				if (!targetText.areaForClass.ContainsKey (name))		
						return gameObject.active = false;
				//check if mouse over link and remember which
				var id = 0;
				foreach (var rects in targetText.areaForClass[name]) {
						foreach (var rect in rects)
								if (rect.Contains (lp)) {
										hoverId = id;
										return true;
								}
						id++;
				}
				hoverId = -1;
				return false;
		}
		//test usability
		public void test ()
		{
				Debug.Log ("VIVAT!");
		}
}
