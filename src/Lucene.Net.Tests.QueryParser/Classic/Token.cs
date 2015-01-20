/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Classic
{
	/// <summary>Describes the input token stream.</summary>
	/// <remarks>Describes the input token stream.</remarks>
	[System.Serializable]
	public class Token
	{
		/// <summary>The version identifier for this Serializable class.</summary>
		/// <remarks>
		/// The version identifier for this Serializable class.
		/// Increment only if the <i>serialized</i> form of the
		/// class changes.
		/// </remarks>
		private const long serialVersionUID = 1L;

		/// <summary>An integer that describes the kind of this token.</summary>
		/// <remarks>
		/// An integer that describes the kind of this token.  This numbering
		/// system is determined by JavaCCParser, and a table of these numbers is
		/// stored in the file ...Constants.java.
		/// </remarks>
		public int kind;

		/// <summary>The line number of the first character of this Token.</summary>
		/// <remarks>The line number of the first character of this Token.</remarks>
		public int beginLine;

		/// <summary>The column number of the first character of this Token.</summary>
		/// <remarks>The column number of the first character of this Token.</remarks>
		public int beginColumn;

		/// <summary>The line number of the last character of this Token.</summary>
		/// <remarks>The line number of the last character of this Token.</remarks>
		public int endLine;

		/// <summary>The column number of the last character of this Token.</summary>
		/// <remarks>The column number of the last character of this Token.</remarks>
		public int endColumn;

		/// <summary>The string image of the token.</summary>
		/// <remarks>The string image of the token.</remarks>
		public string image;

		/// <summary>
		/// A reference to the next regular (non-special) token from the input
		/// stream.
		/// </summary>
		/// <remarks>
		/// A reference to the next regular (non-special) token from the input
		/// stream.  If this is the last token from the input stream, or if the
		/// token manager has not read tokens beyond this one, this field is
		/// set to null.  This is true only if this token is also a regular
		/// token.  Otherwise, see below for a description of the contents of
		/// this field.
		/// </remarks>
		public Org.Apache.Lucene.Queryparser.Classic.Token next;

		/// <summary>
		/// This field is used to access special tokens that occur prior to this
		/// token, but after the immediately preceding regular (non-special) token.
		/// </summary>
		/// <remarks>
		/// This field is used to access special tokens that occur prior to this
		/// token, but after the immediately preceding regular (non-special) token.
		/// If there are no such special tokens, this field is set to null.
		/// When there are more than one such special token, this field refers
		/// to the last of these special tokens, which in turn refers to the next
		/// previous special token through its specialToken field, and so on
		/// until the first special token (whose specialToken field is null).
		/// The next fields of special tokens refer to other special tokens that
		/// immediately follow it (without an intervening regular token).  If there
		/// is no such token, this field is null.
		/// </remarks>
		public Org.Apache.Lucene.Queryparser.Classic.Token specialToken;

		/// <summary>An optional attribute value of the Token.</summary>
		/// <remarks>
		/// An optional attribute value of the Token.
		/// Tokens which are not used as syntactic sugar will often contain
		/// meaningful values that will be used later on by the compiler or
		/// interpreter. This attribute value is often different from the image.
		/// Any subclass of Token that actually wants to return a non-null value can
		/// override this method as appropriate.
		/// </remarks>
		public virtual object GetValue()
		{
			return null;
		}

		/// <summary>No-argument constructor</summary>
		public Token()
		{
		}

		/// <summary>Constructs a new token for the specified Image.</summary>
		/// <remarks>Constructs a new token for the specified Image.</remarks>
		public Token(int kind) : this(kind, null)
		{
		}

		/// <summary>Constructs a new token for the specified Image and Kind.</summary>
		/// <remarks>Constructs a new token for the specified Image and Kind.</remarks>
		public Token(int kind, string image)
		{
			this.kind = kind;
			this.image = image;
		}

		/// <summary>Returns the image.</summary>
		/// <remarks>Returns the image.</remarks>
		public override string ToString()
		{
			return image;
		}

		/// <summary>Returns a new Token object, by default.</summary>
		/// <remarks>
		/// Returns a new Token object, by default. However, if you want, you
		/// can create and return subclass objects based on the value of ofKind.
		/// Simply add the cases to the switch for all those special cases.
		/// For example, if you have a subclass of Token called IDToken that
		/// you want to create if ofKind is ID, simply add something like :
		/// case MyParserConstants.ID : return new IDToken(ofKind, image);
		/// to the following switch statement. Then you can cast matchedToken
		/// variable to the appropriate type and use sit in your lexical actions.
		/// </remarks>
		public static Org.Apache.Lucene.Queryparser.Classic.Token NewToken(int ofKind, string
			 image)
		{
			switch (ofKind)
			{
				default:
				{
					return new Org.Apache.Lucene.Queryparser.Classic.Token(ofKind, image);
					break;
				}
			}
		}

		public static Org.Apache.Lucene.Queryparser.Classic.Token NewToken(int ofKind)
		{
			return NewToken(ofKind, null);
		}
	}
}
