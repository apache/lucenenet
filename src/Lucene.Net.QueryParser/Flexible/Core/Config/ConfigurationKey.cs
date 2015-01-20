/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Config
{
	/// <summary>
	/// An instance of this class represents a key that is used to retrieve a value
	/// from
	/// <see cref="AbstractQueryConfig">AbstractQueryConfig</see>
	/// . It also holds the value's type, which is
	/// defined in the generic argument.
	/// </summary>
	/// <seealso cref="AbstractQueryConfig">AbstractQueryConfig</seealso>
	public sealed class ConfigurationKey<T>
	{
		public ConfigurationKey()
		{
		}

		/// <summary>Creates a new instance.</summary>
		/// <remarks>Creates a new instance.</remarks>
		/// <?></?>
		/// <returns>a new instance</returns>
		public static Lucene.Net.Queryparser.Flexible.Core.Config.ConfigurationKey
			<T> NewInstance<T>()
		{
			return new Lucene.Net.Queryparser.Flexible.Core.Config.ConfigurationKey<T>
				();
		}
	}
}
