using Assets.Scripts;
using UnityEngine.UI;

namespace Assets
{
	public class IntFilter : BaseFilter
	{
		public InputField inputField;

		public override void OnChanged()
		{
			base.OnChanged();

			node.UpdateQueries();
			GraphEditor.UpdateAllQueries();
		}

		public override void SetValue(Dataset.Filter value)
		{
			base.SetValue(value);

			inputField.text = value.intValue.ToString();
		}
	}
}