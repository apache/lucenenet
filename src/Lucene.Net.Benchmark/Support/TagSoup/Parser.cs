// This file is part of TagSoup and is Copyright 2002-2008 by John Cowan.
//
// TagSoup is licensed under the Apache License,
// Version 2.0.  You may obtain a copy of this license at
// http://www.apache.org/licenses/LICENSE-2.0 .  You may also have
// additional legal rights not granted by this license.
//
// TagSoup is distributed in the hope that it will be useful, but
// unless required by applicable law or agreed to in writing, TagSoup
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, either express or implied; not even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// 
// 
// The TagSoup parser

using J2N.Text;
using Lucene;
using Lucene.Net.Support;
using Sax;
using Sax.Ext;
using Sax.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace TagSoup
{
    /// <summary>
    ///   The SAX parser class.
    /// </summary>
    public class Parser : DefaultHandler, IScanHandler, IXMLReader, ILexicalHandler
    {
        // XMLReader implementation

        private IContentHandler theContentHandler;
        private ILexicalHandler theLexicalHandler;
        private IDTDHandler theDTDHandler;
        private IErrorHandler theErrorHandler;
        private IEntityResolver theEntityResolver;
        private Schema theSchema;
        private IScanner theScanner;
        private IAutoDetector theAutoDetector;

        // Default values for feature flags

        private const bool DEFAULT_NAMESPACES = true;
        private const bool DEFAULT_IGNORE_BOGONS = false;
        private const bool DEFAULT_BOGONS_EMPTY = false;
        private const bool DEFAULT_ROOT_BOGONS = true;
        private const bool DEFAULT_DEFAULT_ATTRIBUTES = true;
        private const bool DEFAULT_TRANSLATE_COLONS = false;
        private const bool DEFAULT_RESTART_ELEMENTS = true;
        private const bool DEFAULT_IGNORABLE_WHITESPACE = false;
        private const bool DEFAULT_CDATA_ELEMENTS = true;

        // Feature flags.  

        private bool namespaces = DEFAULT_NAMESPACES;
        private bool ignoreBogons = DEFAULT_IGNORE_BOGONS;
        private bool bogonsEmpty = DEFAULT_BOGONS_EMPTY;
        private bool rootBogons = DEFAULT_ROOT_BOGONS;
        private bool defaultAttributes = DEFAULT_DEFAULT_ATTRIBUTES;
        private bool translateColons = DEFAULT_TRANSLATE_COLONS;
        private bool restartElements = DEFAULT_RESTART_ELEMENTS;
        private bool ignorableWhitespace = DEFAULT_IGNORABLE_WHITESPACE;
        private bool cDataElements = DEFAULT_CDATA_ELEMENTS;

        /// <summary>
        ///   A value of "true" indicates namespace URIs and unprefixed local
        ///   names for element and attribute names will be available.
        /// </summary>
        public const string NAMESPACES_FEATURE = "http://xml.org/sax/features/namespaces";

        /// <summary>
        ///   A value of "true" indicates that XML qualified names (with prefixes)
        ///   and attributes (including xmlns* attributes) will be available.
        ///   We don't support this value.
        /// </summary>
        public const string NAMESPACE_PREFIXES_FEATURE = "http://xml.org/sax/features/namespace-prefixes";

        /// <summary>
        ///   Reports whether this parser processes external general entities
        ///   (it doe
        /// </summary>
        public const string EXTERNAL_GENERAL_ENTITIES_FEATURE = "http://xml.org/sax/features/external-general-entities";

        /// <summary>
        ///   Reports whether this parser processes external parameter entities
        ///   (it doesn't).
        /// </summary>
        public const string EXTERNAL_PARAMETER_ENTITIES_FEATURE = "http://xml.org/sax/features/external-parameter-entities";

        /// <summary>
        ///   May be examined only during a parse, after the startDocument()
        ///   callback has been completed; read-only. The value is true if
        ///   the document specified standalone="yes" in its XML declaration,
        ///   and otherwise is false.  (It's always false.)
        /// </summary>
        public const string IS_STANDALONE_FEATURE = "http://xml.org/sax/features/is-standalone";

        /// <summary>
        ///   A value of "true" indicates that the LexicalHandler will report
        ///   the beginning and end of parameter entities (it won't).
        /// </summary>
        public const string LEXICAL_HANDLER_PARAMETER_ENTITIES_FEATURE =
            "http://xml.org/sax/features/lexical-handler/parameter-entities";

        /// <summary>
        ///   A value of "true" indicates that system IDs in declarations will
        ///   be absolutized (relative to their base URIs) before reporting.
        ///   (This returns true but doesn't actually do anything.)
        /// </summary>
        public const string RESOLVE_DTD_URIS_FEATURE = "http://xml.org/sax/features/resolve-dtd-uris";

        /// <summary>
        /// Has a value of "true" if all XML names (for elements,
        /// prefixes, attributes, entities, notations, and local
        /// names), as well as Namespace URIs, will have been interned
        /// using <see cref="StringExtensions.Intern(string)" />. This supports fast testing of
        /// equality/inequality against string constants, rather than forcing
        /// slower calls to <see cref="string.Equals(object)" />.  (We always intern.)
        /// </summary>
        public const string STRING_INTERNING_FEATURE = "http://xml.org/sax/features/string-interning";

        /// <summary>
        /// Returns "true" if the Attributes objects passed by this
        /// parser in <see cref="IContentHandler.StartElement" /> implement the
        /// <see cref="Sax.Ext.IAttributes2" /> interface.	(They don't.)
        /// </summary>
        public const string USE_ATTRIBUTES2_FEATURE = "http://xml.org/sax/features/use-attributes2";

        /// <summary>
        ///   Returns "true" if the Locator objects passed by this parser
        ///   parser in <see cref="IContentHandler.SetDocumentLocator" /> implement the
        ///   <see cref="Sax.Ext.ILocator2" /> interface.  (They don't.)
        /// </summary>
        public const string USE_LOCATOR2_FEATURE = "http://xml.org/sax/features/use-locator2";
        /// <summary>
        ///   Returns "true" if, when setEntityResolver is given an object
        ///   implementing the  <see cref="Sax.Ext.IEntityResolver2" /> interface,
        ///   those new methods will be used.  (They won't be.)
        /// </summary>
        public const string USE_ENTITY_RESOLVER2_FEATURE = "http://xml.org/sax/features/use-entity-resolver2";

        /// <summary>
        ///   Controls whether the parser is reporting all validity errors
        ///   (We don't report any validity errors.)
        /// </summary>
        public const string VALIDATION_FEATURE = "http://xml.org/sax/features/validation";

        /// <summary>
        ///   Controls whether the parser reports Unicode normalization
        ///   errors as described in section 2.13 and Appendix B of the XML
        ///   1.1 Recommendation.  (We don't normalize.)
        /// </summary>
        public const string UNICODE_NORMALIZATION_CHECKING_FEATURE =
            "http://xml.org/sax/features/unicode-normalization-checking";

        /// <summary>
        ///   Controls whether, when the namespace-prefixes feature is set,
        ///   the parser treats namespace declaration attributes as being in
        ///   the http://www.w3.org/2000/xmlns/ namespace.  (It doesn't.)
        /// </summary>
        public const string XMLNS_URIS_FEATURE = "http://xml.org/sax/features/xmlns-uris";

        /// <summary>
        ///   Returns <c>true</c> if the parser supports both XML 1.1 and XML 1.0.
        ///   (Always <c>false</c>.)
        /// </summary>
        public const string XML11_FEATURE = "http://xml.org/sax/features/xml-1.1";

        /// <summary>
        ///   A value of <c>true</c> indicates that the parser will ignore
        ///   unknown elements.
        /// </summary>
        public const string IGNORE_BOGONS_FEATURE = "http://www.ccil.org/~cowan/tagsoup/features/ignore-bogons";

        /// <summary>
        ///   A value of <c>true</c> indicates that the parser will give unknown
        ///   elements a content model of EMPTY; a value of <c>false</c>, a
        ///   content model of ANY.
        /// </summary>
        public const string BOGONS_EMPTY_FEATURE = "http://www.ccil.org/~cowan/tagsoup/features/bogons-empty";

        /// <summary>
        ///   A value of <c>true</c> indicates that the parser will allow unknown
        ///   elements to be the root element.
        /// </summary>
        public const string ROOT_BOGONS_FEATURE = "http://www.ccil.org/~cowan/tagsoup/features/root-bogons";

        /// <summary>
        ///   A value of <c>true</c> indicates that the parser will return default
        ///   attribute values for missing attributes that have default values.
        /// </summary>
        public const string DEFAULT_ATTRIBUTES_FEATURE = "http://www.ccil.org/~cowan/tagsoup/features/default-attributes";

        /// <summary>
        ///   A value of <c>true</c> indicates that the parser will
        ///   translate colons into underscores in names.
        /// </summary>
        public const string TRANSLATE_COLONS_FEATURE = "http://www.ccil.org/~cowan/tagsoup/features/translate-colons";

        /// <summary>
        ///   A value of <c>true</c> indicates that the parser will
        ///   attempt to restart the restartable elements.
        /// </summary>
        public const string RESTART_ELEMENTS_FEATURE = "http://www.ccil.org/~cowan/tagsoup/features/restart-elements";

        /// <summary>
        ///   A value of "true" indicates that the parser will
        ///   transmit whitespace in element-only content via the SAX
        ///   ignorableWhitespace callback.  Normally this is not done,
        ///   because HTML is an SGML application and SGML suppresses
        ///   such whitespace.
        /// </summary>
        public const string IGNORABLE_WHITESPACE_FEATURE =
            "http://www.ccil.org/~cowan/tagsoup/features/ignorable-whitespace";

        /// <summary>
        ///   A value of "true" indicates that the parser will treat CDATA
        ///   elements specially.  Normally true, since the input is by
        ///   default HTML.
        /// </summary>
        public const string CDATA_ELEMENTS_FEATURE = "http://www.ccil.org/~cowan/tagsoup/features/cdata-elements";

        /// <summary>
        ///   Used to see some syntax events that are essential in some
        ///   applications: comments, CDATA delimiters, selected general
        ///   entity inclusions, and the start and end of the DTD (and
        ///   declaration of document element name). The Object must implement
        ///   <see cref="ILexicalHandler" />
        /// </summary>
        public const string LEXICAL_HANDLER_PROPERTY = "http://xml.org/sax/properties/lexical-handler";

        /// <summary>
        ///   Specifies the Scanner object this Parser uses.
        /// </summary>
        public const string SCANNER_PROPERTY = "http://www.ccil.org/~cowan/tagsoup/properties/scanner";

        /// <summary>
        ///   Specifies the Schema object this Parser uses.
        /// </summary>
        public const string SCHEMA_PROPERTY = "http://www.ccil.org/~cowan/tagsoup/properties/schema";

        /// <summary>
        ///   Specifies the AutoDetector (for encoding detection) this Parser uses.
        /// </summary>
        public const string AUTO_DETECTOR_PROPERTY = "http://www.ccil.org/~cowan/tagsoup/properties/auto-detector";


        // Due to sucky Java order of initialization issues, these
        // entries are maintained separately from the initial values of
        // the corresponding instance variables, but care must be taken
        // to keep them in sync.

        private readonly IDictionary<string, bool> features = new Dictionary<string, bool> {
            { NAMESPACES_FEATURE, DEFAULT_NAMESPACES },
            { NAMESPACE_PREFIXES_FEATURE, false },
            { EXTERNAL_GENERAL_ENTITIES_FEATURE, false },
            { EXTERNAL_PARAMETER_ENTITIES_FEATURE, false },
            { IS_STANDALONE_FEATURE, false },
            { LEXICAL_HANDLER_PARAMETER_ENTITIES_FEATURE, false },
            { RESOLVE_DTD_URIS_FEATURE, true },
            { STRING_INTERNING_FEATURE, true },
            { USE_ATTRIBUTES2_FEATURE, false },
            { USE_LOCATOR2_FEATURE, false },
            { USE_ENTITY_RESOLVER2_FEATURE, false },
            { VALIDATION_FEATURE, false },
            { XMLNS_URIS_FEATURE, false },
            { XML11_FEATURE, false },
            { IGNORE_BOGONS_FEATURE, DEFAULT_IGNORE_BOGONS },
            { BOGONS_EMPTY_FEATURE, DEFAULT_BOGONS_EMPTY },
            { ROOT_BOGONS_FEATURE, DEFAULT_ROOT_BOGONS },
            { DEFAULT_ATTRIBUTES_FEATURE, DEFAULT_DEFAULT_ATTRIBUTES },
            { TRANSLATE_COLONS_FEATURE, DEFAULT_TRANSLATE_COLONS },
            { RESTART_ELEMENTS_FEATURE, DEFAULT_RESTART_ELEMENTS },
            { IGNORABLE_WHITESPACE_FEATURE, DEFAULT_IGNORABLE_WHITESPACE },
            { CDATA_ELEMENTS_FEATURE, DEFAULT_CDATA_ELEMENTS },
        };

        public virtual bool GetFeature(string name)
        {
            if (features.TryGetValue(name, out bool value))
            {
                return value;
            }
            throw new SAXNotRecognizedException("Unknown feature " + name);
        }

        public virtual void SetFeature(string name, bool value)
        {
            if (false == features.ContainsKey(name))
            {
                throw new SAXNotRecognizedException("Unknown feature " + name);
            }
            features[name] = value;

            if (name.Equals(NAMESPACES_FEATURE, StringComparison.Ordinal))
            {
                namespaces = value;
            }
            else if (name.Equals(IGNORE_BOGONS_FEATURE, StringComparison.Ordinal))
            {
                ignoreBogons = value;
            }
            else if (name.Equals(BOGONS_EMPTY_FEATURE, StringComparison.Ordinal))
            {
                bogonsEmpty = value;
            }
            else if (name.Equals(ROOT_BOGONS_FEATURE, StringComparison.Ordinal))
            {
                rootBogons = value;
            }
            else if (name.Equals(DEFAULT_ATTRIBUTES_FEATURE, StringComparison.Ordinal))
            {
                defaultAttributes = value;
            }
            else if (name.Equals(TRANSLATE_COLONS_FEATURE, StringComparison.Ordinal))
            {
                translateColons = value;
            }
            else if (name.Equals(RESTART_ELEMENTS_FEATURE, StringComparison.Ordinal))
            {
                restartElements = value;
            }
            else if (name.Equals(IGNORABLE_WHITESPACE_FEATURE, StringComparison.Ordinal))
            {
                ignorableWhitespace = value;
            }
            else if (name.Equals(CDATA_ELEMENTS_FEATURE, StringComparison.Ordinal))
            {
                cDataElements = value;
            }
        }

        public virtual object GetProperty(string name)
        {
            // LUCENENET: Added guard clause
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (name.Equals(LEXICAL_HANDLER_PROPERTY, StringComparison.Ordinal))
            {
                return theLexicalHandler == this ? null : theLexicalHandler;
            }
            if (name.Equals(SCANNER_PROPERTY, StringComparison.Ordinal))
            {
                return theScanner;
            }
            if (name.Equals(SCHEMA_PROPERTY, StringComparison.Ordinal))
            {
                return theSchema;
            }
            if (name.Equals(AUTO_DETECTOR_PROPERTY, StringComparison.Ordinal))
            {
                return theAutoDetector;
            }
            throw new SAXNotRecognizedException("Unknown property " + name);
        }

        public virtual void SetProperty(string name, object value)
        {
            // LUCENENET: Added guard clauses
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (name.Equals(LEXICAL_HANDLER_PROPERTY, StringComparison.Ordinal))
            {
                if (value is null)
                {
                    theLexicalHandler = this;
                }
                else
                {
                    if (value is ILexicalHandler handler)
                    {
                        theLexicalHandler = handler;
                    }
                    else
                    {
                        throw new SAXNotSupportedException("Your lexical handler is not a ILexicalHandler");
                    }
                }
            }
            else if (name.Equals(SCANNER_PROPERTY, StringComparison.Ordinal))
            {
                if (value is IScanner scanner)
                {
                    theScanner = scanner;
                }
                else
                {
                    throw new SAXNotSupportedException("Your scanner is not a IScanner");
                }
            }
            else if (name.Equals(SCHEMA_PROPERTY, StringComparison.Ordinal))
            {
                if (value is Schema schema)
                {
                    theSchema = schema;
                }
                else
                {
                    throw new SAXNotSupportedException("Your schema is not a Schema");
                }
            }
            else if (name.Equals(AUTO_DETECTOR_PROPERTY, StringComparison.Ordinal))
            {
                if (value is IAutoDetector detector)
                {
                    theAutoDetector = detector;
                }
                else
                {
                    throw new SAXNotSupportedException("Your auto-detector is not an IAutoDetector");
                }
            }
            else
            {
                throw new SAXNotRecognizedException("Unknown property " + name);
            }
        }

        public virtual IEntityResolver EntityResolver
        {
            get => theEntityResolver == this ? null : theEntityResolver;
            set => theEntityResolver = value ?? this;
        }

        public virtual IDTDHandler DTDHandler
        {
            get => theDTDHandler == this ? null : theDTDHandler;
            set => theDTDHandler = value ?? this;
        }

        public virtual IContentHandler ContentHandler
        {
            get => theContentHandler == this ? null : theContentHandler;
            set => theContentHandler = value ?? this;
        }

        public virtual IErrorHandler ErrorHandler
        {
            get => theErrorHandler == this ? null : theErrorHandler;
            set => theErrorHandler = value ?? this;
        }

        public virtual void Parse(InputSource input)
        {
            // LUCENENET: Added guard clause
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            Setup();
            TextReader r = GetReader(input);
            theContentHandler.StartDocument();
            theScanner.ResetDocumentLocator(input.PublicId, input.SystemId);
            if (theScanner is ILocator locator)
            {
                theContentHandler.SetDocumentLocator(locator);
            }
            if (theSchema.Uri.Length > 0)
            {
                theContentHandler.StartPrefixMapping(theSchema.Prefix, theSchema.Uri);
            }
            theScanner.Scan(r, this);
        }

        public virtual void Parse(string systemId)
        {
            // LUCENENET: Added guard clause
            if (systemId is null)
                throw new ArgumentNullException(nameof(systemId));

            Parse(new InputSource(systemId));
        }

        // Sets up instance variables that haven't been set by setFeature
        private void Setup()
        {
            if (theSchema is null)
            {
                theSchema = new HTMLSchema();
            }
            if (theScanner is null)
            {
                theScanner = new HTMLScanner();
            }
            if (theAutoDetector is null)
            {
                theAutoDetector = new AutoDetectorDelegate(stream => new StreamReader(stream));
            }
            theStack = new Element(theSchema.GetElementType("<root>"), defaultAttributes);
            thePCDATA = new Element(theSchema.GetElementType("<pcdata>"), defaultAttributes);
            theNewElement = null;
            theAttributeName = null;
            thePITarget = null;
            theSaved = null;
            theEntity = 0;
            virginStack = true;
            theDoctypeName = theDoctypePublicId = theDoctypeSystemId = null;
        }

        /// <summary>
        /// Return a <see cref="TextReader"/> based on the contents of an <see cref="InputSource"/>
        /// Buffer the Stream
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private TextReader GetReader(InputSource s)
        {
            TextReader r = s.TextReader;
            Stream i = s.Stream;
            Encoding encoding = s.Encoding;
            string publicId = s.PublicId;
            string systemId = s.SystemId;
            if (r is null)
            {
                if (i is null)
                {
                    i = GetInputStream(publicId, systemId);
                }
                if (!(i is BufferedStream))
                {
                    i = new BufferedStream(i);
                }
                if (encoding is null)
                {
                    r = theAutoDetector.AutoDetectingReader(i);
                }
                else
                {
                    //try {
                    //TODO: Safe?
                    r = new StreamReader(i, encoding);
                    //  }
                    //catch (Exception e) when (e.IsUnsupportedEncodingException()) {
                    //  r = new StreamReader(i);
                    //  }
                }
            }
            //		r = new BufferedReader(r);
            return r;
        }

        /// <summary>
        ///   Get an Stream based on a publicId and a systemId
        ///   We don't process publicids (who uses them anyhow?)
        /// </summary>
        /// <param name="publicId"></param>
        /// <param name="systemId"></param>
        /// <returns></returns>
#pragma warning disable IDE0060 // Remove unused parameter
        private static Stream GetInputStream(string publicId, string systemId) // LUCENENET: CA1822: Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // LUCENENET: Added guard clause
            if (systemId is null)
                throw new ArgumentNullException(nameof(systemId));

            var basis = new Uri("file://" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
            var url = new Uri(basis, systemId);
            return new FileStream(url.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        // ScanHandler implementation

        private Element theNewElement;
        private string theAttributeName;
        private bool theDoctypeIsPresent;
        private string theDoctypePublicId;
        private string theDoctypeSystemId;
        private string theDoctypeName;
        private string thePITarget;
        private Element theStack;
        private Element theSaved;
        private Element thePCDATA;
        private int theEntity; // needs to support chars past U+FFFF


        public virtual void Adup(char[] buffer, int startIndex, int length)
        {
            if (theNewElement is null || theAttributeName is null)
            {
                return;
            }
            theNewElement.SetAttribute(theAttributeName, null, theAttributeName);
            theAttributeName = null;
        }

        public virtual void Aname(char[] buffer, int startIndex, int length)
        {
            if (theNewElement is null)
            {
                return;
            }
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            // Currently we don't rely on Schema to canonicalize
            // attribute names.
            theAttributeName = MakeName(buffer, startIndex, length).ToLowerInvariant();
            //		System.err.println("%% Attribute name " + theAttributeName);
        }

        public virtual void Aval(char[] buffer, int startIndex, int length)
        {
            if (theNewElement is null || theAttributeName is null)
            {
                return;
            }
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            var value = new string(buffer, startIndex, length);
            //		System.err.println("%% Attribute value [" + value + "]");
            value = ExpandEntities(value);
            theNewElement.SetAttribute(theAttributeName, null, value);
            theAttributeName = null;
            //		System.err.println("%% Aval done");
        }

        /// <summary>
        ///   Expand entity references in attribute values selectively.
        ///   Currently we expand a reference iff it is properly terminated
        ///   with a semicolon.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private string ExpandEntities(string src)
        {
            int refStart = -1;
            int len = src.Length;
            var dst = new char[len];
            int dstlen = 0;
            for (int i = 0; i < len; i++)
            {
                char ch = src[i];
                dst[dstlen++] = ch;
                //			System.err.print("i = " + i + ", d = " + dstlen + ", ch = [" + ch + "] ");
                if (ch == '&' && refStart == -1)
                {
                    // start of a ref excluding &
                    refStart = dstlen;
                    //				System.err.println("start of ref");
                }
                else if (refStart == -1)
                {
                    // not in a ref
                    //				System.err.println("not in ref");
                }
                else if (char.IsLetter(ch) || char.IsDigit(ch) || ch == '#')
                {
                    // valid entity char
                    //				System.err.println("valid");
                }
                else if (ch == ';')
                {
                    // properly terminated ref
                    //				System.err.print("got [" + new string(dst, refStart, dstlen-refStart-1) + "]");
                    int ent = LookupEntity(dst, refStart, dstlen - refStart - 1);
                    //				System.err.println(" = " + ent);
                    if (ent > 0xFFFF)
                    {
                        ent -= 0x10000;
                        dst[refStart - 1] = (char)((ent >> 10) + 0xD800);
                        dst[refStart] = (char)((ent & 0x3FF) + 0xDC00);
                        dstlen = refStart + 1;
                    }
                    else if (ent != 0)
                    {
                        dst[refStart - 1] = (char)ent;
                        dstlen = refStart;
                    }
                    refStart = -1;
                }
                else
                {
                    // improperly terminated ref
                    //				System.err.println("end of ref");
                    refStart = -1;
                }
            }
            return new string(dst, 0, dstlen);
        }

        public virtual void Entity(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theEntity = LookupEntity(buffer, startIndex, length);
        }

        /// <summary>
        ///   Process numeric character references,
        ///   deferring to the schema for named ones.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private int LookupEntity(char[] buffer, int startIndex, int length)
        {
            int result = 0;
            if (length < 1)
            {
                return result;
            }
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            //		System.err.println("%% Entity at " + startIndex + " " + length);
            //		System.err.println("%% Got entity [" + new string(buffer, startIndex, length) + "]");
            if (buffer[startIndex] == '#')
            {
                if (length > 1 && (buffer[startIndex + 1] == 'x' || buffer[startIndex + 1] == 'X'))
                {
                    // LUCENENET: don't allow int parsing to throw exceptions
                    if (J2N.Numerics.Int32.TryParse(buffer, startIndex + 2, length - 2, 16, out result))
                        return result;

                    return 0;
                }
                // LUCENENET: don't allow int parsing to throw exceptions
                if (J2N.Numerics.Int32.TryParse(buffer, startIndex + 1, length - 1, 10, out result))
                    return result;

                return 0;
            }
            return theSchema.GetEntity(new string(buffer, startIndex, length));
        }

        public virtual void EOF(char[] buffer, int startIndex, int length)
        {
            if (virginStack)
            {
                Rectify(thePCDATA);
            }
            while (theStack.Next != null)
            {
                Pop();
            }
            if (theSchema.Uri.Length > 0) // LUCENENET: CA1820: Test for empty strings using string length
            {
                theContentHandler.EndPrefixMapping(theSchema.Prefix);
            }
            theContentHandler.EndDocument();
        }

        public virtual void ETag(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            if (ETagCdata(buffer, startIndex, length))
            {
                return;
            }
            ETagBasic(buffer, startIndex, length);
        }

        private static readonly char[] etagchars = { '<', '/', '>' };
        public virtual bool ETagCdata(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            string currentName = theStack.Name;
            // If this is a CDATA element and the tag doesn't match,
            // or isn't properly formed (junk after the name),
            // restart CDATA mode and process the tag as characters.
            if (cDataElements && (theStack.Flags & Schema.F_CDATA) != 0)
            {
                bool realTag = (length == currentName.Length);
                if (realTag)
                {
                    for (int i = 0; i < length; i++)
                    {
                        if (char.ToLowerInvariant(buffer[startIndex + i]) != char.ToLowerInvariant(currentName[i]))
                        {
                            realTag = false;
                            break;
                        }
                    }
                }
                if (!realTag)
                {
                    theContentHandler.Characters(etagchars, 0, 2);
                    theContentHandler.Characters(buffer, startIndex, length);
                    theContentHandler.Characters(etagchars, 2, 1);
                    theScanner.StartCDATA();
                    return true;
                }
            }
            return false;
        }

        public virtual void ETagBasic(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theNewElement = null;
            string name;
            if (length != 0)
            {
                // Canonicalize case of name
                name = MakeName(buffer, startIndex, length);
                //			System.err.println("got etag [" + name + "]");
                ElementType type = theSchema.GetElementType(name);
                if (type is null)
                {
                    return; // mysterious end-tag
                }
                name = type.Name;
            }
            else
            {
                name = theStack.Name;
            }
            //		System.err.println("%% Got end of " + name);

            Element sp;
            bool inNoforce = false;
            for (sp = theStack; sp != null; sp = sp.Next)
            {
                if (sp.Name.Equals(name, StringComparison.Ordinal))
                {
                    break;
                }
                if ((sp.Flags & Schema.F_NOFORCE) != 0)
                {
                    inNoforce = true;
                }
            }

            if (sp is null)
            {
                return; // Ignore unknown etags
            }
            if (sp.Next is null || sp.Next.Next is null)
            {
                return;
            }
            if (inNoforce)
            {
                // inside an F_NOFORCE element?
                sp.Preclose(); // preclose the matching element
            }
            else
            {
                // restartably pop everything above us
                while (theStack != sp)
                {
                    RestartablyPop();
                }
                Pop();
            }
            // pop any preclosed elements now at the top
            while (theStack.IsPreclosed)
            {
                Pop();
            }
            Restart(null);
        }

        /// <summary>
        ///   Push restartables on the stack if possible
        ///   e is the next element to be started, if we know what it is
        /// </summary>
        /// <param name="e"></param>
        private void Restart(Element e)
        {
            while (theSaved != null && theStack.CanContain(theSaved) && (e is null || theSaved.CanContain(e)))
            {
                Element next = theSaved.Next;
                Push(theSaved);
                theSaved = next;
            }
        }

        /// <summary>
        ///   Pop the stack irrevocably
        /// </summary>
        private void Pop()
        {
            if (theStack is null)
            {
                return; // empty stack
            }
            string name = theStack.Name;
            string localName = theStack.LocalName;
            string ns = theStack.Namespace;
            string prefix = PrefixOf(name);

            //		System.err.println("%% Popping " + name);
            if (!namespaces)
            {
                ns = localName = "";
            }
            theContentHandler.EndElement(ns, localName, name);
            if (Foreign(prefix, ns))
            {
                theContentHandler.EndPrefixMapping(prefix);
                //			System.err.println("%% Unmapping [" + prefix + "] for elements to " + namespace);
            }
            Attributes atts = theStack.Attributes;
            for (int i = atts.Length - 1; i >= 0; i--)
            {
                string attNamespace = atts.GetURI(i);
                string attPrefix = PrefixOf(atts.GetQName(i));
                if (Foreign(attPrefix, attNamespace))
                {
                    theContentHandler.EndPrefixMapping(attPrefix);
                    //			System.err.println("%% Unmapping [" + attPrefix + "] for attributes to " + attNamespace);
                }
            }
            theStack = theStack.Next;
        }

        /// <summary>
        ///   Pop the stack restartably
        /// </summary>
        private void RestartablyPop()
        {
            Element popped = theStack;
            Pop();
            if (restartElements && (popped.Flags & Schema.F_RESTART) != 0)
            {
                popped.Anonymize();
                popped.Next = theSaved;
                theSaved = popped;
            }
        }

        // Push element onto stack
        private bool virginStack = true;
        private void Push(Element e)
        {
            string name = e.Name;
            string localName = e.LocalName;
            string ns = e.Namespace;
            string prefix = PrefixOf(name);

            //		System.err.println("%% Pushing " + name);
            e.Clean();
            if (!namespaces)
            {
                ns = localName = "";
            }
            if (virginStack && localName.Equals(theDoctypeName, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    theEntityResolver.ResolveEntity(theDoctypePublicId, theDoctypeSystemId);
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                } // Can't be thrown for root I believe.
            }
            if (Foreign(prefix, ns))
            {
                theContentHandler.StartPrefixMapping(prefix, ns);
                //			System.err.println("%% Mapping [" + prefix + "] for elements to " + namespace);
            }
            Attributes atts = e.Attributes;
            int len = atts.Length;
            for (int i = 0; i < len; i++)
            {
                string attNamespace = atts.GetURI(i);
                string attPrefix = PrefixOf(atts.GetQName(i));
                if (Foreign(attPrefix, attNamespace))
                {
                    theContentHandler.StartPrefixMapping(attPrefix, attNamespace);
                    //				System.err.println("%% Mapping [" + attPrefix + "] for attributes to " + attNamespace);
                }
            }
            theContentHandler.StartElement(ns, localName, name, e.Attributes);
            e.Next = theStack;
            theStack = e;
            virginStack = false;
            if (cDataElements && (theStack.Flags & Schema.F_CDATA) != 0)
            {
                theScanner.StartCDATA();
            }
        }

        /// <summary>
        ///   Get the prefix from a QName
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string PrefixOf(string name)
        {
            int i = name.IndexOf(':');
            string prefix = "";
            if (i != -1)
            {
                prefix = name.Substring(0, i);
            }
            //		System.err.println("%% " + prefix + " is prefix of " + name);
            return prefix;
        }

        /// <summary>
        ///   Return true if we have a foreign name
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        private bool Foreign(string prefix, string ns)
        {
            //		System.err.print("%% Testing " + prefix + " and " + namespace + " for foreignness -- ");
            bool foreign = !(prefix.Length == 0 || ns.Length == 0 || ns.Equals(theSchema.Uri, StringComparison.Ordinal)); // LUCENENET: CA1820: Test for empty strings using string length
            //		System.err.println(foreign);
            return foreign;
        }

        /// <summary>
        ///   Parsing the complete XML Document Type Definition is way too complex,
        ///   but for many simple cases we can extract something useful from it.
        ///   doctypedecl ::= '&lt;!DOCTYPE' S Name (S ExternalID)? S? ('[' intSubset ']' S?)? '>'
        ///   DeclSep ::= PEReference | S
        ///   intSubset ::= (markupdecl | DeclSep)*
        ///   markupdecl ::= elementdecl | AttlistDecl | EntityDecl | NotationDecl | PI | Comment
        ///   ExternalID ::= 'SYSTEM' S SystemLiteral | 'PUBLIC' S PubidLiteral S SystemLiteral
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        public virtual void Decl(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            var s = new string(buffer, startIndex, length);
            string name = null;
            string systemId = null;
            string publicId = null;
            string[] v = Split(s);
            if (v.Length > 0 && "DOCTYPE".Equals(v[0], StringComparison.OrdinalIgnoreCase))
            {
                if (theDoctypeIsPresent)
                {
                    return; // one doctype only!
                }
                theDoctypeIsPresent = true;
                if (v.Length > 1)
                {
                    name = v[1];
                    if (v.Length > 3 && "SYSTEM".Equals(v[2], StringComparison.Ordinal))
                    {
                        systemId = v[3];
                    }
                    else if (v.Length > 3 && "PUBLIC".Equals(v[2], StringComparison.Ordinal))
                    {
                        publicId = v[3];
                        if (v.Length > 4)
                        {
                            systemId = v[4];
                        }
                        else
                        {
                            systemId = "";
                        }
                    }
                }
            }
            publicId = TrimQuotes(publicId);
            systemId = TrimQuotes(systemId);
            if (name != null)
            {
                publicId = CleanPublicId(publicId);
                theLexicalHandler.StartDTD(name, publicId, systemId);
                theLexicalHandler.EndDTD();
                theDoctypeName = name;
                theDoctypePublicId = publicId;
                if (theScanner is ILocator locator)
                {
                    // Must resolve systemId
                    theDoctypeSystemId = locator.SystemId;
                    try
                    {
                        if (Uri.IsWellFormedUriString(theDoctypeSystemId, UriKind.Absolute))
                        {
                            theDoctypeSystemId = new Uri(new Uri(theDoctypeSystemId), systemId).ToString();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        // If the string is quoted, trim the quotes.
        private static string TrimQuotes(string value)
        {
            if (value is null)
            {
                return null;
            }
            int length = value.Length;
            if (length == 0)
            {
                return value;
            }
            char s = value[0];
            char e = value[length - 1];
            if (s == e && (s == '\'' || s == '"'))
            {
                value = value.Substring(1, value.Length - 1);
            }
            return value;
        }

        /// <summary>
        ///   Split the supplied string into words or phrases seperated by spaces.
        ///   Recognises quotes around a phrase and doesn't split it.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static string[] Split(string val)
        {
            val = val.Trim();
            if (val.Length == 0)
            {
                return Arrays.Empty<string>();
            }
            var l = new JCG.List<string>();
            int s = 0;
            int e; // LUCENENET: IDE0059: Remove unnecessary value assignment
            bool sq = false; // single quote
            bool dq = false; // double quote
            var lastc = (char)0;
            int len = val.Length;
            for (e = 0; e < len; e++)
            {
                char c = val[e];
                if (!dq && c == '\'' && lastc != '\\')
                {
                    sq = !sq;
                    if (s < 0)
                    {
                        s = e;
                    }
                }
                else if (!sq && c == '\"' && lastc != '\\')
                {
                    dq = !dq;
                    if (s < 0)
                    {
                        s = e;
                    }
                }
                else if (!sq && !dq)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        if (s >= 0)
                        {
                            l.Add(val.Substring(s, e - s));
                        }
                        s = -1;
                    }
                    else if (s < 0 && c != ' ')
                    {
                        s = e;
                    }
                }
                lastc = c;
            }
            l.Add(val.Substring(s, e - s));
            return l.ToArray();
        }

        private const string LEGAL = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-'()+,./:=?;!*#@$_%";

        /// <summary>
        ///   Replace junk in publicids with spaces
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static string CleanPublicId(string src) // LUCENENET: CA1822: Mark members as static
        {
            if (src is null)
            {
                return null;
            }
            int len = src.Length;
            var dst = new StringBuilder(len);
            bool suppressSpace = true;
            for (int i = 0; i < len; i++)
            {
                char ch = src[i];
                if (LEGAL.IndexOf(ch) != -1)
                {
                    // legal but not whitespace
                    dst.Append(ch);
                    suppressSpace = false;
                }
                else if (suppressSpace)
                {
                    // normalizable whitespace or junk
                }
                else
                {
                    dst.Append(' ');
                    suppressSpace = true;
                }
            }
            //		System.err.println("%% Publicid [" + dst.tostring().trim() + "]");
            return dst.ToString().Trim(); // trim any final junk whitespace
        }

        public virtual void GI(char[] buffer, int startIndex, int length)
        {
            if (theNewElement != null)
            {
                return;
            }
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            string name = MakeName(buffer, startIndex, length);
            if (name is null)
            {
                return;
            }
            ElementType type = theSchema.GetElementType(name);
            if (type is null)
            {
                // Suppress unknown elements if ignore-bogons is on
                if (ignoreBogons)
                {
                    return;
                }
                int bogonModel = (bogonsEmpty ? Schema.M_EMPTY : Schema.M_ANY);
                int bogonMemberOf = (rootBogons ? Schema.M_ANY : (Schema.M_ANY & ~Schema.M_ROOT));
                theSchema.ElementType(name, bogonModel, bogonMemberOf, 0);
                if (!rootBogons)
                {
                    theSchema.Parent(name, theSchema.RootElementType.Name);
                }
                type = theSchema.GetElementType(name);
            }

            theNewElement = new Element(type, defaultAttributes);
            //		System.err.println("%% Got GI " + theNewElement.name());
        }

        public virtual void CDSect(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theLexicalHandler.StartCDATA();
            PCDATA(buffer, startIndex, length);
            theLexicalHandler.EndCDATA();
        }

        public virtual void PCDATA(char[] buffer, int startIndex, int length)
        {
            if (length == 0)
            {
                return;
            }

            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            bool allWhite = true;
            for (int i = 0; i < length; i++)
            {
                if (!char.IsWhiteSpace(buffer[startIndex + i]))
                {
                    allWhite = false;
                    break; // LUCENENET: No need to check the rest
                }
            }
            if (allWhite && !theStack.CanContain(thePCDATA))
            {
                if (ignorableWhitespace)
                {
                    theContentHandler.IgnorableWhitespace(buffer, startIndex, length);
                }
            }
            else
            {
                Rectify(thePCDATA);
                theContentHandler.Characters(buffer, startIndex, length);
            }
        }

        public virtual void PITarget(char[] buffer, int startIndex, int length)
        {
            if (theNewElement != null)
            {
                return;
            }
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            thePITarget = MakeName(buffer, startIndex, length).Replace(':', '_');
        }

        public virtual void PI(char[] buffer, int startIndex, int length)
        {
            if (theNewElement != null || thePITarget is null)
            {
                return;
            }
            if ("xml".Equals(thePITarget, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            //		if (length > 0 && buffer[length - 1] == '?') System.err.println("%% Removing ? from PI");
            if (length > 0 && buffer[length - 1] == '?')
            {
                length--; // remove trailing ?
            }
            theContentHandler.ProcessingInstruction(thePITarget, new string(buffer, startIndex, length));
            thePITarget = null;
        }

        public virtual void STagC(char[] buffer, int startIndex, int length)
        {
            //		System.err.println("%% Start-tag");
            if (theNewElement is null)
            {
                return;
            }

            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            Rectify(theNewElement);
            if (theStack.Model == Schema.M_EMPTY)
            {
                // Force an immediate end tag
                ETagBasic(buffer, startIndex, length);
            }
        }

        public virtual void STagE(char[] buffer, int startIndex, int length)
        {
            //		System.err.println("%% Empty-tag");
            if (theNewElement is null)
            {
                return;
            }

            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            Rectify(theNewElement);
            // Force an immediate end tag
            ETagBasic(buffer, startIndex, length);
        }

        //private char[] theCommentBuffer = new char[2000]; // LUCENENET: Never read
        public virtual void Cmnt(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theLexicalHandler.Comment(buffer, startIndex, length);
        }

        /// <summary>
        ///   Rectify the stack, pushing and popping as needed
        ///   so that the argument can be safely pushed
        /// </summary>
        /// <param name="e"></param>
        private void Rectify(Element e)
        {
            Element sp;
            while (true)
            {
                for (sp = theStack; sp != null; sp = sp.Next)
                {
                    if (sp.CanContain(e))
                    {
                        break;
                    }
                }
                if (sp != null)
                {
                    break;
                }
                ElementType parentType = e.Parent;
                if (parentType is null)
                {
                    break;
                }
                var parent = new Element(parentType, defaultAttributes);
                //			System.err.println("%% Ascending from " + e.name() + " to " + parent.name());
                parent.Next = e;
                e = parent;
            }
            if (sp is null)
            {
                return; // don't know what to do
            }
            while (theStack != sp)
            {
                if (theStack is null || theStack.Next is null || theStack.Next.Next is null)
                {
                    break;
                }
                RestartablyPop();
            }
            while (e != null)
            {
                Element nexte = e.Next;
                if (!e.Name.Equals("<pcdata>", StringComparison.Ordinal))
                {
                    Push(e);
                }
                e = nexte;
                Restart(e);
            }
            theNewElement = null;
        }

        public virtual int GetEntity()
        {
            return theEntity;
        }

        /// <summary>
        ///   Return the argument as a valid XML name
        ///   This no longer lowercases the result: we depend on Schema to
        ///   canonicalize case.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private string MakeName(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            var dst = new StringBuilder(length + 2);
            bool seenColon = false;
            bool start = true;
            //		string src = new string(buffer, startIndex, length); // DEBUG
            for (; length-- > 0; startIndex++)
            {
                char ch = buffer[startIndex];
                if (char.IsLetter(ch) || ch == '_')
                {
                    start = false;
                    dst.Append(ch);
                }
                else if (char.IsDigit(ch) || ch == '-' || ch == '.')
                {
                    if (start)
                    {
                        dst.Append('_');
                    }
                    start = false;
                    dst.Append(ch);
                }
                else if (ch == ':' && !seenColon)
                {
                    seenColon = true;
                    if (start)
                    {
                        dst.Append('_');
                    }
                    start = true;
                    dst.Append(translateColons ? '_' : ch);
                }
            }
            int dstLength = dst.Length;
            if (dstLength == 0 || dst[dstLength - 1] == ':')
            {
                dst.Append('_');
            }
            //		System.err.println("Made name \"" + dst + "\" from \"" + src + "\"");
            return dst.ToString().Intern();
        }

        private class AutoDetectorDelegate : IAutoDetector
        {
            private readonly Func<Stream, StreamReader> _delegate;

            public AutoDetectorDelegate(Func<Stream, StreamReader> @delegate)
            {
                _delegate = @delegate;
            }

            public TextReader AutoDetectingReader(Stream stream)
            {
                return _delegate(stream);
            }
        }

        // Default LexicalHandler implementation

        public virtual void Comment(char[] ch, int start, int length)
        {
        }

        public virtual void EndCDATA()
        {
        }

        public virtual void EndDTD()
        {
        }

        public virtual void EndEntity(string name)
        {
        }

        public virtual void StartCDATA()
        {
        }

        public virtual void StartDTD(string name, string publicId, string systemId)
        {
        }

        public virtual void StartEntity(string name)
        {
        }

        /// <summary>
        ///  Creates a new instance of <see cref="Parser" />
        /// </summary>
        public Parser()
        {
            theNewElement = null;
            theContentHandler = this;
            theLexicalHandler = this;
            theDTDHandler = this;
            theErrorHandler = this;
            theEntityResolver = this;
        }
    }
}
