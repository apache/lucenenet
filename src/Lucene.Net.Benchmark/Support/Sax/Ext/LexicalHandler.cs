// LexicalHandler.java - optional handler for lexical parse events.
// http://www.saxproject.org
// Public Domain: no warranty.
// $Id: LexicalHandler.java,v 1.5 2002/01/30 21:00:44 dbrownell Exp $

namespace Sax.Ext
{
    /// <summary>
    /// SAX2 extension handler for lexical events.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This is an optional extension handler for SAX2 to provide
    /// lexical information about an XML document, such as comments
    /// and CDATA section boundaries.
    /// XML readers are not required to recognize this handler, and it
    /// is not part of core-only SAX2 distributions.
    /// <para/>
    /// The events in the lexical handler apply to the entire document,
    /// not just to the document element, and all lexical handler events
    /// must appear between the content handler's StartDocument and
    /// EndDocument events.
    /// <para/>
    /// To set the LexicalHandler for an XML reader, use the
    /// <see cref="IXMLReader.SetProperty(string, object)"/> method
    /// with the property name
    /// <a href="http://xml.org/sax/properties/lexical-handler">http://xml.org/sax/properties/lexical-handler</a>
    /// and an object implementing this interface (or null) as the value.
    /// If the reader does not report lexical events, it will throw a
    /// <see cref="SAXNotRecognizedException"/>
    /// when you attempt to register the handler.
    /// </remarks>
    /// <since>SAX 2.0 (extensions 1.0)</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1 (sax2r2)</version>
    public interface ILexicalHandler
    {
        /// <summary>
        /// Report the start of DTD declarations, if any.
        /// </summary>
        /// <remarks>
        /// This method is intended to report the beginning of the
        /// DOCTYPE declaration; if the document has no DOCTYPE declaration,
        /// this method will not be invoked.
        /// <para/>
        /// All declarations reported through 
        /// <see cref="IDTDHandler"/> or
        /// <see cref="Ext.IDeclHandler"/> events must appear
        /// between the startDTD and <see cref="EndDTD()"/> events.
        /// Declarations are assumed to belong to the internal DTD subset
        /// unless they appear between <see cref="StartEntity(string)"/>
        /// and <see cref="EndEntity(string)"/> events.  Comments and
        /// processing instructions from the DTD should also be reported
        /// between the <see cref="StartDTD(string, string, string)"/> and <see cref="EndDTD()"/> events, in their original
        /// order of(logical) occurrence; they are not required to
        /// appear in their correct locations relative to <see cref="IDTDHandler"/>
        /// or <see cref="IDeclHandler"/> events, however.
        /// <para/>
        /// Note that the start / endDTD events will appear within
        /// the start / endDocument events from <see cref="IContentHandler"/> and
        /// before the first <see cref="IContentHandler.StartElement(string, string, string, IAttributes)"/>
        /// event.
        /// </remarks>
        /// <param name="name">The document type name.</param>
        /// <param name="publicId">The declared public identifier for the
        /// external DTD subset, or null if none was declared.</param>
        /// <param name="systemId">The declared system identifier for the
        /// external DTD subset, or null if none was declared.
        /// (Note that this is not resolved against the document
        /// base URI.)</param>
        /// <exception cref="SAXException">The application may raise an exception.</exception>
        /// <see cref="EndDTD()"/>
        /// <see cref="StartEntity(string)"/>
        void StartDTD(string name, string publicId,
                   string systemId);

        /// <summary>
        /// Report the end of DTD declarations.
        /// <para/>
        /// This method is intended to report the end of the
        /// DOCTYPE declaration; if the document has no DOCTYPE declaration,
        /// this method will not be invoked.
        /// </summary>
        /// <exception cref="SAXException">The application may raise an exception.</exception>
        /// <seealso cref="StartDTD(string, string, string)"/>
        void EndDTD();

        /// <summary>
        /// Report the beginning of some internal and external XML entities.
        /// </summary>
        /// <remarks>
        /// The reporting of parameter entities (including
        /// the external DTD subset) is optional, and SAX2 drivers that
        /// report LexicalHandler events may not implement it; you can use the
        /// <a href="http://xml.org/sax/features/lexical-handler/parameter-entities">http://xml.org/sax/features/lexical-handler/parameter-entities</a>
        /// feature to query or control the reporting of parameter entities.
        /// <para/>
        /// General entities are reported with their regular names,
        /// parameter entities have '%' prepended to their names, and 
        /// the external DTD subset has the pseudo-entity name "[dtd]".
        /// <para/>
        /// When a SAX2 driver is providing these events, all other 
        /// events must be properly nested within start/end entity 
        /// events. There is no additional requirement that events from 
        /// <see cref="IDeclHandler"/> or
        /// <see cref="IDTDHandler"/> be properly ordered.
        /// <para/>
        /// Note that skipped entities will be reported through the
        /// <see cref="IContentHandler.SkippedEntity(string)"/>
        /// event, which is part of the ContentHandler interface.
        /// <para/>Because of the streaming event model that SAX uses, some
        /// entity boundaries cannot be reported under any
        /// circumstances:
        /// <list type="bullet">
        ///     <item><description>general entities within attribute values</description></item>
        ///     <item><description>parameter entities within declarations</description></item>
        /// </list>
        /// <para/>These will be silently expanded, with no indication of where
        /// the original entity boundaries were.
        /// <para/>Note also that the boundaries of character references (which
        /// are not really entities anyway) are not reported.
        /// <para/>All start/endEntity events must be properly nested.
        /// </remarks>
        /// <param name="name">The name of the entity.  If it is a parameter
        /// entity, the name will begin with '%', and if it is the
        /// external DTD subset, it will be "[dtd]".</param>
        /// <exception cref="SAXException">The application may raise an exception.</exception>
        /// <seealso cref="EndEntity(string)"/>
        /// <seealso cref="IDeclHandler.InternalEntityDecl(string, string)"/>
        /// <seealso cref="IDeclHandler.ExternalEntityDecl(string, string, string)"/>
        void StartEntity(string name);

        /// <summary>
        /// Report the end of an entity.
        /// </summary>
        /// <param name="name">The name of the entity that is ending.</param>
        /// <exception cref="SAXException">The application may raise an exception.</exception>
        /// <seealso cref="StartEntity(string)"/>
        void EndEntity(string name);

        /// <summary>
        /// Report the start of a CDATA section.
        /// </summary>
        /// <remarks>
        /// The contents of the CDATA section will be reported through
        /// the regular <see cref="IContentHandler.Characters(char[], int, int)"/>
        /// event; this event is intended only to report
        /// the boundary.
        /// </remarks>
        /// <exception cref="SAXException">The application may raise an exception.</exception>
        /// <seealso cref="EndEntity(string)"/>
        void StartCDATA();

        /// <summary>
        /// Report the end of a CDATA section.
        /// </summary>
        /// <exception cref="SAXException">The application may raise an exception.</exception>
        /// <seealso cref="StartCDATA()"/>
        void EndCDATA();

        /// <summary>
        /// Report an XML comment anywhere in the document.
        /// <para/>
        /// This callback will be used for comments inside or outside the
        /// document element, including comments in the external DTD
        /// subset(if read).  Comments in the DTD must be properly
        /// nested inside start/endDTD and start/endEntity events(if
        /// used).
        /// </summary>
        /// <param name="ch">An array holding the characters in the comment.</param>
        /// <param name="start">The starting position in the array.</param>
        /// <param name="length">The number of characters to use from the array.</param>
        /// <exception cref="SAXException">The application may raise an exception.</exception>
        void Comment(char[] ch, int start, int length);
    }
}
