public interface IRepository<TEntity, in TKey> where TEntity : class
{
	TEntity GetByID(TKey);
	void Create(TEntity);
	void Update(TEntity);
	void Delete(TEntity);
}