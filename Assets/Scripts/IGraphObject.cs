namespace Assets.Scripts
{
	public interface IGraphObject<TKey>
	{
		TKey ID { get; set; }
		object GetValue(string name);
	}
}