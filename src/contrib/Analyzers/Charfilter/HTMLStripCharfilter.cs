using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Charfilter
{
    public class HTMLStripCharFilter : BaseCharFilter
    {
        /// <summary>
        ///     Lexical states
        /// </summary>
        private const int YYINITIAL = 0;

        private const int AMPERSAND = 2;
        private const int NUMERIC_CHARACTER = 4;
        private const int CHARACTER_REFERENCE_TAIL = 6;
        private const int LEFT_ANGLE_BRACKET = 8;
        private const int BANG = 10;
        private const int COMMENT = 12;
        private const int SCRIPT = 14;
        private const int SCRIPT_COMMENT = 16;
        private const int LEFT_ANGLE_BRACKET_SLASH = 18;
        private const int LEFT_ANGLE_BRACKET_SPACE = 20;
        private const int CDATA = 22;
        private const int SERVER_SIDE_INCLUDE = 24;
        private const int SINGLE_QUOTED_STRING = 26;
        private const int DOUBLE_QUOTED_STRING = 28;
        private const int END_TAG_TAIL_INCLUDE = 30;
        private const int END_TAG_TAIL_EXCLUDE = 32;
        private const int END_TAG_TAIL_SUBSTITUTE = 34;
        private const int START_TAG_TAIL_INCLUDE = 36;
        private const int START_TAG_TAIL_EXCLUDE = 38;
        private const int START_TAG_TAIL_SUBSTITUTE = 40;
        private const int STYLE = 42;
        private const int STYLE_COMMENT = 44;

        private static readonly ResourceManager Resources =
            new ResourceManager("HTMLCharSTripFilterResources", Assembly.GetAssembly(typeof (HTMLStripCharFilter)));

        /// <summary>
        ///     This character denotes the end of file
        /// </summary>
        private static readonly int YYEOF = -1;

        /// <summary>
        ///     initial size of the lookahead buffer
        /// </summary>
        private static readonly int ZZ_BUFFERSIZE = 16384;


        /// <summary>
        ///     ZZ_LEXSTATE[l] is the state in the DFA for the lexical state l
        ///     ZZ_LEXSTATE[l+1] is the state in the DFA for the lexical state l
        ///     at the beginning of a line
        ///     l is of the form l = 2*k, k a non negative integer
        /// </summary>
        private static readonly int[] ZZ_LEXSTATE =
        {
            0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7,
            8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15,
            16, 16, 17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22
        };


        /// <summary>
        ///     Translates characters to character classes.
        /// </summary>
        private static readonly string ZZ_CMAP_PACKED = Resources.GetString("ZZ_CMAP_PACKED");


        /// <summary>
        ///     Translates characters to character classes
        /// </summary>
        private static readonly char[] ZZ_CMAP = zzUnpackCMap(ZZ_CMAP_PACKED);


        /// <summary>
        ///     Translates DFA states to action switch labels.
        /// </summary>
        private static readonly int[] ZZ_ACTION = zzUnpackAction();


        private static readonly string ZZ_ACTION_PACKED_0 = Resources.GetString("ZZ_ACTION_PACKED_0");


        /// <summary>
        ///     Translates a state to a row index in the transition table
        /// </summary>
        private static readonly int[] ZZ_ROWMAP = zzUnpackRowMap();


        private static readonly string ZZ_ROWMAP_PACKED_0 = Resources.GetString("ZZ_ROWMAP_PACKED_0");


        /// <summary>
        ///     The transition table of the DFA
        /// </summary>
        private static readonly int[] ZZ_TRANS = zzUnpackTrans();


        private static readonly string ZZ_TRANS_PACKED_0 = Resources.GetString("ZZ_TRANS_PACKED_0");

        private static readonly string ZZ_TRANS_PACKED_1 = Resources.GetString("ZZ_TRANS_PACKED_1");

        private static readonly string ZZ_TRANS_PACKED_2 = Resources.GetString("ZZ_TRANS_PACKED_2");

        private static readonly string ZZ_TRANS_PACKED_3 = Resources.GetString("ZZ_TRANS_PACKED_3");

        private static readonly string ZZ_TRANS_PACKED_4 = Resources.GetString("ZZ_TRANS_PACKED_4");

        private static readonly string ZZ_TRANS_PACKED_5 = Resources.GetString("ZZ_TRANS_PACKED_5");

        private static readonly string ZZ_TRANS_PACKED_6 = Resources.GetString("ZZ_TRANS_PACKED_6");

        private static readonly string ZZ_TRANS_PACKED_7 = Resources.GetString("ZZ_TRANS_PACKED_7");

        private static readonly string ZZ_TRANS_PACKED_8 = Resources.GetString("ZZ_TRANS_PACKED_8");

        private static readonly string ZZ_TRANS_PACKED_9 = Resources.GetString("ZZ_TRANS_PACKED_9");

        private static readonly string ZZ_TRANS_PACKED_10 = Resources.GetString("ZZ_TRANS_PACKED_10");

        private static readonly string ZZ_TRANS_PACKED_11 = Resources.GetString("ZZ_TRANS_PACKED_11");

        private static readonly string ZZ_TRANS_PACKED_12 = Resources.GetString("ZZ_TRANS_PACKED_12");

        private static readonly string ZZ_TRANS_PACKED_13 = Resources.GetString("ZZ_TRANS_PACKED_13");

        /* error codes */
        private static readonly int ZZ_UNKNOWN_ERROR = 0;
        private static readonly int ZZ_NO_MATCH = 1;
        private static readonly int ZZ_PUSHBACK_2BIG = 2;

        /* error messages for the codes above */

        private static readonly string[] ZZ_ERROR_MSG =
        {
            "Unkown internal scanner error",
            "Error: could not match input",
            "Error: pushback value was too large"
        };

        /**
       * ZZ_ATTRIBUTE[aState] Contains the attributes of state <code>aState</code>
       */
        private static readonly int[] ZZ_ATTRIBUTE = zzUnpackAttribute();


        private static readonly string ZZ_ATTRIBUTE_PACKED_0 = Resources.GetString("ZZ_ATTRIBUTE_PACKED_0");

        /* user code: */
        private static readonly IDictionary<String, String> upperCaseVariantsAccepted = new HashMap<String, String>();

        private static readonly CharArrayMap<char> entityValues = new CharArrayMap<char>(Version.LUCENE_40, 253, false);

        private static readonly int INITIAL_INPUT_SEGMENT_SIZE = 1024;
        private static readonly char BLOCK_LEVEL_START_TAG_REPLACEMENT = '\n';
        private static readonly char BLOCK_LEVEL_END_TAG_REPLACEMENT = '\n';
        private static readonly char BR_START_TAG_REPLACEMENT = '\n';
        private static readonly char BR_END_TAG_REPLACEMENT = '\n';
        private static readonly char SCRIPT_REPLACEMENT = '\n';
        private static readonly char STYLE_REPLACEMENT = '\n';
        private static readonly char REPLACEMENT_CHARACTER = '\uFFFD';
        private readonly TextSegment entitySegment = new TextSegment(2);


        private readonly bool escapeBR;
        private readonly bool escapeSCRIPT;
        private readonly bool escapeSTYLE;
        private readonly CharArraySet escapedTags;
        private readonly TextSegment inputSegment = new TextSegment(INITIAL_INPUT_SEGMENT_SIZE);
        private int cumulativeDiff;
        private int eofReturnValue;
        private int inputStart;
        private int outputCharCount;
        private TextSegment outputSegment;
        private int previousRestoreState;
        private int restoreState;
        private int yychar;

        /**
        * the number of characters from the last newline up to the start of the 
        * matched text
        */
        private int yycolumn;
        private int yyline;
        private bool zzAtBOL = true;

        /** zzAtEOF == true <=> the scanner is at the EOF */
        private bool zzAtEOF;
        private char[] zzBuffer = new char[ZZ_BUFFERSIZE];
        private int zzCurrentPos;
        private bool zzEOFDone;
        private int zzEndRead;
        private int zzLexicalState = YYINITIAL;
        private int zzMarkedPos;
        private StreamReader zzReader;
        private int zzStartRead;
        private int zzState;

        static HTMLStripCharFilter()
        {
            upperCaseVariantsAccepted.Add("quot", "QUOT");
            upperCaseVariantsAccepted.Add("copy", "COPY");
            upperCaseVariantsAccepted.Add("gt", "GT");
            upperCaseVariantsAccepted.Add("lt", "LT");
            upperCaseVariantsAccepted.Add("reg", "REG");
            upperCaseVariantsAccepted.Add("amp", "AMP");

            string[] entities =
            {
                "AElig", "\u00C6", "Aacute", "\u00C1", "Acirc", "\u00C2",
                "Agrave", "\u00C0", "Alpha", "\u0391", "Aring", "\u00C5",
                "Atilde", "\u00C3", "Auml", "\u00C4", "Beta", "\u0392",
                "Ccedil", "\u00C7", "Chi", "\u03A7", "Dagger", "\u2021",
                "Delta", "\u0394", "ETH", "\u00D0", "Eacute", "\u00C9",
                "Ecirc", "\u00CA", "Egrave", "\u00C8", "Epsilon", "\u0395",
                "Eta", "\u0397", "Euml", "\u00CB", "Gamma", "\u0393", "Iacute", "\u00CD",
                "Icirc", "\u00CE", "Igrave", "\u00CC", "Iota", "\u0399",
                "Iuml", "\u00CF", "Kappa", "\u039A", "Lambda", "\u039B", "Mu", "\u039C",
                "Ntilde", "\u00D1", "Nu", "\u039D", "OElig", "\u0152",
                "Oacute", "\u00D3", "Ocirc", "\u00D4", "Ograve", "\u00D2",
                "Omega", "\u03A9", "Omicron", "\u039F", "Oslash", "\u00D8",
                "Otilde", "\u00D5", "Ouml", "\u00D6", "Phi", "\u03A6", "Pi", "\u03A0",
                "Prime", "\u2033", "Psi", "\u03A8", "Rho", "\u03A1", "Scaron", "\u0160",
                "Sigma", "\u03A3", "THORN", "\u00DE", "Tau", "\u03A4", "Theta", "\u0398",
                "Uacute", "\u00DA", "Ucirc", "\u00DB", "Ugrave", "\u00D9",
                "Upsilon", "\u03A5", "Uuml", "\u00DC", "Xi", "\u039E",
                "Yacute", "\u00DD", "Yuml", "\u0178", "Zeta", "\u0396",
                "aacute", "\u00E1", "acirc", "\u00E2", "acute", "\u00B4",
                "aelig", "\u00E6", "agrave", "\u00E0", "alefsym", "\u2135",
                "alpha", "\u03B1", "amp", "\u0026", "and", "\u2227", "ang", "\u2220",
                "apos", "\u0027", "aring", "\u00E5", "asymp", "\u2248",
                "atilde", "\u00E3", "auml", "\u00E4", "bdquo", "\u201E",
                "beta", "\u03B2", "brvbar", "\u00A6", "bull", "\u2022", "cap", "\u2229",
                "ccedil", "\u00E7", "cedil", "\u00B8", "cent", "\u00A2", "chi", "\u03C7",
                "circ", "\u02C6", "clubs", "\u2663", "cong", "\u2245", "copy", "\u00A9",
                "crarr", "\u21B5", "cup", "\u222A", "curren", "\u00A4", "dArr", "\u21D3",
                "dagger", "\u2020", "darr", "\u2193", "deg", "\u00B0", "delta", "\u03B4",
                "diams", "\u2666", "divide", "\u00F7", "eacute", "\u00E9",
                "ecirc", "\u00EA", "egrave", "\u00E8", "empty", "\u2205",
                "emsp", "\u2003", "ensp", "\u2002", "epsilon", "\u03B5",
                "equiv", "\u2261", "eta", "\u03B7", "eth", "\u00F0", "euml", "\u00EB",
                "euro", "\u20AC", "exist", "\u2203", "fnof", "\u0192",
                "forall", "\u2200", "frac12", "\u00BD", "frac14", "\u00BC",
                "frac34", "\u00BE", "frasl", "\u2044", "gamma", "\u03B3", "ge", "\u2265",
                "gt", "\u003E", "hArr", "\u21D4", "harr", "\u2194", "hearts", "\u2665",
                "hellip", "\u2026", "iacute", "\u00ED", "icirc", "\u00EE",
                "iexcl", "\u00A1", "igrave", "\u00EC", "image", "\u2111",
                "infin", "\u221E", "int", "\u222B", "iota", "\u03B9", "iquest", "\u00BF",
                "isin", "\u2208", "iuml", "\u00EF", "kappa", "\u03BA", "lArr", "\u21D0",
                "lambda", "\u03BB", "lang", "\u2329", "laquo", "\u00AB",
                "larr", "\u2190", "lceil", "\u2308", "ldquo", "\u201C", "le", "\u2264",
                "lfloor", "\u230A", "lowast", "\u2217", "loz", "\u25CA", "lrm", "\u200E",
                "lsaquo", "\u2039", "lsquo", "\u2018", "lt", "\u003C", "macr", "\u00AF",
                "mdash", "\u2014", "micro", "\u00B5", "middot", "\u00B7",
                "minus", "\u2212", "mu", "\u03BC", "nabla", "\u2207", "nbsp", " ",
                "ndash", "\u2013", "ne", "\u2260", "ni", "\u220B", "not", "\u00AC",
                "notin", "\u2209", "nsub", "\u2284", "ntilde", "\u00F1", "nu", "\u03BD",
                "oacute", "\u00F3", "ocirc", "\u00F4", "oelig", "\u0153",
                "ograve", "\u00F2", "oline", "\u203E", "omega", "\u03C9",
                "omicron", "\u03BF", "oplus", "\u2295", "or", "\u2228", "ordf", "\u00AA",
                "ordm", "\u00BA", "oslash", "\u00F8", "otilde", "\u00F5",
                "otimes", "\u2297", "ouml", "\u00F6", "para", "\u00B6", "part", "\u2202",
                "permil", "\u2030", "perp", "\u22A5", "phi", "\u03C6", "pi", "\u03C0",
                "piv", "\u03D6", "plusmn", "\u00B1", "pound", "\u00A3",
                "prime", "\u2032", "prod", "\u220F", "prop", "\u221D", "psi", "\u03C8",
                "quot", "\"", "rArr", "\u21D2", "radic", "\u221A", "rang", "\u232A",
                "raquo", "\u00BB", "rarr", "\u2192", "rceil", "\u2309",
                "rdquo", "\u201D", "real", "\u211C", "reg", "\u00AE", "rfloor", "\u230B",
                "rho", "\u03C1", "rlm", "\u200F", "rsaquo", "\u203A", "rsquo", "\u2019",
                "sbquo", "\u201A", "scaron", "\u0161", "sdot", "\u22C5",
                "sect", "\u00A7", "shy", "\u00AD", "sigma", "\u03C3", "sigmaf", "\u03C2",
                "sim", "\u223C", "spades", "\u2660", "sub", "\u2282", "sube", "\u2286",
                "sum", "\u2211", "sup", "\u2283", "sup1", "\u00B9", "sup2", "\u00B2",
                "sup3", "\u00B3", "supe", "\u2287", "szlig", "\u00DF", "tau", "\u03C4",
                "there4", "\u2234", "theta", "\u03B8", "thetasym", "\u03D1",
                "thinsp", "\u2009", "thorn", "\u00FE", "tilde", "\u02DC",
                "times", "\u00D7", "trade", "\u2122", "uArr", "\u21D1",
                "uacute", "\u00FA", "uarr", "\u2191", "ucirc", "\u00FB",
                "ugrave", "\u00F9", "uml", "\u00A8", "upsih", "\u03D2",
                "upsilon", "\u03C5", "uuml", "\u00FC", "weierp", "\u2118",
                "xi", "\u03BE", "yacute", "\u00FD", "yen", "\u00A5", "yuml", "\u00FF",
                "zeta", "\u03B6", "zwj", "\u200D", "zwnj", "\u200C"
            };

            for (int i = 0; i < entities.Length; i += 2)
            {
                char value = entities[i + 1][0];
                entityValues.Add(entities[i], value);
                String upperCaseVariant = upperCaseVariantsAccepted[entities[i]];
                if (upperCaseVariant != null)
                {
                    entityValues.Add(upperCaseVariant, value);
                }
            }
        }


        /**
       * Creates a new HTMLStripCharFilter over the provided Reader.
       * @param source Reader to strip html tags from.
       */

        public HTMLStripCharFilter(StreamReader source)
            : base(source)
        {
            zzReader = source;
            outputSegment = inputSegment;
        }


        /**
           * Creates a new HTMLStripCharFilter over the provided Reader
           * with the specified start and end tags.
           * @param source Reader to strip html tags from.
           * @param escapedTags Tags in this set (both start and end tags)
           *  will not be filtered out.
           */

        public HTMLStripCharFilter(StreamReader source, ISet<string> escapedTags)
            : base(source)
        {
            zzReader = source;

            outputSegment = inputSegment;

            if (null != escapedTags)
            {
                foreach (string tag in escapedTags)
                {
                    if (tag.EqualsIgnoreCase("BR"))
                    {
                        escapeBR = true;
                    }
                    else if (tag.EqualsIgnoreCase("SCRIPT"))
                    {
                        escapeSCRIPT = true;
                    }
                    else if (tag.EqualsIgnoreCase("STYLE"))
                    {
                        escapeSTYLE = true;
                    }
                    else
                    {
                        if (null == this.escapedTags)
                        {
                            this.escapedTags = new CharArraySet(Version.LUCENE_40, 16, true);
                        }
                        this.escapedTags.Add(tag);
                    }
                }
            }
        }

        private static int[] zzUnpackAction()
        {
            var result = new int[14873];
            int offset = 0;
            offset = zzUnpackAction(ZZ_ACTION_PACKED_0, offset, result);
            return result;
        }

        private static int zzUnpackAction(String packed, int offset, int[] result)
        {
            int i = 0; /* index in packed string  */
            int j = offset; /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }

        private static int[] zzUnpackRowMap()
        {
            var result = new int[14873];
            int offset = 0;
            offset = zzUnpackRowMap(ZZ_ROWMAP_PACKED_0, offset, result);
            return result;
        }

        private static int zzUnpackRowMap(String packed, int offset, int[] result)
        {
            int i = 0; /* index in packed string  */
            int j = offset; /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int high = packed[i++] << 16;
                result[j++] = high | packed[i++];
            }
            return j;
        }

        private static int[] zzUnpackTrans()
        {
            var result = new int[2814164];
            int offset = 0;
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_0, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_1, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_2, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_3, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_4, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_5, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_6, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_7, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_8, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_9, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_10, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_11, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_12, offset, result);
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_13, offset, result);
            return result;
        }

        private static int zzUnpackTrans(String packed, int offset, int[] result)
        {
            int i = 0; /* index in packed string  */
            int j = offset; /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                value--;
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }

        private static int[] zzUnpackAttribute()
        {
            var result = new int[14873];
            int offset = 0;
            offset = zzUnpackAttribute(ZZ_ATTRIBUTE_PACKED_0, offset, result);
            return result;
        }

        private static int zzUnpackAttribute(String packed, int offset, int[] result)
        {
            int i = 0; /* index in packed string  */
            int j = offset; /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }


        public override int Read()
        {
            int ch;
            if (outputSegment.IsRead())
            {
                if (zzAtEOF)
                {
                    return -1;
                }
                ch = NextChar();
                ++outputCharCount;
                return ch;
            }
            ch = outputSegment.NextChar();
            ++outputCharCount;
            return ch;
        }

        public override int Read(char[] cbuf, int off, int len)
        {
            int i = 0;
            for (; i < len; ++i)
            {
                int ch = Read();
                if (ch == -1) break;
                cbuf[off++] = (char) ch;
            }
            return i > 0 ? i : (len == 0 ? 0 : -1);
        }


        public override void Close()
        {
            yyclose();
        }

        private static int GetInitialBufferSize()
        {
            // Package private, for testing purposes
            return ZZ_BUFFERSIZE;
        }


        /** 
       * Unpacks the compressed character translation table.
       *
       * @param packed   the packed character translation table
       * @return         the unpacked character translation table
       */

        private static char[] zzUnpackCMap(String packed)
        {
            var map = new char[0x10000];
            int i = 0; /* index in packed string  */
            int j = 0; /* index in unpacked array */
            while (i < 2778)
            {
                int count = packed[i++];
                char value = packed[i++];
                do map[j++] = value; while (--count > 0);
            }
            return map;
        }


        /**
   * Refills the input buffer.
   *
   * @return      <code>false</code>, iff there was new input.
   * 
   * @exception   java.io.IOException  if any I/O-Error occurs
   */

        private bool zzRefill()
        {
            /* first: make room (if you can) */
            if (zzStartRead > 0)
            {
                Array.Copy(zzBuffer, zzStartRead,
                    zzBuffer, 0,
                    zzEndRead - zzStartRead);

                /* translate stored positions */
                zzEndRead -= zzStartRead;
                zzCurrentPos -= zzStartRead;
                zzMarkedPos -= zzStartRead;
                zzStartRead = 0;
            }

            /* is the buffer big enough? */
            if (zzCurrentPos >= zzBuffer.Length)
            {
                /* if not: blow it up */
                var newBuffer = new char[zzCurrentPos*2];
                Array.Copy(zzBuffer, 0, newBuffer, 0, zzBuffer.Length);
                zzBuffer = newBuffer;
            }

            /* finally: fill the buffer with new input */
            int numRead = zzReader.Read(zzBuffer, zzEndRead,
                zzBuffer.Length - zzEndRead);

            if (numRead > 0)
            {
                zzEndRead += numRead;
                return false;
            }
            // unlikely but not impossible: read 0 characters, but not at end of stream    
            if (numRead == 0)
            {
                int c = zzReader.Read();
                if (c == -1)
                {
                    return true;
                }
                zzBuffer[zzEndRead++] = (char) c;
                return false;
            }

            // numRead < 0
            return true;
        }


        /**
           * Closes the input stream.
           */

        private void yyclose()
        {
            zzAtEOF = true; /* indicate end of file */
            zzEndRead = zzStartRead; /* invalidate buffer    */

            if (zzReader != null)
                zzReader.Close();
        }


        /**
       * Resets the scanner to read from a new input stream.
       * Does not close the old reader.
       *
       * All internal variables are reset, the old input stream 
       * <b>cannot</b> be reused (internal buffer is discarded and lost).
       * Lexical state is set to <tt>ZZ_INITIAL</tt>.
       *
       * Internal scan buffer is resized down to its initial length, if it has grown.
       *
       * @param reader   the new input stream 
       */

        private void yyreset(StreamReader reader)
        {
            zzReader = reader;
            zzAtBOL = true;
            zzAtEOF = false;
            zzEOFDone = false;
            zzEndRead = zzStartRead = 0;
            zzCurrentPos = zzMarkedPos = 0;
            yyline = yychar = yycolumn = 0;
            zzLexicalState = YYINITIAL;
            if (zzBuffer.Length > ZZ_BUFFERSIZE)
                zzBuffer = new char[ZZ_BUFFERSIZE];
        }


        /**
         * Returns the current lexical state.
         */

        private int yystate()
        {
            return zzLexicalState;
        }


        /**
       * Enters a new lexical state
       *
       * @param newState the new lexical state
       */

        private void yybegin(int newState)
        {
            zzLexicalState = newState;
        }


        /**
         * Returns the text matched by the current regular expression.
         */

        private String yytext()
        {
            return new String(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
        }


        /**
         * Returns the character at position <tt>pos</tt> from the 
         * matched text. 
         * 
         * It is equivalent to yytext().charAt(pos), but faster
         *
         * @param pos the position of the character to fetch. 
         *            A value from 0 to yylength()-1.
         *
         * @return the character at position pos
         */

        private char yycharat(int pos)
        {
            return zzBuffer[zzStartRead + pos];
        }


        /**
       * Returns the length of the matched text region.
       */

        private int yylength()
        {
            return zzMarkedPos - zzStartRead;
        }


        /**
         * Reports an error that occured while scanning.
         *
         * In a wellformed scanner (no or only correct usage of 
         * yypushback(int) and a match-all fallback rule) this method 
         * will only be called with things that "Can't Possibly Happen".
         * If this method is called, something is seriously wrong
         * (e.g. a JFlex bug producing a faulty scanner etc.).
         *
         * Usual syntax/scanner level error handling should be done
         * in error fallback rules.
         *
         * @param   errorCode  the code of the errormessage to display
         */

        private void zzScanError(int errorCode)
        {
            String message;
            try
            {
                message = ZZ_ERROR_MSG[errorCode];
            }
            catch (IndexOutOfRangeException e)
            {
                message = ZZ_ERROR_MSG[ZZ_UNKNOWN_ERROR];
            }

            throw new Exception(message);
        }

        /**
       * Pushes the specified amount of characters back into the input stream.
       *
       * They will be read again by then next call of the scanning method
       *
       * @param number  the number of characters to be read again.
       *                This number must not be greater than yylength()!
       */

        private void yypushback(int number)
        {
            if (number > yylength())
                zzScanError(ZZ_PUSHBACK_2BIG);

            zzMarkedPos -= number;
        }


        /**
   * Contains user EOF-code, which will be executed exactly once,
   * when the end of file is reached
   */

        private void zzDoEOF()
        {
            if (!zzEOFDone)
            {
                zzEOFDone = true;
                switch (zzLexicalState)
                {
                    case SCRIPT:
                    case COMMENT:
                    case SCRIPT_COMMENT:
                    case STYLE:
                    case STYLE_COMMENT:
                    case SINGLE_QUOTED_STRING:
                    case DOUBLE_QUOTED_STRING:
                    case END_TAG_TAIL_EXCLUDE:
                    case END_TAG_TAIL_SUBSTITUTE:
                    case START_TAG_TAIL_EXCLUDE:
                    case SERVER_SIDE_INCLUDE:
                    case START_TAG_TAIL_SUBSTITUTE:
                    {
                        // Exclude
                        // add (length of input that won't be output) [ - (substitution length) = 0 ]
                        cumulativeDiff += yychar - inputStart;
                        // position the correction at (already output length) [ + (substitution length) = 0 ]
                        AddOffCorrectMap(outputCharCount, cumulativeDiff);
                        outputSegment.Clear();
                        eofReturnValue = -1;
                        break;
                    }
                    case CHARACTER_REFERENCE_TAIL:
                    {
                        // Substitute
                        // At end of file, allow char refs without semicolons
                        // add (length of input that won't be output) - (substitution length)
                        cumulativeDiff += inputSegment.Length - outputSegment.Length;
                        // position the correction at (already output length) + (substitution length)
                        AddOffCorrectMap(outputCharCount + outputSegment.Length, cumulativeDiff);
                        eofReturnValue = outputSegment.NextChar();
                        break;
                    }
                    case BANG:
                    case CDATA:
                    case AMPERSAND:
                    case NUMERIC_CHARACTER:
                    case END_TAG_TAIL_INCLUDE:
                    case START_TAG_TAIL_INCLUDE:
                    case LEFT_ANGLE_BRACKET:
                    case LEFT_ANGLE_BRACKET_SLASH:
                    case LEFT_ANGLE_BRACKET_SPACE:
                    {
                        // Include
                        outputSegment = inputSegment;
                        eofReturnValue = outputSegment.NextChar();
                        break;
                    }
                    default:
                    {
                        eofReturnValue = -1;
                    }
                        break;
                }
            }
        }


        private void zzForAction()
        {
        }


        /**
   * Resumes scanning until the next regular expression is matched,
   * the end of input is encountered or an I/O-Error occurs.
   *
   * @return      the next token
   * @exception   java.io.IOException  if any I/O-Error occurs
   */

        private int NextChar()
        {
            int zzInput;
            int zzAction;

            // cached fields:
            int zzCurrentPosL;
            int zzMarkedPosL;
            int zzEndReadL = zzEndRead;
            char[] zzBufferL = zzBuffer;
            char[] zzCMapL = ZZ_CMAP;

            int[] zzTransL = ZZ_TRANS;
            int[] zzRowMapL = ZZ_ROWMAP;
            int[] zzAttrL = ZZ_ATTRIBUTE;

            while (true)
            {
                zzMarkedPosL = zzMarkedPos;

                yychar += zzMarkedPosL - zzStartRead;

                zzAction = -1;

                zzCurrentPosL = zzCurrentPos = zzStartRead = zzMarkedPosL;

                zzState = ZZ_LEXSTATE[zzLexicalState];

                // set up zzAction for empty match case:
                int zzAttributes = zzAttrL[zzState];
                if ((zzAttributes & 1) == 1)
                {
                    zzAction = zzState;
                }


                {
                    while (true)
                    {
                        if (zzCurrentPosL < zzEndReadL)
                            zzInput = zzBufferL[zzCurrentPosL++];
                        else if (zzAtEOF)
                        {
                            zzInput = YYEOF;
                            break;
                        }
                        else
                        {
                            // store back cached positions
                            zzCurrentPos = zzCurrentPosL;
                            zzMarkedPos = zzMarkedPosL;
                            bool eof = zzRefill();
                            // get translated positions and possibly new buffer
                            zzCurrentPosL = zzCurrentPos;
                            zzMarkedPosL = zzMarkedPos;
                            zzBufferL = zzBuffer;
                            zzEndReadL = zzEndRead;
                            if (eof)
                            {
                                zzInput = YYEOF;
                                break;
                            }
                            zzInput = zzBufferL[zzCurrentPosL++];
                        }
                        int zzNext = zzTransL[zzRowMapL[zzState] + zzCMapL[zzInput]];
                        if (zzNext == -1) break;
                        zzState = zzNext;

                        zzAttributes = zzAttrL[zzState];
                        if ((zzAttributes & 1) == 1)
                        {
                            zzAction = zzState;
                            zzMarkedPosL = zzCurrentPosL;
                            if ((zzAttributes & 8) == 8) break;
                        }
                    }
                }

                // store back cached position
                zzMarkedPos = zzMarkedPosL;

                switch (zzAction < 0 ? zzAction : ZZ_ACTION[zzAction])
                {
                    case 1:
                    {
                        return zzBuffer[zzStartRead];
                    }
                    case 54:
                        break;
                    case 2:
                    {
                        inputStart = yychar;
                        inputSegment.Clear();
                        inputSegment.Append('<');
                        yybegin(LEFT_ANGLE_BRACKET);
                    }
                        break;
                    case 55:
                        break;
                    case 3:
                    {
                        inputStart = yychar;
                        inputSegment.Clear();
                        inputSegment.Append('&');
                        yybegin(AMPERSAND);
                    }
                        break;
                    case 56:
                        break;
                    case 4:
                    {
                        yypushback(1);
                        outputSegment = inputSegment;
                        outputSegment.Restart();
                        yybegin(YYINITIAL);
                        return outputSegment.NextChar();
                    }
                    case 57:
                        break;
                    case 5:
                    {
                        inputSegment.Append('#');
                        yybegin(NUMERIC_CHARACTER);
                    }
                        break;
                    case 58:
                        break;
                    case 6:
                    {
                        int matchLength = yylength();
                        inputSegment.Write(zzBuffer, zzStartRead, matchLength);
                        if (matchLength <= 7)
                        {
                            // 0x10FFFF = 1114111: max 7 decimal chars
                            String decimalCharRef = yytext();
                            int codePoint = 0;
                            try
                            {
                                codePoint = int.Parse(decimalCharRef);
                            }
                            catch (Exception e)
                            {
                                //assert false: "Exception parsing code point '" + decimalCharRef + "'";
                            }
                            if (codePoint <= 0x10FFFF)
                            {
                                outputSegment = entitySegment;
                                outputSegment.Clear();
                                if (codePoint >= Character.MIN_SURROGATE
                                    && codePoint <= Character.MAX_SURROGATE)
                                {
                                    outputSegment.UnsafeWrite(REPLACEMENT_CHARACTER);
                                }
                                else
                                {
                                    outputSegment.Length =
                                        Character.ToChars(codePoint, outputSegment.GetArray(), 0);
                                }
                                yybegin(CHARACTER_REFERENCE_TAIL);
                            }
                            else
                            {
                                outputSegment = inputSegment;
                                yybegin(YYINITIAL);
                                return outputSegment.NextChar();
                            }
                        }
                        else
                        {
                            outputSegment = inputSegment;
                            yybegin(YYINITIAL);
                            return outputSegment.NextChar();
                        }
                    }
                        break;
                    case 59:
                        break;
                    case 7:
                    {
                        // add (previously matched input length) + (this match length) - (substitution length)
                        cumulativeDiff += inputSegment.Length + yylength() - outputSegment.Length;
                        // position the correction at (already output length) + (substitution length)
                        AddOffCorrectMap(outputCharCount + outputSegment.Length, cumulativeDiff);
                        yybegin(YYINITIAL);
                        return outputSegment.NextChar();
                    }
                    case 60:
                        break;
                    case 8:
                    {
                        inputSegment.Write(zzBuffer, zzStartRead, yylength());
                        if (null != escapedTags
                            && escapedTags.Contains(zzBuffer, zzStartRead, yylength()))
                        {
                            yybegin(START_TAG_TAIL_INCLUDE);
                        }
                        else
                        {
                            yybegin(START_TAG_TAIL_SUBSTITUTE);
                        }
                    }
                        break;
                    case 61:
                        break;
                    case 9:
                    {
                        inputSegment.Write(zzBuffer, zzStartRead, yylength());
                        if (null != escapedTags
                            && escapedTags.Contains(zzBuffer, zzStartRead, yylength()))
                        {
                            yybegin(START_TAG_TAIL_INCLUDE);
                        }
                        else
                        {
                            yybegin(START_TAG_TAIL_EXCLUDE);
                        }
                    }
                        break;
                    case 62:
                        break;
                    case 10:
                    {
                        inputSegment.Append('!');
                        yybegin(BANG);
                    }
                        break;
                    case 63:
                        break;
                    case 11:
                    {
                        inputSegment.Write(zzBuffer, zzStartRead, yylength());
                        yybegin(LEFT_ANGLE_BRACKET_SPACE);
                    }
                        break;
                    case 64:
                        break;
                    case 12:
                    {
                        inputSegment.Append('/');
                        yybegin(LEFT_ANGLE_BRACKET_SLASH);
                    }
                        break;
                    case 65:
                        break;
                    case 13:
                    {
                        inputSegment.Append(zzBuffer[zzStartRead]);
                    }
                        break;
                    case 66:
                        break;
                    case 14:
                    {
                        // add (previously matched input length) + (this match length) [ - (substitution length) = 0 ]
                        cumulativeDiff += inputSegment.Length + yylength();
                        // position the correction at (already output length) [ + (substitution length) = 0 ]
                        AddOffCorrectMap(outputCharCount, cumulativeDiff);
                        inputSegment.Clear();
                        yybegin(YYINITIAL);
                    }
                        break;
                    case 67:
                        break;
                    case 15:
                    {
                    }
                        break;
                    case 68:
                        break;
                    case 16:
                    {
                        restoreState = SCRIPT_COMMENT;
                        yybegin(SINGLE_QUOTED_STRING);
                    }
                        break;
                    case 69:
                        break;
                    case 17:
                    {
                        restoreState = SCRIPT_COMMENT;
                        yybegin(DOUBLE_QUOTED_STRING);
                    }
                        break;
                    case 70:
                        break;
                    case 18:
                    {
                        inputSegment.Write(zzBuffer, zzStartRead, yylength());
                        if (null != escapedTags
                            && escapedTags.Contains(zzBuffer, zzStartRead, yylength()))
                        {
                            yybegin(END_TAG_TAIL_INCLUDE);
                        }
                        else
                        {
                            yybegin(END_TAG_TAIL_SUBSTITUTE);
                        }
                    }
                        break;
                    case 71:
                        break;
                    case 19:
                    {
                        inputSegment.Write(zzBuffer, zzStartRead, yylength());
                        if (null != escapedTags
                            && escapedTags.Contains(zzBuffer, zzStartRead, yylength()))
                        {
                            yybegin(END_TAG_TAIL_INCLUDE);
                        }
                        else
                        {
                            yybegin(END_TAG_TAIL_EXCLUDE);
                        }
                    }
                        break;
                    case 72:
                        break;
                    case 20:
                    {
                        inputSegment.Write(zzBuffer, zzStartRead, yylength());
                    }
                        break;
                    case 73:
                        break;
                    case 21:
                    {
                        previousRestoreState = restoreState;
                        restoreState = SERVER_SIDE_INCLUDE;
                        yybegin(SINGLE_QUOTED_STRING);
                    }
                        break;
                    case 74:
                        break;
                    case 22:
                    {
                        previousRestoreState = restoreState;
                        restoreState = SERVER_SIDE_INCLUDE;
                        yybegin(DOUBLE_QUOTED_STRING);
                    }
                        break;
                    case 75:
                        break;
                    case 23:
                    {
                        yybegin(restoreState);
                        restoreState = previousRestoreState;
                    }
                        break;
                    case 76:
                        break;
                    case 24:
                    {
                        inputSegment.Write(zzBuffer, zzStartRead, yylength());
                        outputSegment = inputSegment;
                        yybegin(YYINITIAL);
                        return outputSegment.NextChar();
                    }
                    case 77:
                        break;
                    case 25:
                    {
                        // add (previously matched input length) + (this match length) - (substitution length)
                        cumulativeDiff += inputSegment.Length + yylength() - 1;
                        // position the correction at (already output length) + (substitution length)
                        AddOffCorrectMap(outputCharCount + 1, cumulativeDiff);
                        inputSegment.Clear();
                        yybegin(YYINITIAL);
                        return BLOCK_LEVEL_END_TAG_REPLACEMENT;
                    }
                    case 78:
                        break;
                    case 26:
                    {
                        // add (previously matched input length) + (this match length) [ - (substitution length) = 0 ]
                        cumulativeDiff += inputSegment.Length + yylength();
                        // position the correction at (already output length) [ + (substitution length) = 0 ]
                        AddOffCorrectMap(outputCharCount, cumulativeDiff);
                        inputSegment.Clear();
                        outputSegment = inputSegment;
                        yybegin(YYINITIAL);
                    }
                        break;
                    case 79:
                        break;
                    case 27:
                    {
                        // add (previously matched input length) + (this match length) - (substitution length)
                        cumulativeDiff += inputSegment.Length + yylength() - 1;
                        // position the correction at (already output length) + (substitution length)
                        AddOffCorrectMap(outputCharCount + 1, cumulativeDiff);
                        inputSegment.Clear();
                        yybegin(YYINITIAL);
                        return BLOCK_LEVEL_START_TAG_REPLACEMENT;
                    }
                    case 80:
                        break;
                    case 28:
                    {
                        restoreState = STYLE_COMMENT;
                        yybegin(SINGLE_QUOTED_STRING);
                    }
                        break;
                    case 81:
                        break;
                    case 29:
                    {
                        restoreState = STYLE_COMMENT;
                        yybegin(DOUBLE_QUOTED_STRING);
                    }
                        break;
                    case 82:
                        break;
                    case 30:
                    {
                        int length = yylength();
                        inputSegment.Write(zzBuffer, zzStartRead, length);
                        entitySegment.Clear();
                        char ch = entityValues.Get(zzBuffer, zzStartRead, length);
                        entitySegment.Append(ch);
                        outputSegment = entitySegment;
                        yybegin(CHARACTER_REFERENCE_TAIL);
                    }
                        break;
                    case 83:
                        break;
                    case 31:
                    {
                        int matchLength = yylength();
                        inputSegment.Write(zzBuffer, zzStartRead, matchLength);
                        if (matchLength <= 6)
                        {
                            // 10FFFF: max 6 hex chars
                            var hexCharRef
                                = new String(zzBuffer, zzStartRead + 1, matchLength - 1);
                            int codePoint = 0;
                            try
                            {
                                codePoint = int.Parse(hexCharRef, NumberStyles.HexNumber);
                            }
                            catch (Exception e)
                            {
                                //assert false: "Exception parsing hex code point '" + hexCharRef + "'";
                            }
                            if (codePoint <= 0x10FFFF)
                            {
                                outputSegment = entitySegment;
                                outputSegment.Clear();
                                if (codePoint >= Character.MIN_SURROGATE
                                    && codePoint <= Character.MAX_SURROGATE)
                                {
                                    outputSegment.UnsafeWrite(REPLACEMENT_CHARACTER);
                                }
                                else
                                {
                                    outputSegment.Length =
                                        Character.ToChars(codePoint, outputSegment.GetArray(), 0);
                                }
                                yybegin(CHARACTER_REFERENCE_TAIL);
                            }
                            else
                            {
                                outputSegment = inputSegment;
                                yybegin(YYINITIAL);
                                return outputSegment.NextChar();
                            }
                        }
                        else
                        {
                            outputSegment = inputSegment;
                            yybegin(YYINITIAL);
                            return outputSegment.NextChar();
                        }
                    }
                        break;
                    case 84:
                        break;
                    case 32:
                    {
                        yybegin(COMMENT);
                    }
                        break;
                    case 85:
                        break;
                    case 33:
                    {
                        yybegin(YYINITIAL);
                        if (escapeBR)
                        {
                            inputSegment.Write(zzBuffer, zzStartRead, yylength());
                            outputSegment = inputSegment;
                            return outputSegment.NextChar();
                        }
                        // add (previously matched input length) + (this match length) - (substitution length)
                        cumulativeDiff += inputSegment.Length + yylength() - 1;
                        // position the correction at (already output length) + (substitution length)
                        AddOffCorrectMap(outputCharCount + 1, cumulativeDiff);
                        inputSegment.Reset();
                        return BR_START_TAG_REPLACEMENT;
                    }
                    case 86:
                        break;
                    case 34:
                    {
                        // add (previously matched input length) + (this match length) [ - (substitution length) = 0]
                        cumulativeDiff += yychar - inputStart + yylength();
                        // position the correction at (already output length) [ + (substitution length) = 0]
                        AddOffCorrectMap(outputCharCount, cumulativeDiff);
                        inputSegment.Clear();
                        yybegin(YYINITIAL);
                    }
                        break;
                    case 87:
                        break;
                    case 35:
                    {
                        yybegin(SCRIPT);
                    }
                        break;
                    case 88:
                        break;
                    case 36:
                    {
                        yybegin(YYINITIAL);
                        if (escapeBR)
                        {
                            inputSegment.Write(zzBuffer, zzStartRead, yylength());
                            outputSegment = inputSegment;
                            return outputSegment.NextChar();
                        }
                        // add (previously matched input length) + (this match length) - (substitution length)
                        cumulativeDiff += inputSegment.Length + yylength() - 1;
                        // position the correction at (already output length) + (substitution length)
                        AddOffCorrectMap(outputCharCount + 1, cumulativeDiff);
                        inputSegment.Reset();
                        return BR_END_TAG_REPLACEMENT;
                    }
                    case 89:
                        break;
                    case 37:
                    {
                        // add (this match length) [ - (substitution length) = 0 ]
                        cumulativeDiff += yylength();
                        // position the correction at (already output length) [ + (substitution length) = 0 ]
                        AddOffCorrectMap(outputCharCount, cumulativeDiff);
                        yybegin(YYINITIAL);
                    }
                        break;
                    case 90:
                        break;
                    case 38:
                    {
                        yybegin(restoreState);
                    }
                        break;
                    case 91:
                        break;
                    case 39:
                    {
                        yybegin(STYLE);
                    }
                        break;
                    case 92:
                        break;
                    case 40:
                    {
                        yybegin(SCRIPT_COMMENT);
                    }
                        break;
                    case 93:
                        break;
                    case 41:
                    {
                        yybegin(STYLE_COMMENT);
                    }
                        break;
                    case 94:
                        break;
                    case 42:
                    {
                        restoreState = COMMENT;
                        yybegin(SERVER_SIDE_INCLUDE);
                    }
                        break;
                    case 95:
                        break;
                    case 43:
                    {
                        restoreState = SCRIPT_COMMENT;
                        yybegin(SERVER_SIDE_INCLUDE);
                    }
                        break;
                    case 96:
                        break;
                    case 44:
                    {
                        restoreState = STYLE_COMMENT;
                        yybegin(SERVER_SIDE_INCLUDE);
                    }
                        break;
                    case 97:
                        break;
                    case 45:
                    {
                        yybegin(STYLE);
                        if (escapeSTYLE)
                        {
                            inputSegment.Write(zzBuffer, zzStartRead, yylength());
                            outputSegment = inputSegment;
                            inputStart += 1 + yylength();
                            return outputSegment.NextChar();
                        }
                    }
                        break;
                    case 98:
                        break;
                    case 46:
                    {
                        yybegin(SCRIPT);
                        if (escapeSCRIPT)
                        {
                            inputSegment.Write(zzBuffer, zzStartRead, yylength());
                            outputSegment = inputSegment;
                            inputStart += 1 + yylength();
                            return outputSegment.NextChar();
                        }
                    }
                        break;
                    case 99:
                        break;
                    case 47:
                    {
                        // add (previously matched input length) + (this match length) [ - (substitution length) = 0 ]
                        cumulativeDiff += inputSegment.Length + yylength();
                        // position the correction at (already output length) [ + (substitution length) = 0 ]
                        AddOffCorrectMap(outputCharCount, cumulativeDiff);
                        inputSegment.Clear();
                        yybegin(CDATA);
                    }
                        break;
                    case 100:
                        break;
                    case 48:
                    {
                        inputSegment.Clear();
                        yybegin(YYINITIAL);
                        // add (previously matched input length) -- current match and substitution handled below
                        cumulativeDiff += yychar - inputStart;
                        // position the offset correction at (already output length) -- substitution handled below
                        int offsetCorrectionPos = outputCharCount;
                        int returnValue;
                        if (escapeSTYLE)
                        {
                            inputSegment.Write(zzBuffer, zzStartRead, yylength());
                            outputSegment = inputSegment;
                            returnValue = outputSegment.NextChar();
                        }
                        else
                        {
                            // add (this match length) - (substitution length)
                            cumulativeDiff += yylength() - 1;
                            // add (substitution length)
                            ++offsetCorrectionPos;
                            returnValue = STYLE_REPLACEMENT;
                        }
                        AddOffCorrectMap(offsetCorrectionPos, cumulativeDiff);
                        return returnValue;
                    }
                    case 101:
                        break;
                    case 49:
                    {
                        inputSegment.Clear();
                        yybegin(YYINITIAL);
                        // add (previously matched input length) -- current match and substitution handled below
                        cumulativeDiff += yychar - inputStart;
                        // position at (already output length) -- substitution handled below
                        int offsetCorrectionPos = outputCharCount;
                        int returnValue;
                        if (escapeSCRIPT)
                        {
                            inputSegment.Write(zzBuffer, zzStartRead, yylength());
                            outputSegment = inputSegment;
                            returnValue = outputSegment.NextChar();
                        }
                        else
                        {
                            // add (this match length) - (substitution length)
                            cumulativeDiff += yylength() - 1;
                            // add (substitution length)
                            ++offsetCorrectionPos;
                            returnValue = SCRIPT_REPLACEMENT;
                        }
                        AddOffCorrectMap(offsetCorrectionPos, cumulativeDiff);
                        return returnValue;
                    }
                    case 102:
                        break;
                    case 50:
                    {
                        // Handle paired UTF-16 surrogates.
                        outputSegment = entitySegment;
                        outputSegment.Clear();
                        String surrogatePair = yytext();
                        char highSurrogate = '\u0000';
                        try
                        {
                            highSurrogate = (char) int.Parse(surrogatePair.Substring(2, 6), NumberStyles.HexNumber);
                        }
                        catch (Exception e)
                        {
                            // should never happen
                            //assert false: "Exception parsing high surrogate '" +surrogatePair.Substring(2, 6) + "'";
                        }
                        try
                        {
                            outputSegment.UnsafeWrite
                                ((char) int.Parse(surrogatePair.Substring(10, 14), NumberStyles.HexNumber));
                        }
                        catch (Exception e)
                        {
                            // should never happen
                            //assert false: "Exception parsing low surrogate '" + surrogatePair.substring(10, 14) + "'";
                        }
                        // add (previously matched input length) + (this match length) - (substitution length)
                        cumulativeDiff += inputSegment.Length + yylength() - 2;
                        // position the correction at (already output length) + (substitution length)
                        AddOffCorrectMap(outputCharCount + 2, cumulativeDiff);
                        inputSegment.Clear();
                        yybegin(YYINITIAL);
                        return highSurrogate;
                    }
                    case 103:
                        break;
                    case 51:
                    {
                        // Handle paired UTF-16 surrogates.
                        String surrogatePair = yytext();
                        char highSurrogate = '\u0000';
                        char lowSurrogate = '\u0000';
                        try
                        {
                            highSurrogate = (char) int.Parse(surrogatePair.Substring(2, 6), NumberStyles.HexNumber);
                        }
                        catch (Exception e)
                        {
                            // should never happen
                            //assert false: "Exception parsing high surrogate '" + surrogatePair.substring(2, 6) + "'";
                        }
                        try
                        {
                            // Low surrogates are in decimal range [56320, 57343]
                            lowSurrogate = (char) int.Parse(surrogatePair.Substring(9, 14));
                        }
                        catch (Exception e)
                        {
                            // should never happen
                            //assert false: "Exception parsing low surrogate '" + surrogatePair.substring(9, 14) + "'";
                        }
                        if (Character.IsLowSurrogate(lowSurrogate))
                        {
                            outputSegment = entitySegment;
                            outputSegment.Clear();
                            outputSegment.UnsafeWrite(lowSurrogate);
                            // add (previously matched input length) + (this match length) - (substitution length)
                            cumulativeDiff += inputSegment.Length + yylength() - 2;
                            // position the correction at (already output length) + (substitution length)
                            AddOffCorrectMap(outputCharCount + 2, cumulativeDiff);
                            inputSegment.Clear();
                            yybegin(YYINITIAL);
                            return highSurrogate;
                        }
                        yypushback(surrogatePair.Length - 1); // Consume only '#'
                        inputSegment.Append('#');
                        yybegin(NUMERIC_CHARACTER);
                    }
                        break;
                    case 104:
                        break;
                    case 52:
                    {
                        // Handle paired UTF-16 surrogates.
                        String surrogatePair = yytext();
                        char highSurrogate = '\u0000';
                        try
                        {
                            // High surrogates are in decimal range [55296, 56319]
                            highSurrogate = (char) int.Parse(surrogatePair.Substring(1, 6));
                        }
                        catch (Exception e)
                        {
                            // should never happen
                            // assert false: "Exception parsing high surrogate '" + surrogatePair.substring(1, 6) + "'";
                        }
                        if (Character.IsHighSurrogate(highSurrogate))
                        {
                            outputSegment = entitySegment;
                            outputSegment.Clear();
                            try
                            {
                                outputSegment.UnsafeWrite
                                    ((char) int.Parse(surrogatePair.Substring(10, 14), NumberStyles.HexNumber));
                            }
                            catch (Exception e)
                            {
                                // should never happen
                                //  assert false: "Exception parsing low surrogate '" + surrogatePair.substring(10, 14) + "'";
                            }
                            // add (previously matched input length) + (this match length) - (substitution length)
                            cumulativeDiff += inputSegment.Length + yylength() - 2;
                            // position the correction at (already output length) + (substitution length)
                            AddOffCorrectMap(outputCharCount + 2, cumulativeDiff);
                            inputSegment.Clear();
                            yybegin(YYINITIAL);
                            return highSurrogate;
                        }
                        yypushback(surrogatePair.Length - 1); // Consume only '#'
                        inputSegment.Append('#');
                        yybegin(NUMERIC_CHARACTER);
                    }
                        break;
                    case 105:
                        break;
                    case 53:
                    {
                        // Handle paired UTF-16 surrogates.
                        String surrogatePair = yytext();
                        char highSurrogate = '\u0000';
                        try
                        {
                            // High surrogates are in decimal range [55296, 56319]
                            highSurrogate = (char) int.Parse(surrogatePair.Substring(1, 6));
                        }
                        catch (Exception e)
                        {
                            // should never happen
                            // assert false: "Exception parsing high surrogate '" + surrogatePair.substring(1, 6) + "'";
                        }
                        if (Character.IsHighSurrogate(highSurrogate))
                        {
                            char lowSurrogate = '\u0000';
                            try
                            {
                                // Low surrogates are in decimal range [56320, 57343]
                                lowSurrogate = (char) int.Parse(surrogatePair.Substring(9, 14));
                            }
                            catch (Exception e)
                            {
                                // should never happen
                                //assert false: "Exception parsing low surrogate '" + surrogatePair.substring(9, 14) + "'";
                            }
                            if (Character.IsLowSurrogate(lowSurrogate))
                            {
                                outputSegment = entitySegment;
                                outputSegment.Clear();
                                outputSegment.UnsafeWrite(lowSurrogate);
                                // add (previously matched input length) + (this match length) - (substitution length)
                                cumulativeDiff += inputSegment.Length + yylength() - 2;
                                // position the correction at (already output length) + (substitution length)
                                AddOffCorrectMap(outputCharCount + 2, cumulativeDiff);
                                inputSegment.Clear();
                                yybegin(YYINITIAL);
                                return highSurrogate;
                            }
                        }
                        yypushback(surrogatePair.Length - 1); // Consume only '#'
                        inputSegment.Append('#');
                        yybegin(NUMERIC_CHARACTER);
                    }
                        break;
                    case 106:
                        break;
                    default:
                        if (zzInput == YYEOF && zzStartRead == zzCurrentPos)
                        {
                            zzAtEOF = true;
                            zzDoEOF();
                            {
                                return eofReturnValue;
                            }
                        }
                        zzScanError(ZZ_NO_MATCH);
                        break;
                }
            }
        }

        private class TextSegment : OpenStringBuilder
        {
            /** The position from which the next char will be read. */
            private int pos;

            /** Wraps the given buffer and sets this.len to the given length. */

            public TextSegment(char[] buffer, int length)
                : base(buffer, length)
            {
            }

            /** Allocates an internal buffer of the given size. */

            public TextSegment(int size)
                : base(size)
            {
            }

            /** Sets len = 0 and pos = 0. */

            public void Clear()
            {
                Reset();
                Restart();
            }

            /** Sets pos = 0 */

            public void Restart()
            {
                pos = 0;
            }

            /** Returns the next char in the segment. */

            public int NextChar()
            {
                //assert (! isRead()): "Attempting to read past the end of a segment.";
                return buf[pos++];
            }

            /** Returns true when all characters in the text segment have been read */

            public bool IsRead()
            {
                return pos >= len;
            }
        }
    }
}