using UnityEditor;
using UnityEngine;
using UnityEditor.UI;
/*[Serializable]
public class CSSContainer
{
		[SerializeField]
		private string
				name;
		//public CSS4UGUI css; /scriptableObject?
}*/
[CanEditMultipleObjects, CustomEditor(typeof(TextWithEvents), true)]
public class TextClassHandler : GraphicEditor
{
		private TextWithEvents Target {
				get {
						return target as TextWithEvents;
				}
		}
		//private SerializedProperty m_Text;
		private SerializedProperty m_NonParsedStr;
		private SerializedProperty m_FontData;
		private GUIContent m_TextContent;


		[MenuItem("GameObject/UI/Text With Events")]
		static void Test (MenuCommand command)
		{
				var go = new GameObject ("TextWithEvents");
				go.AddComponent<TextWithEvents> ();
				GameObjectUtility.SetParentAndAlign (go, command.context as GameObject);
				Undo.RegisterCreatedObjectUndo (go, "Create" + go.name);
				Selection.activeObject = go;
		}
		protected override void OnEnable ()
		{
				base.OnEnable ();
				this.m_TextContent = new GUIContent ("Text");
				this.m_NonParsedStr = base.serializedObject.FindProperty ("nonParsedStr");
				this.m_FontData = base.serializedObject.FindProperty ("m_FontData");
				
		}
		public override void OnInspectorGUI ()
		{

				base.serializedObject.Update ();
				EditorGUILayout.PropertyField (this.m_NonParsedStr, this.m_TextContent, new GUILayoutOption[0]);
				EditorGUILayout.PropertyField (this.m_FontData, new GUILayoutOption[0]);
				base.AppearanceControlsGUI ();
				base.serializedObject.ApplyModifiedProperties ();
		}
		
}
