using Assets.Scripts;
using UnityEngine;
using Vectrosity;

namespace Assets
{
	public class Wire
	{
		public EditorNode start;
		public EditorNode end;
		public VectorLine line;
		public GameObject dropdownObject;
		public Dataset.RelationshipData relationship;

		public void Update()
		{
			var dir = (start.transform.position - end.transform.position).normalized;
			line.points2[0] = end.transform.position + dir * GraphEditor.instance.wireEndOffset;
			line.points2[1] = start.transform.position - dir * GraphEditor.instance.wireEndOffset * .75f;
			line.Draw();

			dropdownObject.transform.position = (start.transform.position + end.transform.position) / 2f;
		}
	}
}