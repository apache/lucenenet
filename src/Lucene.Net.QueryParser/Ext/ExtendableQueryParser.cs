/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Queryparser.Classic;
using Lucene.Net.Queryparser.Ext;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Ext
{
	/// <summary>
	/// The
	/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
	/// enables arbitrary query parser extension
	/// based on a customizable field naming scheme. The lucene query syntax allows
	/// implicit and explicit field definitions as query prefix followed by a colon
	/// (':') character. The
	/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
	/// allows to encode extension
	/// keys into the field symbol associated with a registered instance of
	/// <see cref="ParserExtension">ParserExtension</see>
	/// . A customizable separation character separates the
	/// extension key from the actual field symbol. The
	/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
	/// splits (@see
	/// <see cref="Extensions.SplitExtensionField(string, string)">Extensions.SplitExtensionField(string, string)
	/// 	</see>
	/// ) the
	/// extension key from the field symbol and tries to resolve the associated
	/// <see cref="ParserExtension">ParserExtension</see>
	/// . If the parser can't resolve the key or the field
	/// token does not contain a separation character,
	/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
	/// yields the same behavior as its super class
	/// <see cref="Lucene.Net.Queryparser.Classic.QueryParser">Lucene.Net.Queryparser.Classic.QueryParser
	/// 	</see>
	/// . Otherwise,
	/// if the key is associated with a
	/// <see cref="ParserExtension">ParserExtension</see>
	/// instance, the parser
	/// builds an instance of
	/// <see cref="ExtensionQuery">ExtensionQuery</see>
	/// to be processed by
	/// <see cref="ParserExtension.Parse(ExtensionQuery)">ParserExtension.Parse(ExtensionQuery)
	/// 	</see>
	/// .If a extension field does not
	/// contain a field part the default field for the query will be used.
	/// <p>
	/// To guarantee that an extension field is processed with its associated
	/// extension, the extension query part must escape any special characters like
	/// '*' or '['. If the extension query contains any whitespace characters, the
	/// extension query part must be enclosed in quotes.
	/// Example ('_' used as separation character):
	/// <pre>
	/// title_customExt:"Apache Lucene\?" OR content_customExt:prefix\
	/// </pre>
	/// Search on the default field:
	/// <pre>
	/// _customExt:"Apache Lucene\?" OR _customExt:prefix\
	/// </pre>
	/// </p>
	/// <p>
	/// The
	/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
	/// itself does not implement the logic how
	/// field and extension key are separated or ordered. All logic regarding the
	/// extension key and field symbol parsing is located in
	/// <see cref="Extensions">Extensions</see>
	/// .
	/// Customized extension schemes should be implemented by sub-classing
	/// <see cref="Extensions">Extensions</see>
	/// .
	/// </p>
	/// <p>
	/// For details about the default encoding scheme see
	/// <see cref="Extensions">Extensions</see>
	/// .
	/// </p>
	/// </summary>
	/// <seealso cref="Extensions">Extensions</seealso>
	/// <seealso cref="ParserExtension">ParserExtension</seealso>
	/// <seealso cref="ExtensionQuery">ExtensionQuery</seealso>
	public class ExtendableQueryParser : QueryParser
	{
		private readonly string defaultField;

		private readonly Extensions extensions;

		/// <summary>Default empty extensions instance</summary>
		private static readonly Extensions DEFAULT_EXTENSION = new Extensions();

		/// <summary>
		/// Creates a new
		/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
		/// instance
		/// </summary>
		/// <param name="matchVersion">the lucene version to use.</param>
		/// <param name="f">the default query field</param>
		/// <param name="a">the analyzer used to find terms in a query string</param>
		public ExtendableQueryParser(Version matchVersion, string f, Analyzer a) : this(matchVersion
			, f, a, DEFAULT_EXTENSION)
		{
		}

		/// <summary>
		/// Creates a new
		/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
		/// instance
		/// </summary>
		/// <param name="matchVersion">the lucene version to use.</param>
		/// <param name="f">the default query field</param>
		/// <param name="a">the analyzer used to find terms in a query string</param>
		/// <param name="ext">the query parser extensions</param>
		public ExtendableQueryParser(Version matchVersion, string f, Analyzer a, Extensions
			 ext) : base(matchVersion, f, a)
		{
			this.defaultField = f;
			this.extensions = ext;
		}

		/// <summary>Returns the extension field delimiter character.</summary>
		/// <remarks>Returns the extension field delimiter character.</remarks>
		/// <returns>the extension field delimiter character.</returns>
		public virtual char GetExtensionFieldDelimiter()
		{
			return extensions.GetExtensionFieldDelimiter();
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetFieldQuery(string field, string queryText, bool
			 quoted)
		{
			Extensions.Pair<string, string> splitExtensionField = this.extensions.SplitExtensionField
				(defaultField, field);
			ParserExtension extension = this.extensions.GetExtension(splitExtensionField.cud);
			if (extension != null)
			{
				return extension.Parse(new ExtensionQuery(this, splitExtensionField.cur, queryText
					));
			}
			return base.GetFieldQuery(field, queryText, quoted);
		}
	}
}
