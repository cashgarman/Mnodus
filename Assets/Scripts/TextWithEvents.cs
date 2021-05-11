using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Text;

[AddComponentMenu("UI/TextWithEvents", 12),Serializable,RequireComponent(typeof(RectTransform),typeof(CanvasRenderer))]
public class TextWithEvents : Text
{
		
#if UNITY_EDITOR
		[TextArea(3,10)]
		public string
				nonParsedStr;
		protected override void OnValidate ()
		{
				base.OnValidate ();
				base.text = OnBeforeValueChange (nonParsedStr);
				var allTextButtons = GetComponentsInChildren<TextButton> ();
				for (var id=0; id<allTextButtons.Length; id++)
						allTextButtons [id].WrapperForValidation ();
		}
#endif
		//workaround for lack of OnValueChanged event
		public new string text {
				get{ return base.text;}
				set {
						if (string.IsNullOrEmpty (value)) {
								if (string.IsNullOrEmpty (this.text)) {
										return;
								}
								base.text = string.Empty;
								#if UNITY_EDITOR
								nonParsedStr = string.Empty;
								#endif
								this.charsIdForClass.Clear ();
								this.areaForClass.Clear ();
								this.SetVerticesDirty ();
						} else {
								if (base.text != value) {
//										if (!onlyColorChanged) {
												base.text = OnBeforeValueChange (value);
												#if UNITY_EDITOR
												nonParsedStr = value;
												#endif
												this.SetAllDirty ();
//										} else
//												base.text = value;

					
								}
								
						}

				}
		}
		public bool onlyColorChanged = false;
		//used for optimizing merge string 
		private StringBuilder sb = new StringBuilder ();
		public Dictionary<string,List<HashSet<Rect>>> areaForClass = new Dictionary<string, List<HashSet<Rect>>> ();
		public Dictionary<string,List<int[]>> charsIdForClass = new Dictionary<string, List<int[]>> ();

		private string[] splittedStr;
		//compiled regex eat relatively a lot time on start but gain performance later. it require .NET 2.0 instead of .NET 2.0 subset
		//if u dont support .Net 2.0 or hiccup is out of the question simply delete |RegexOptions.Compiled
		private static Regex _regex = new Regex (@"<a href=([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline /*| RegexOptions.Compiled*/);

		//check if text contain any link if yes then strip them and generate some infos useful for OnFillVBO
		private string OnBeforeValueChange (string strToParse)
		{
			splittedStr = _regex.Split (strToParse);
			var i = 0;
			//clear sb
			sb.Length = 0;
			//allocate memory once, no more later if actual text is smaller than previous.
			//Placing the biggest planned string for this component on one frame may be a good idea for optimize allocation
			//but remember - this make hiccup particulary for realy huge text
			sb.EnsureCapacity (strToParse.Length);
			charsIdForClass.Clear ();
			foreach (var str in splittedStr) {
					if (i + 2 < splittedStr.Length && splittedStr [i + 2] == "</a>") {
							int[] charsId = new int[2] {
									sb.Length,
									sb.Length + splittedStr [i + 1].Length - 1
							};
							if (charsIdForClass.ContainsKey (str))
									charsIdForClass [str].Add (charsId);
							else {
									charsIdForClass.Add (str, new List<int[]> (){charsId});
									var child = transform.Find (str);
									if (child != null)
											child.gameObject.SetActive (true);
									else {//add event for new buton created so procedural content will be easier
											var newButton = new GameObject (str);
											newButton.AddComponent<TextButton>().targetText = GetComponent<TextWithEvents>();
											newButton.GetComponent<Image> ().color = new Color (0, 0, 0, 0);
											newButton.GetComponent<RectTransform> ().SetParent (transform, false);
									}
							}
								
					} else if (str != "</a>" && str != string.Empty) 
							sb.Append (str);
						
					i++;
			}
				
			return sb.ToString ();
		}
		//regenerate interactable area every something critical changed like rescale, rotate, change text etc.
		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			base.OnPopulateMesh(toFill);
				
				if (charsIdForClass.Count == 0)
						return;
				if (onlyColorChanged && areaForClass.Count > 0) {
						onlyColorChanged = false;
						return;
				}
				var cTextGen = cachedTextGenerator;
				//check if UI is normal or layout and alias them
				if (cTextGen == null)
						cTextGen = cachedTextGeneratorForLayout;
				foreach (var indexes in charsIdForClass) {
						//prepare event area for button like click, hover etc.
						if (areaForClass.ContainsKey (indexes.Key))
								areaForClass [indexes.Key].Clear ();
						else 
								areaForClass.Add (indexes.Key, new List<HashSet<Rect>> ());
						int i = 0;
						//upewnij sie ze jest przerwa m liniami inaczej statetsy dziwnie dzialaja jak sa obok sie
						//support multiple link with same href
						foreach (var charsIndex in indexes.Value) {
								areaForClass [indexes.Key].Add (new HashSet<Rect> ());
								//iteruj na liniach tylko raz o ile to mozliwe
								//iterate over text's lines if last char in link is under first char then generate rect for every line in link 
								//or - if aggresive approx enable - max 3 rects
								if (cTextGen.lineCount > 1 && cTextGen.verts [charsIndex [0] * 4 + 2].position.y > cTextGen.verts [charsIndex [1] * 4 + 3 > cTextGen.vertexCount - 1 ? cTextGen.vertexCount - 4 : charsIndex [1] * 4].position.y) {
										for (var lId=0; lId<cTextGen.lineCount-1; lId++) {
												if (charsIndex [0] > cTextGen.lines [lId].startCharIdx && charsIndex [0] < cTextGen.lines [lId + 1].startCharIdx) {
														areaForClass [indexes.Key] [i].Add (new Rect (cTextGen.verts [charsIndex [0] * 4].position.x, cTextGen.verts [charsIndex [0] * 4 + 3].position.y, cTextGen.verts [(cTextGen.lines [lId + 1].startCharIdx - 1) * 4 + 1].position.x - cTextGen.verts [charsIndex [0] * 4].position.x, cTextGen.lines [lId].height));
												} else if (charsIndex [1] > cTextGen.lines [lId].startCharIdx && charsIndex [1] < cTextGen.lines [lId + 1].startCharIdx) {
														areaForClass [indexes.Key] [i].Add (new Rect (cTextGen.verts [charsIndex [1] * 4].position.x, cTextGen.verts [charsIndex [1] * 4 + 3].position.y, cTextGen.verts [charsIndex [1] * 4 + 1].position.x - cTextGen.verts [cTextGen.lines [lId].startCharIdx * 4].position.x, cTextGen.lines [lId].height));
														break;
												} else if (charsIndex [0] < cTextGen.lines [lId + 1].startCharIdx) {
														areaForClass [indexes.Key] [i].Add (new Rect (cTextGen.verts [cTextGen.lines [lId].startCharIdx * 4].position.x, cTextGen.verts [cTextGen.lines [lId].startCharIdx * 4 + 3].position.y, cTextGen.verts [(cTextGen.lines [lId + 1].startCharIdx - 1) * 4 + 1].position.x - cTextGen.verts [cTextGen.lines [lId].startCharIdx * 4].position.x, cTextGen.lines [lId].height));
												}
												if (lId == cTextGen.lineCount - 2) {
														//check if ugui cut last vertices due to fact that text is too long for container
														if (charsIndex [1] * 4 + 3 > cTextGen.vertexCount - 1)
																areaForClass [indexes.Key] [i].Add (new Rect (cTextGen.verts [cTextGen.lines [lId + 1].startCharIdx * 4].position.x, cTextGen.verts [cTextGen.vertexCount - 5].position.y, cTextGen.verts [(cTextGen.vertexCount - 3)].position.x - cTextGen.verts [cTextGen.lines [lId + 1].startCharIdx * 4].position.x, cTextGen.lines [lId + 1].height));
														else
																areaForClass [indexes.Key] [i].Add (new Rect (cTextGen.verts [cTextGen.lines [lId + 1].startCharIdx * 4].position.x, cTextGen.verts [charsIndex [1] * 4 + 3].position.y, cTextGen.verts [charsIndex [1] * 4 + 1].position.x - cTextGen.verts [cTextGen.lines [lId + 1].startCharIdx * 4].position.x, cTextGen.lines [lId + 1].height));
												}
										}
										//simple case - inline link
								} else {
										//check if ugui cut last vertices due to fact that text is too long for container
										if (charsIndex [1] * 4 + 3 > cTextGen.vertexCount - 1)
												areaForClass [indexes.Key] [i].Add (new Rect (cTextGen.verts [charsIndex [0] * 4].position.x, cTextGen.verts [charsIndex [0] * 4 + 3].position.y, cTextGen.verts [cTextGen.vertexCount - 3].position.x - cTextGen.verts [charsIndex [0] * 4].position.x, cTextGen.verts [charsIndex [0] * 4].position.y - cTextGen.verts [charsIndex [0] * 4 + 3].position.y));
										else
												areaForClass [indexes.Key] [i].Add (new Rect (cTextGen.verts [charsIndex [0] * 4].position.x, cTextGen.verts [charsIndex [0] * 4 + 3].position.y, cTextGen.verts [charsIndex [1] * 4 + 1].position.x - cTextGen.verts [charsIndex [0] * 4].position.x, cTextGen.verts [charsIndex [0] * 4].position.y - cTextGen.verts [charsIndex [0] * 4 + 3].position.y));

								}
								i++;
						}

				}
		}
}
