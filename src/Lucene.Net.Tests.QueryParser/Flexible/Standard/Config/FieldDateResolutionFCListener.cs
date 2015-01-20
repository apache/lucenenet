/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Config
{
	/// <summary>
	/// This listener listens for every field configuration request and assign a
	/// <see cref="ConfigurationKeys.DATE_RESOLUTION">ConfigurationKeys.DATE_RESOLUTION</see>
	/// to the equivalent
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfig">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfig
	/// 	</see>
	/// based
	/// on a defined map: fieldName -&gt;
	/// <see cref="Org.Apache.Lucene.Document.DateTools.Resolution">Org.Apache.Lucene.Document.DateTools.Resolution
	/// 	</see>
	/// stored in
	/// <see cref="ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP">ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP
	/// 	</see>
	/// .
	/// </summary>
	/// <seealso cref="ConfigurationKeys.DATE_RESOLUTION">ConfigurationKeys.DATE_RESOLUTION
	/// 	</seealso>
	/// <seealso cref="ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP">ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfig">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfig
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfigListener
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfigListener</seealso>
	public class FieldDateResolutionFCListener : FieldConfigListener
	{
		private QueryConfigHandler config = null;

		public FieldDateResolutionFCListener(QueryConfigHandler config)
		{
			this.config = config;
		}

		public virtual void BuildFieldConfig(FieldConfig fieldConfig)
		{
			DateTools.Resolution dateRes = null;
			IDictionary<CharSequence, DateTools.Resolution> dateResMap = this.config.Get(StandardQueryConfigHandler.ConfigurationKeys
				.FIELD_DATE_RESOLUTION_MAP);
			if (dateResMap != null)
			{
				dateRes = dateResMap.Get(fieldConfig.GetField());
			}
			if (dateRes == null)
			{
				dateRes = this.config.Get(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
					);
			}
			if (dateRes != null)
			{
				fieldConfig.Set(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION, dateRes
					);
			}
		}
	}
}
