using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
	public class ObjectCollection<TKey> : IObjectCollection
	{
		public Dataset.EntitySpecification entitySpecification;
		public IEnumerable<IGraphObject<TKey>> objects;

		public virtual IGraphObject<TKey> GetByID(TKey id)
		{
			throw new NotImplementedException();
		}
	}
}