/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Config
{
	/// <summary>This class represents a field configuration.</summary>
	/// <remarks>This class represents a field configuration.</remarks>
	public class FieldConfig : AbstractQueryConfig
	{
		private string fieldName;

		/// <summary>
		/// Constructs a
		/// <see cref="FieldConfig">FieldConfig</see>
		/// </summary>
		/// <param name="fieldName">the field name, it cannot be null</param>
		/// <exception cref="System.ArgumentException">if the field name is null</exception>
		public FieldConfig(string fieldName)
		{
			if (fieldName == null)
			{
				throw new ArgumentException("field name should not be null!");
			}
			this.fieldName = fieldName;
		}

		/// <summary>Returns the field name this configuration represents.</summary>
		/// <remarks>Returns the field name this configuration represents.</remarks>
		/// <returns>the field name</returns>
		public virtual string GetField()
		{
			return this.fieldName;
		}

		public override string ToString()
		{
			return "<fieldconfig name=\"" + this.fieldName + "\" configurations=\"" + base.ToString
				() + "\"/>";
		}
	}
}
