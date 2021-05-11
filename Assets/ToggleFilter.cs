using Assets.Scripts;
using UnityEngine.UI;

namespace Assets
{
	public class ToggleFilter : BaseFilter
	{
		public Toggle toggle;

		public override void OnChanged()
		{
			base.OnChanged();

			node.UpdateQueries();
			GraphEditor.UpdateAllQueries();
		}

		public override void SetValue(Dataset.Filter value)
		{
			base.SetValue(value);

			toggle.isOn = value.booleanValue;
		}
	}
}