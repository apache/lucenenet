/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Config
{
	/// <summary>
	/// This listener listens for every field configuration request and assign a
	/// <see cref="ConfigurationKeys.BOOST">ConfigurationKeys.BOOST</see>
	/// to the
	/// equivalent
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig">Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig
	/// 	</see>
	/// based on a defined map: fieldName -&gt; boostValue stored in
	/// <see cref="ConfigurationKeys.FIELD_BOOST_MAP">ConfigurationKeys.FIELD_BOOST_MAP</see>
	/// .
	/// </summary>
	/// <seealso cref="ConfigurationKeys.FIELD_BOOST_MAP">ConfigurationKeys.FIELD_BOOST_MAP
	/// 	</seealso>
	/// <seealso cref="ConfigurationKeys.BOOST">ConfigurationKeys.BOOST</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig">Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfigListener
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfigListener</seealso>
	public class FieldBoostMapFCListener : FieldConfigListener
	{
		private QueryConfigHandler config = null;

		public FieldBoostMapFCListener(QueryConfigHandler config)
		{
			this.config = config;
		}

		public virtual void BuildFieldConfig(FieldConfig fieldConfig)
		{
			IDictionary<string, float> fieldBoostMap = this.config.Get(StandardQueryConfigHandler.ConfigurationKeys
				.FIELD_BOOST_MAP);
			if (fieldBoostMap != null)
			{
				float boost = fieldBoostMap.Get(fieldConfig.GetField());
				if (boost != null)
				{
					fieldConfig.Set(StandardQueryConfigHandler.ConfigurationKeys.BOOST, boost);
				}
			}
		}
	}
}
