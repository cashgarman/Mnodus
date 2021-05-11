using Assets.Scripts;
using UnityEngine.UI;

namespace Assets
{
	public class ChoiceFilter : BaseFilter
	{
		public Toggle trueToggle;
		public Toggle falseToggle;

		public override void OnChanged()
		{
			base.OnChanged();

			node.UpdateQueries();
			GraphEditor.UpdateAllQueries();
		}

		public override void SetValue(Dataset.Filter value)
		{
			base.SetValue(value);

			trueToggle.isOn = value.booleanValue;
			falseToggle.isOn = !value.booleanValue;
		}
	}
}