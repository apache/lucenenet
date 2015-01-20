/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Config;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Config
{
	/// <summary>
	/// This interface should be implemented by classes that wants to listen for
	/// field configuration requests.
	/// </summary>
	/// <remarks>
	/// This interface should be implemented by classes that wants to listen for
	/// field configuration requests. The implementation receives a
	/// <see cref="FieldConfig">FieldConfig</see>
	/// object and may add/change its configuration.
	/// </remarks>
	/// <seealso cref="FieldConfig">FieldConfig</seealso>
	/// <seealso cref="QueryConfigHandler">QueryConfigHandler</seealso>
	public interface FieldConfigListener
	{
		/// <summary>This method is called ever time a field configuration is requested.</summary>
		/// <remarks>This method is called ever time a field configuration is requested.</remarks>
		/// <param name="fieldConfig">the field configuration requested, should never be null
		/// 	</param>
		void BuildFieldConfig(FieldConfig fieldConfig);
	}
}
