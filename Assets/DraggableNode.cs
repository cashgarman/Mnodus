using UnityEngine;

namespace Assets
{
	public class DraggableNode : MonoBehaviour
	{
		private Vector3 mouseOffset;
		public bool clone;
		private bool moving;
		private bool wiring;

		public void OnBeginDrag()
		{
			// Left mouse button drag
			if (Input.GetMouseButton(0))
			{
				if (clone)
				{
					// Leave a new clonable object behind
					var siblingIndex = transform.GetSiblingIndex();
					var replacementObject = Instantiate(gameObject);
					replacementObject.transform.position = transform.position;
					replacementObject.transform.SetParent(transform.parent);
					replacementObject.GetComponent<DraggableNode>().clone = true;
					replacementObject.transform.SetSiblingIndex(siblingIndex);
					var editorNode = replacementObject.GetComponent<EditorNode>();
					editorNode.objectType = GetComponent<EditorNode>().objectType;
					editorNode.SetCollection(GetComponent<EditorNode>().collection);

					// Parent the dragged object to the workspace
					transform.SetParent(GraphEditor.instance.workspaceContainer);

					clone = false;

					GraphEditor.instance.AddNewNode(this);
					GraphEditor.ClearSelection();
				}

				GraphEditor.instance.OnBeginNodeDrag(GetComponent<EditorNode>());
				mouseOffset = Input.mousePosition - transform.position;
				moving = true;
			}

			// Right mouse button drag
			else if (Input.GetMouseButton(1) && !clone)
			{
				GraphEditor.instance.OnStartWire(this);
				wiring = true;
			}
		}

		public void OnDrag()
		{
			if (moving)
			{
				GraphEditor.instance.OnNodeDragged((Input.mousePosition - mouseOffset) - transform.position);
				transform.position = Input.mousePosition - mouseOffset;
			}
			else if (wiring)
				GraphEditor.instance.OnUpdateWire();
		}

		public void OnDragEnd()
		{
			if(wiring)
				GraphEditor.instance.OnEndWire();

			if (!clone && transform.position.x < GraphEditor.instance.toolboxRect.rect.width)
				GraphEditor.DeleteNode(GetComponent<EditorNode>());

			moving = false;
			wiring = false;
		}

		public void OnMouseOver()
		{
			GraphEditor.instance.objectUnderMouse = this;
		}

		public void OnMouseLeft()
		{
			GraphEditor.instance.objectUnderMouse = null;
		}

		public void OnClicked()
		{
			if(!moving && !clone)
				GraphEditor.AddToSelection(this);
		}
	}
}
