// XMLFilter.java - filter SAX2 events.
// http://www.saxproject.org
// Written by David Megginson
// NO WARRANTY!  This class is in the Public Domain.
// $Id: XMLFilter.java,v 1.6 2002/01/30 21:13:48 dbrownell Exp $

using System.IO;

namespace Sax
{
    /// <summary>
    /// Interface for an XML filter.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// An XML filter is like an XML reader, except that it obtains its
    /// events from another XML reader rather than a primary source like
    /// an XML document or database.Filters can modify a stream of
    /// events as they pass on to the final application.
    /// <para/>
    /// The <see cref="IXMLFilter"/> helper class provides a convenient base
    /// for creating SAX2 filters, by passing on all <see cref="IEntityResolver"/>, 
    /// <see cref="IDTDHandler"/>,
    /// <see cref="IContentHandler"/> and <see cref="IErrorHandler"/>
    /// events automatically.
    /// </remarks>
    /// <since>SAX 2.0</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1 (sax2r2)</version>
    /// <seealso cref="Helpers.XMLFilter"/>
    public interface IXMLReader
    {
        ////////////////////////////////////////////////////////////////////
        // Configuration.
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Look up the value of a feature flag.
        /// </summary>
        /// <remarks>
        /// The feature name is any fully-qualified URI.  It is
        /// possible for an XMLReader to recognize a feature name but
        /// temporarily be unable to return its value.
        /// Some feature values may be available only in specific
        /// contexts, such as before, during, or after a parse.
        /// Also, some feature values may not be programmatically accessible.
        /// (In the case of an adapter for SAX1 {@link Parser}, there is no
        /// implementation-independent way to expose whether the underlying
        /// parser is performing validation, expanding external entities,
        /// and so forth.)
        /// <para/>All XMLReaders are required to recognize the
        /// http://xml.org/sax/features/namespaces and the
        /// http://xml.org/sax/features/namespace-prefixes feature names.
        /// <para/>Typical usage is something like this:
        /// <code>
        /// XMLReader r = new MySAXDriver();
        ///                         // try to activate validation
        /// try {
        ///    r.SetFeature("http://xml.org/sax/features/validation", true);
        /// } catch (SAXException e) {
        ///    Console.Error.WriteLine("Cannot activate validation."); 
        /// }
        ///                         // register event handlers
        /// r.ContentHandler = new MyContentHandler();
        /// r.ErrorHandler = new MyErrorHandler();
        ///                         // parse the first document
        /// try {
        ///    r.Parse("http://www.foo.com/mydoc.xml");
        /// } catch (IOException e) {
        ///    Console.Error.WriteLine("I/O exception reading XML document");
        /// } catch (SAXException e) {
        ///    Console.Error.WriteLine("XML exception reading document.");
        /// }
        /// </code>
        /// <para/>Implementors are free (and encouraged) to invent their own features,
        /// using names built on their own URIs.
        /// </remarks>
        /// <param name="name">The feature name, which is a fully-qualified URI.</param>
        /// <returns>The current value of the feature (true or false).</returns>
        /// <exception cref="SAXNotRecognizedException">If the feature
        /// value can't be assigned or retrieved.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// <see cref="IXMLReader"/> recognizes the feature name but
        /// cannot determine its value at this time.</exception>
        /// <seealso cref="SetFeature(string, bool)"/>
        bool GetFeature(string name);


        /// <summary>
        /// Set the value of a feature flag.
        /// <para/>
        /// The feature name is any fully-qualified URI.  It is
        /// possible for an XMLReader to expose a feature value but
        /// to be unable to change the current value.
        /// Some feature values may be immutable or mutable only 
        /// in specific contexts, such as before, during, or after 
        /// a parse.
        /// <para/>
        /// All XMLReaders are required to support setting
        /// http://xml.org/sax/features/namespaces to true and
        /// http://xml.org/sax/features/namespace-prefixes to false.
        /// </summary>
        /// <param name="name">The feature name, which is a fully-qualified URI.</param>
        /// <param name="value">The requested value of the feature (true or false).</param>
        /// <exception cref="SAXNotRecognizedException">If the feature
        /// value can't be assigned or retrieved.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// <see cref="IXMLReader"/> recognizes the feature name but
        /// cannot set the requested value.</exception>
        /// <seealso cref="GetFeature(string)"/>
        void SetFeature(string name, bool value);


        /// <summary>
        /// Look up the value of a property.
        /// </summary>
        /// <remarks>
        /// The property name is any fully-qualified URI.  It is
        /// possible for an XMLReader to recognize a property name but
        /// temporarily be unable to return its value.
        /// Some property values may be available only in specific
        /// contexts, such as before, during, or after a parse.
        /// <para/>
        /// <see cref="IXMLReader"/>s are not required to recognize any specific
        /// property names, though an initial core set is documented for
        /// SAX2.
        /// <para/>
        /// Implementors are free (and encouraged) to invent their own properties,
        /// using names built on their own URIs.
        /// </remarks>
        /// <param name="name">The property name, which is a fully-qualified URI.</param>
        /// <returns>The current value of the property.</returns>
        /// <exception cref="SAXNotRecognizedException">If the property
        /// value can't be assigned or retrieved.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// <see cref="IXMLReader"/> recognizes the property name but 
        /// cannot determine its value at this time.</exception>
        /// <seealso cref="SetProperty(string, object)"/>
        object GetProperty(string name);


        /// <summary>
        /// Set the value of a property.
        /// </summary>
        /// <remarks>
        /// The property name is any fully-qualified URI.  It is
        /// possible for an <see cref="IXMLReader"/> to recognize a property name but
        /// to be unable to change the current value.
        /// Some property values may be immutable or mutable only 
        /// in specific contexts, such as before, during, or after 
        /// a parse.
        /// <para/>
        /// <see cref="IXMLReader"/>s are not required to recognize setting
        /// any specific property names, though a core set is defined by 
        /// SAX2.
        /// <para/>
        /// This method is also the standard mechanism for setting
        /// extended handlers.
        /// </remarks>
        /// <param name="name">The property name, which is a fully-qualified URI.</param>
        /// <param name="value">The requested value for the property.</param>
        /// <exception cref="SAXNotRecognizedException">If the property
        /// value can't be assigned or retrieved.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// <see cref="IXMLReader"/> recognizes the property name but
        /// cannot set the requested value.</exception>
        void SetProperty(string name, object value);



        ////////////////////////////////////////////////////////////////////
        // Event handlers.
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Gets or Sets an entity resolver.
        /// </summary>
        /// <remarks>
        /// If the application does not register an entity resolver,
        /// the <see cref="IXMLReader"/> will perform its own default resolution.
        /// <para/>
        /// Applications may register a new or different resolver in the
        /// middle of a parse, and the SAX parser must begin using the new
        /// resolver immediately.
        /// </remarks>
        IEntityResolver EntityResolver { get; set; }

        /// <summary>
        /// Gets or Sets a DTD event handler.
        /// </summary>
        /// <remarks>
        /// If the application does not register a DTD handler, all DTD
        /// events reported by the SAX parser will be silently ignored.
        /// <para/>
        /// Applications may register a new or different handler in the
        /// middle of a parse, and the SAX parser must begin using the new
        /// handler immediately.
        /// </remarks>
        IDTDHandler DTDHandler { get; set; }

        /// <summary>
        /// Gets or Sets a content event handler.
        /// </summary>
        /// <remarks>
        /// <para/>If the application does not register a content handler, all
        /// content events reported by the SAX parser will be silently
        /// ignored.
        /// <para/>Applications may register a new or different handler in the
        /// middle of a parse, and the SAX parser must begin using the new
        /// handler immediately.
        /// </remarks>
        IContentHandler ContentHandler { get; set; }


        /// <summary>
        /// Gets or Sets an error event handler.
        /// </summary>
        /// <remarks>
        /// If the application does not register an error handler, all
        /// error events reported by the SAX parser will be silently
        /// ignored; however, normal processing may not continue.  It is
        /// highly recommended that all SAX applications implement an
        /// error handler to avoid unexpected bugs.
        /// <para/>
        /// Applications may register a new or different handler in the
        /// middle of a parse, and the SAX parser must begin using the new
        /// handler immediately.
        /// </remarks>
        IErrorHandler ErrorHandler { get; set; }


        ////////////////////////////////////////////////////////////////////
        // Parsing.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parse an XML document.
        /// </summary>
        /// <remarks>
        /// The application can use this method to instruct the XML
        /// reader to begin parsing an XML document from any valid input
        /// source (a character stream, a byte stream, or a URI).
        /// <para/>
        /// Applications may not invoke this method while a parse is in
        /// progress (they should create a new XMLReader instead for each
        /// nested XML document).  Once a parse is complete, an
        /// application may reuse the same XMLReader object, possibly with a
        /// different input source.
        /// Configuration of the <see cref="IXMLReader"/> object (such as handler bindings and
        /// values established for feature flags and properties) is unchanged
        /// by completion of a parse, unless the definition of that aspect of
        /// the configuration explicitly specifies other behavior.
        /// (For example, feature flags or properties exposing
        /// characteristics of the document being parsed.)
        /// <para/>
        /// During the parse, the XMLReader will provide information
        /// about the XML document through the registered event
        /// handlers.
        /// <para/>
        /// This method is synchronous: it will not return until parsing
        /// has ended.  If a client application wants to terminate 
        /// parsing early, it should throw an exception.
        /// </remarks>
        /// <param name="input">The input source for the top-level of the
        /// XML document.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <exception cref="IOException">An IO exception from the parser,
        /// possibly from a byte stream or character stream
        /// supplied by the application.</exception>
        /// <seealso cref="InputSource"/>
        /// <seealso cref="Parse(string)"/>
        /// <seealso cref="EntityResolver"/>
        /// <seealso cref="DTDHandler"/>
        /// <seealso cref="ContentHandler"/>
        /// <seealso cref="ErrorHandler"/>
        void Parse(InputSource input);


        /// <summary>
        /// Parse an XML document from a system identifier (URI).
        /// </summary>
        /// <remarks>
        /// This method is a shortcut for the common case of reading a
        /// document from a system identifier.  It is the exact
        /// equivalent of the following:
        /// <code>
        /// Parse(new InputSource(systemId));
        /// </code>
        /// <para/>If the system identifier is a URL, it must be fully resolved
        /// by the application before it is passed to the parser.
        /// </remarks>
        /// <param name="systemId">The system identifier (URI).</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <exception cref="IOException">An IO exception from the parser,
        /// possibly from a byte stream or character stream
        /// supplied by the application.</exception>
        void Parse(string systemId);
    }
}
