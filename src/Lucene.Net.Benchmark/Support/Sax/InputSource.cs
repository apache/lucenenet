// SAX input source.
// http://www.saxproject.org
// No warranty; no copyright -- use this as you will.
// $Id: InputSource.java,v 1.10 2004/12/02 02:49:58 dmegginson Exp $

using System.IO;
using System.Text;

namespace Sax
{
    /// <summary>
    /// A single input source for an XML entity.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This class allows a SAX application to encapsulate information
    /// about an input source in a single object, which may include
    /// a public identifier, a system identifier, a byte stream (possibly
    /// with a specified encoding), and/or a character stream.
    /// <para/>
    /// There are two places that the application can deliver an
    /// input source to the parser: as the argument to the IParser.Parse(InputSource)
    /// method, or as the return value of the <see cref="IEntityResolver.ResolveEntity(string, string)"/>
    /// method.
    /// <para/>
    /// The SAX parser will use the InputSource object to determine how
    /// to read XML input. If there is a character stream available, the
    /// parser will read that stream directly, disregarding any text
    /// encoding declaration found in that stream.
    /// If there is no character stream, but there is
    /// a byte stream, the parser will use that byte stream, using the
    /// encoding specified in the <see cref="InputSource"/> or else (if no encoding is
    /// specified) autodetecting the character encoding using an algorithm
    /// such as the one in the XML specification. If neither a character
    /// stream nor a
    /// byte stream is available, the parser will attempt to open a URL
    /// connection to the resource identified by the system
    /// identifier.
    /// <para/>
    /// An <see cref="InputSource"/> object belongs to the application: the SAX parser
    /// shall never modify it in any way (it may modify a copy if 
    /// necessary).  However, standard processing of both byte and
    /// character streams is to close them on as part of end-of-parse cleanup,
    /// so applications should not attempt to re-use such streams after they
    /// have been handed to a parser.
    /// </remarks>
    /// <since>SAX 1.0</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1 (sax2r2)</version>
    /// <seealso cref="IXMLReader.Parse(InputSource)"/>
    /// <seealso cref="IEntityResolver.ResolveEntity(string, string)"/>
    /// <seealso cref="System.IO.Stream"/>
    /// <seealso cref="System.IO.TextReader"/>
    public class InputSource
    {
        /// <summary>
        /// Zero-argument default constructor.
        /// </summary>
        /// <seealso cref="PublicId"/>
        /// <seealso cref="SystemId"/>
        /// <seealso cref="Stream"/>
        /// <seealso cref="TextReader"/>
        /// <seealso cref="Encoding"/>
        public InputSource()
        {
        }

        /// <summary>
        /// Create a new input source with a system identifier.
        /// </summary>
        /// <remarks>
        /// Applications may use <see cref="PublicId"/> to include a 
        /// public identifier as well, or <see cref="Encoding"/> to specify
        /// the character encoding, if known.
        /// <para/>
        /// If the system identifier is a URL, it must be fully
        /// resolved (it may not be a relative URL).
        /// </remarks>
        /// <param name="systemId">The system identifier (URI).</param>
        /// <seealso cref="PublicId"/>
        /// <seealso cref="SystemId"/>
        /// <seealso cref="Stream"/>
        /// <seealso cref="TextReader"/>
        /// <seealso cref="Encoding"/>
        public InputSource(string systemId)
        {
            this.systemId = systemId;
        }

        /// <summary>
        /// Create a new input source with a byte stream.
        /// </summary>
        /// <remarks>
        /// Application writers should use <see cref="SystemId"/> to provide a base 
        /// for resolving relative URIs, may use <see cref="PublicId"/> to include a 
        /// public identifier, and may use <see cref="Encoding"/> to specify the object's
        /// character encoding.
        /// </remarks>
        /// <param name="byteStream">The raw byte stream containing the document.</param>
        /// <seealso cref="PublicId"/>
        /// <seealso cref="SystemId"/>
        /// <seealso cref="Encoding"/>
        /// <seealso cref="Stream"/>
        /// <seealso cref="TextReader"/>
        public InputSource(Stream byteStream)
        {
            this.byteStream = byteStream;
        }

        /// <summary>
        /// Create a new input source with a character stream.
        /// </summary>
        /// <remarks>
        /// Application writers should use <see cref="SystemId"/> to provide a base 
        /// for resolving relative URIs, and may use <see cref="PublicId"/> to include a
        /// public identifier.
        /// <para/>
        /// The character stream shall not include a byte order mark.
        /// </remarks>
        /// <param name="characterStream"></param>
        /// <seealso cref="PublicId"/>
        /// <seealso cref="SystemId"/>
        /// <seealso cref="Stream"/>
        /// <seealso cref="TextReader"/>
        public InputSource(TextReader characterStream)
        {
            this.characterStream = characterStream;
        }

        /// <summary>
        /// Gets or Sets the public identifier for this input source.
        /// </summary>
        /// <remarks>
        /// The public identifier is always optional: if the application
        /// writer includes one, it will be provided as part of the
        /// location information.
        /// </remarks>
        /// <seealso cref="ILocator.PublicId"/>
        /// <seealso cref="SAXParseException.PublicId"/>
        public virtual string PublicId
        {
            get => publicId;
            set => publicId = value;
        }

        /// <summary>
        /// Gets or Sets the system identifier for this input source.
        /// </summary>
        /// <remarks>
        /// The system identifier is optional if there is a byte stream
        /// or a character stream, but it is still useful to provide one,
        /// since the application can use it to resolve relative URIs
        /// and can include it in error messages and warnings(the parser
        /// will attempt to open a connection to the URI only if
        /// there is no byte stream or character stream specified).
        /// <para/>
        /// If the application knows the character encoding of the
        /// object pointed to by the system identifier, it can register
        /// the encoding using the <see cref="Encoding"/> property setter.
        /// <para/>
        /// If the system identifier is a URL, it must be fully
        /// resolved(it may not be a relative URL).
        /// </remarks>
        /// <seealso cref="Encoding"/>
        /// <seealso cref="SystemId"/>
        /// <seealso cref="ILocator.SystemId"/>
        /// <seealso cref="SAXParseException.SystemId"/>
        public virtual string SystemId
        {
            get => systemId;
            set => systemId = value;
        }

        /// <summary>
        /// Gets or Sets the byte stream for this input source.
        /// </summary>
        /// <remarks>
        /// The SAX parser will ignore this if there is also a character
        /// stream specified, but it will use a byte stream in preference
        /// to opening a URI connection itself.
        /// <para/>
        /// If the application knows the character encoding of the
        /// byte stream, it should set it with the setEncoding method.
        /// </remarks>
        /// <seealso cref="Encoding"/>
        /// <seealso cref="Stream"/>
        /// <seealso cref="System.IO.Stream"/>
        public virtual Stream Stream
        {
            get => byteStream;
            set => byteStream = value;
        }

        /// <summary>
        /// Gets or Sets the character encoding.
        /// </summary>
        /// <remarks>
        /// The encoding must be a string acceptable for an
        /// XML encoding declaration(see section 4.3.3 of the XML 1.0
        /// recommendation).
        /// <para/>
        /// This method has no effect when the application provides a
        /// character stream.
        /// </remarks>
        /// <seealso cref="SystemId"/>
        /// <seealso cref="Stream"/>
        public virtual Encoding Encoding
        {
            get => encoding;
            set => encoding = value;
        }

        /// <summary>
        /// Gets or Sets the character stream for this input source.
        /// </summary>
        /// <remarks>
        /// If there is a character stream specified, the SAX parser
        /// will ignore any byte stream and will not attempt to open
        /// a URI connection to the system identifier.
        /// </remarks>
        /// <seealso cref="System.IO.TextReader"/>
        public virtual TextReader TextReader
        {
            get => characterStream;
            set => characterStream = value;
        }

        ////////////////////////////////////////////////////////////////////
        // Internal state.
        ////////////////////////////////////////////////////////////////////

        private string publicId;
        private string systemId;
        private Stream byteStream;
        private Encoding encoding;
        private TextReader characterStream;
    }
}
