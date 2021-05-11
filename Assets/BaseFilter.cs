using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Assets
{
	public class BaseFilter : MonoBehaviour
	{
		public EditorNode node;
		public Dataset.EntityFilter filter;
		public Text nameText;

		public virtual void SetFilter(Dataset.EntityFilter _filter, EditorNode _node)
		{
			filter = _filter;
			nameText.text = filter.name;
			node = _node;
		}

		public void Remove()
		{
			DestroyImmediate(gameObject);
			node.UpdateQueries();
			GraphEditor.UpdateAllQueries();
		}

		public virtual void SetValue(Dataset.Filter value)
		{
			nameText.text = value.entityFilter.name;
		}

		public virtual void OnChanged()
		{
			node.resultsDirty = true;
		}
	}
}