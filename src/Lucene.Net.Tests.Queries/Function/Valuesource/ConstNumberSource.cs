/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary><code>ConstNumberSource</code> is the base class for all constant numbers
	/// 	</summary>
	public abstract class ConstNumberSource : ValueSource
	{
		public abstract int GetInt();

		public abstract long GetLong();

		public abstract float GetFloat();

		public abstract double GetDouble();

		public abstract Number GetNumber();

		public abstract bool GetBool();
	}
}
