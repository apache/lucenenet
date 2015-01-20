/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Config
{
	/// <summary>
	/// This listener is used to listen to
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig">Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig
	/// 	</see>
	/// requests in
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// and add
	/// <see cref="ConfigurationKeys.NUMERIC_CONFIG">ConfigurationKeys.NUMERIC_CONFIG</see>
	/// based on the
	/// <see cref="ConfigurationKeys.NUMERIC_CONFIG_MAP">ConfigurationKeys.NUMERIC_CONFIG_MAP
	/// 	</see>
	/// set in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// .
	/// </summary>
	/// <seealso cref="NumericConfig">NumericConfig</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
	/// <seealso cref="ConfigurationKeys.NUMERIC_CONFIG">ConfigurationKeys.NUMERIC_CONFIG
	/// 	</seealso>
	/// <seealso cref="ConfigurationKeys.NUMERIC_CONFIG_MAP">ConfigurationKeys.NUMERIC_CONFIG_MAP
	/// 	</seealso>
	public class NumericFieldConfigListener : FieldConfigListener
	{
		private readonly QueryConfigHandler config;

		/// <summary>
		/// Construcs a
		/// <see cref="NumericFieldConfigListener">NumericFieldConfigListener</see>
		/// object using the given
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
		/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
		/// .
		/// </summary>
		/// <param name="config">
		/// the
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
		/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
		/// it will listen too
		/// </param>
		public NumericFieldConfigListener(QueryConfigHandler config)
		{
			if (config == null)
			{
				throw new ArgumentException("config cannot be null!");
			}
			this.config = config;
		}

		public virtual void BuildFieldConfig(FieldConfig fieldConfig)
		{
			IDictionary<string, NumericConfig> numericConfigMap = config.Get(StandardQueryConfigHandler.ConfigurationKeys
				.NUMERIC_CONFIG_MAP);
			if (numericConfigMap != null)
			{
				NumericConfig numericConfig = numericConfigMap.Get(fieldConfig.GetField());
				if (numericConfig != null)
				{
					fieldConfig.Set(StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG, numericConfig
						);
				}
			}
		}
	}
}
