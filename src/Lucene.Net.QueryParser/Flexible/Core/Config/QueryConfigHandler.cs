/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Core.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Config
{
	/// <summary>
	/// This class can be used to hold any query configuration and no field
	/// configuration.
	/// </summary>
	/// <remarks>
	/// This class can be used to hold any query configuration and no field
	/// configuration. For field configuration, it creates an empty
	/// <see cref="FieldConfig">FieldConfig</see>
	/// object and delegate it to field config listeners,
	/// these are responsible for setting up all the field configuration.
	/// <see cref="QueryConfigHandler">QueryConfigHandler</see>
	/// should be extended by classes that intends to
	/// provide configuration to
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Processors.QueryNodeProcessor
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Processors.QueryNodeProcessor</see>
	/// objects.
	/// The class that extends
	/// <see cref="QueryConfigHandler">QueryConfigHandler</see>
	/// should also provide
	/// <see cref="FieldConfig">FieldConfig</see>
	/// objects for each collection field.
	/// </remarks>
	/// <seealso cref="FieldConfig">FieldConfig</seealso>
	/// <seealso cref="FieldConfigListener">FieldConfigListener</seealso>
	/// <seealso cref="QueryConfigHandler">QueryConfigHandler</seealso>
	public abstract class QueryConfigHandler : AbstractQueryConfig
	{
		private readonly List<FieldConfigListener> listeners = new List<FieldConfigListener
			>();

		/// <summary>
		/// Returns an implementation of
		/// <see cref="FieldConfig">FieldConfig</see>
		/// for a specific field name. If the implemented
		/// <see cref="QueryConfigHandler">QueryConfigHandler</see>
		/// does not know a specific field name, it may
		/// return <code>null</code>, indicating there is no configuration for that
		/// field.
		/// </summary>
		/// <param name="fieldName">the field name</param>
		/// <returns>
		/// a
		/// <see cref="FieldConfig">FieldConfig</see>
		/// object containing the field name
		/// configuration or <code>null</code>, if the implemented
		/// <see cref="QueryConfigHandler">QueryConfigHandler</see>
		/// has no configuration for that field
		/// </returns>
		public virtual FieldConfig GetFieldConfig(string fieldName)
		{
			FieldConfig fieldConfig = new FieldConfig(StringUtils.ToString(fieldName));
			foreach (FieldConfigListener listener in this.listeners)
			{
				listener.BuildFieldConfig(fieldConfig);
			}
			return fieldConfig;
		}

		/// <summary>Adds a listener.</summary>
		/// <remarks>
		/// Adds a listener. The added listeners are called in the order they are
		/// added.
		/// </remarks>
		/// <param name="listener">the listener to be added</param>
		public virtual void AddFieldConfigListener(FieldConfigListener listener)
		{
			this.listeners.AddItem(listener);
		}
	}
}
