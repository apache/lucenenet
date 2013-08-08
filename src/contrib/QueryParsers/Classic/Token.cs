using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Classic
{
    [Serializable]
    public class Token // : ISerializable
    {
        private const long serialVersionUID = 1L;

        /**
        * An integer that describes the kind of this token.  This numbering
        * system is determined by JavaCCParser, and a table of these numbers is
        * stored in the file ...Constants.java.
        */
        public int kind;

        /** The line number of the first character of this Token. */
        public int beginLine;
        /** The column number of the first character of this Token. */
        public int beginColumn;
        /** The line number of the last character of this Token. */
        public int endLine;
        /** The column number of the last character of this Token. */
        public int endColumn;

        /**
         * The string image of the token.
         */
        public String image;

        /**
        * A reference to the next regular (non-special) token from the input
        * stream.  If this is the last token from the input stream, or if the
        * token manager has not read tokens beyond this one, this field is
        * set to null.  This is true only if this token is also a regular
        * token.  Otherwise, see below for a description of the contents of
        * this field.
        */
        public Token next;

        /**
         * This field is used to access special tokens that occur prior to this
         * token, but after the immediately preceding regular (non-special) token.
         * If there are no such special tokens, this field is set to null.
         * When there are more than one such special token, this field refers
         * to the last of these special tokens, which in turn refers to the next
         * previous special token through its specialToken field, and so on
         * until the first special token (whose specialToken field is null).
         * The next fields of special tokens refer to other special tokens that
         * immediately follow it (without an intervening regular token).  If there
         * is no such token, this field is null.
         */
        public Token specialToken;

        /**
         * An optional attribute value of the Token.
         * Tokens which are not used as syntactic sugar will often contain
         * meaningful values that will be used later on by the compiler or
         * interpreter. This attribute value is often different from the image.
         * Any subclass of Token that actually wants to return a non-null value can
         * override this method as appropriate.
         */
        public virtual Object Value
        {
            get { return null; }
        }

        public Token() { }

        public Token(int kind)
            : this(kind, null)
        {
        }

        public Token(int kind, String image)
        {
            this.kind = kind;
            this.image = image;
        }

        public override string ToString()
        {
            return image;
        }

        public static Token NewToken(int ofKind, String image)
        {
            switch (ofKind)
            {
                default: return new Token(ofKind, image);
            }
        }

        public static Token NewToken(int ofKind)
        {
            return NewToken(ofKind, null);
        }
    }
}
