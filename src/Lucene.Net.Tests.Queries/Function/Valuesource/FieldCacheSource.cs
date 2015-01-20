/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// A base class for ValueSource implementations that retrieve values for
	/// a single field from the
	/// <see cref="Org.Apache.Lucene.Search.FieldCache">Org.Apache.Lucene.Search.FieldCache
	/// 	</see>
	/// .
	/// </summary>
	public abstract class FieldCacheSource : ValueSource
	{
		protected internal readonly string field;

		protected internal readonly FieldCache cache = FieldCache.DEFAULT;

		public FieldCacheSource(string field)
		{
			this.field = field;
		}

		public virtual FieldCache GetFieldCache()
		{
			return cache;
		}

		public virtual string GetField()
		{
			return field;
		}

		public override string Description()
		{
			return field;
		}

		public override bool Equals(object o)
		{
			if (!(o is Org.Apache.Lucene.Queries.Function.Valuesource.FieldCacheSource))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.FieldCacheSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.FieldCacheSource
				)o;
			return this.field.Equals(other.field) && this.cache == other.cache;
		}

		public override int GetHashCode()
		{
			return cache.GetHashCode() + field.GetHashCode();
		}
	}
}
