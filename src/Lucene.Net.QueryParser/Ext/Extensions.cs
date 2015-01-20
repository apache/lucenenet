/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Lucene.Net.Queryparser.Classic;
using Lucene.Net.Queryparser.Ext;
using Sharpen;

namespace Lucene.Net.Queryparser.Ext
{
	/// <summary>
	/// The
	/// <see cref="Extensions">Extensions</see>
	/// class represents an extension mapping to associate
	/// <see cref="ParserExtension">ParserExtension</see>
	/// instances with extension keys. An extension key is a
	/// string encoded into a Lucene standard query parser field symbol recognized by
	/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
	/// . The query parser passes each extension field
	/// token to
	/// <see cref="SplitExtensionField(string, string)">SplitExtensionField(string, string)
	/// 	</see>
	/// to separate the
	/// extension key from the field identifier.
	/// <p>
	/// In addition to the key to extension mapping this class also defines the field
	/// name overloading scheme.
	/// <see cref="ExtendableQueryParser">ExtendableQueryParser</see>
	/// uses the given
	/// extension to split the actual field name and extension key by calling
	/// <see cref="SplitExtensionField(string, string)">SplitExtensionField(string, string)
	/// 	</see>
	/// . To change the order or the key
	/// / field name encoding scheme users can subclass
	/// <see cref="Extensions">Extensions</see>
	/// to
	/// implement their own.
	/// </summary>
	/// <seealso cref="ExtendableQueryParser">ExtendableQueryParser</seealso>
	/// <seealso cref="ParserExtension">ParserExtension</seealso>
	public class Extensions
	{
		private readonly IDictionary<string, ParserExtension> extensions = new Dictionary
			<string, ParserExtension>();

		private readonly char extensionFieldDelimiter;

		/// <summary>The default extension field delimiter character.</summary>
		/// <remarks>
		/// The default extension field delimiter character. This constant is set to
		/// ':'
		/// </remarks>
		public const char DEFAULT_EXTENSION_FIELD_DELIMITER = ':';

		/// <summary>
		/// Creates a new
		/// <see cref="Extensions">Extensions</see>
		/// instance with the
		/// <see cref="DEFAULT_EXTENSION_FIELD_DELIMITER">DEFAULT_EXTENSION_FIELD_DELIMITER</see>
		/// as a delimiter character.
		/// </summary>
		public Extensions() : this(DEFAULT_EXTENSION_FIELD_DELIMITER)
		{
		}

		/// <summary>
		/// Creates a new
		/// <see cref="Extensions">Extensions</see>
		/// instance
		/// </summary>
		/// <param name="extensionFieldDelimiter">the extensions field delimiter character</param>
		public Extensions(char extensionFieldDelimiter)
		{
			this.extensionFieldDelimiter = extensionFieldDelimiter;
		}

		/// <summary>
		/// Adds a new
		/// <see cref="ParserExtension">ParserExtension</see>
		/// instance associated with the given key.
		/// </summary>
		/// <param name="key">the parser extension key</param>
		/// <param name="extension">the parser extension</param>
		public virtual void Add(string key, ParserExtension extension)
		{
			this.extensions.Put(key, extension);
		}

		/// <summary>
		/// Returns the
		/// <see cref="ParserExtension">ParserExtension</see>
		/// instance for the given key or
		/// <code>null</code> if no extension can be found for the key.
		/// </summary>
		/// <param name="key">the extension key</param>
		/// <returns>
		/// the
		/// <see cref="ParserExtension">ParserExtension</see>
		/// instance for the given key or
		/// <code>null</code> if no extension can be found for the key.
		/// </returns>
		public ParserExtension GetExtension(string key)
		{
			return this.extensions.Get(key);
		}

		/// <summary>Returns the extension field delimiter</summary>
		/// <returns>the extension field delimiter</returns>
		public virtual char GetExtensionFieldDelimiter()
		{
			return extensionFieldDelimiter;
		}

		/// <summary>
		/// Splits a extension field and returns the field / extension part as a
		/// <see cref="Pair{Cur, Cud}">Pair&lt;Cur, Cud&gt;</see>
		/// . This method tries to split on the first occurrence of the
		/// extension field delimiter, if the delimiter is not present in the string
		/// the result will contain a <code>null</code> value for the extension key and
		/// the given field string as the field value. If the given extension field
		/// string contains no field identifier the result pair will carry the given
		/// default field as the field value.
		/// </summary>
		/// <param name="defaultField">the default query field</param>
		/// <param name="field">the extension field string</param>
		/// <returns>
		/// a
		/// <see cref="Pair{Cur, Cud}">Pair&lt;Cur, Cud&gt;</see>
		/// with the field name as the
		/// <see cref="Pair{Cur, Cud}.cur">Pair&lt;Cur, Cud&gt;.cur</see>
		/// and the
		/// extension key as the
		/// <see cref="Pair{Cur, Cud}.cud">Pair&lt;Cur, Cud&gt;.cud</see>
		/// </returns>
		public virtual Extensions.Pair<string, string> SplitExtensionField(string defaultField
			, string field)
		{
			int indexOf = field.IndexOf(this.extensionFieldDelimiter);
			if (indexOf < 0)
			{
				return new Extensions.Pair<string, string>(field, null);
			}
			string indexField = indexOf == 0 ? defaultField : Sharpen.Runtime.Substring(field
				, 0, indexOf);
			string extensionKey = Sharpen.Runtime.Substring(field, indexOf + 1);
			return new Extensions.Pair<string, string>(indexField, extensionKey);
		}

		/// <summary>Escapes an extension field.</summary>
		/// <remarks>
		/// Escapes an extension field. The default implementation is equivalent to
		/// <see cref="Lucene.Net.Queryparser.Classic.QueryParserBase.Escape(string)">Lucene.Net.Queryparser.Classic.QueryParserBase.Escape(string)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="extfield">the extension field identifier</param>
		/// <returns>
		/// the extension field identifier with all special chars escaped with
		/// a backslash character.
		/// </returns>
		public virtual string EscapeExtensionField(string extfield)
		{
			return QueryParserBase.Escape(extfield);
		}

		/// <summary>
		/// Builds an extension field string from a given extension key and the default
		/// query field.
		/// </summary>
		/// <remarks>
		/// Builds an extension field string from a given extension key and the default
		/// query field. The default field and the key are delimited with the extension
		/// field delimiter character. This method makes no assumption about the order
		/// of the extension key and the field. By default the extension key is
		/// appended to the end of the returned string while the field is added to the
		/// beginning. Special Query characters are escaped in the result.
		/// <p>
		/// Note:
		/// <see cref="Extensions">Extensions</see>
		/// subclasses must maintain the contract between
		/// <see cref="BuildExtensionField(string)">BuildExtensionField(string)</see>
		/// and
		/// <see cref="SplitExtensionField(string, string)">SplitExtensionField(string, string)
		/// 	</see>
		/// where the latter inverts the
		/// former.
		/// </p>
		/// </remarks>
		public virtual string BuildExtensionField(string extensionKey)
		{
			return BuildExtensionField(extensionKey, string.Empty);
		}

		/// <summary>
		/// Builds an extension field string from a given extension key and the
		/// extensions field.
		/// </summary>
		/// <remarks>
		/// Builds an extension field string from a given extension key and the
		/// extensions field. The field and the key are delimited with the extension
		/// field delimiter character. This method makes no assumption about the order
		/// of the extension key and the field. By default the extension key is
		/// appended to the end of the returned string while the field is added to the
		/// beginning. Special Query characters are escaped in the result.
		/// <p>
		/// Note:
		/// <see cref="Extensions">Extensions</see>
		/// subclasses must maintain the contract between
		/// <see cref="BuildExtensionField(string, string)">BuildExtensionField(string, string)
		/// 	</see>
		/// and
		/// <see cref="SplitExtensionField(string, string)">SplitExtensionField(string, string)
		/// 	</see>
		/// where the latter inverts the
		/// former.
		/// </p>
		/// </remarks>
		/// <param name="extensionKey">the extension key</param>
		/// <param name="field">the field to apply the extension on.</param>
		/// <returns>escaped extension field identifier</returns>
		/// <seealso cref="BuildExtensionField(string)">to use the default query field</seealso>
		public virtual string BuildExtensionField(string extensionKey, string field)
		{
			StringBuilder builder = new StringBuilder(field);
			builder.Append(this.extensionFieldDelimiter);
			builder.Append(extensionKey);
			return EscapeExtensionField(builder.ToString());
		}

		/// <summary>This class represents a generic pair.</summary>
		/// <remarks>This class represents a generic pair.</remarks>
		/// <?></?>
		/// <?></?>
		public class Pair<Cur, Cud>
		{
			public readonly Cur cur;

			public readonly Cud cud;

			/// <summary>Creates a new Pair</summary>
			/// <param name="cur">the pairs first element</param>
			/// <param name="cud">the pairs last element</param>
			public Pair(Cur cur, Cud cud)
			{
				this.cur = cur;
				this.cud = cud;
			}
		}
	}
}
