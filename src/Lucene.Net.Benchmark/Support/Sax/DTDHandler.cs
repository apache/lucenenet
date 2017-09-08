// SAX DTD handler.
// http://www.saxproject.org
// No warranty; no copyright -- use this as you will.
// $Id: DTDHandler.java,v 1.8 2002/01/30 21:13:43 dbrownell Exp $


namespace Sax
{
    /// <summary>
    /// Receive notification of basic DTD-related events.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with <strong>NO WARRANTY</strong>.</em>
    /// See <a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// If a SAX application needs information about notations and
    /// unparsed entities, then the application implements this
    /// interface and registers an instance with the SAX parser using 
    /// the parser's setDTDHandler method.  The parser uses the 
    /// instance to report notation and unparsed entity declarations to
    /// the application.
    /// <para/>
    /// Note that this interface includes only those DTD events that
    /// the XML recommendation<em>requires</em> processors to report:
    /// notation and unparsed entity declarations.
    /// <para/>
    /// The SAX parser may report these events in any order, regardless
    /// of the order in which the notations and unparsed entities were
    /// declared; however, all DTD events must be reported after the
    /// document handler's startDocument event, and before the first
    /// startElement event.
    /// (If the <see cref="Ext.ILexicalHandler"/> is
    /// used, these events must also be reported before the endDTD event.)
    /// <para/>
    /// It is up to the application to store the information for 
    /// future use(perhaps in a hash table or object tree).
    /// If the application encounters attributes of type "NOTATION",
    /// "ENTITY", or "ENTITIES", it can use the information that it
    /// obtained through this interface to find the entity and/or
    /// notation corresponding with the attribute value.
    /// </remarks>
    /// <seealso cref="IXMLReader.DTDHandler"/>
    public interface IDTDHandler
    {
        /// <summary>
        /// Receive notification of a notation declaration event.
        /// </summary>
        /// <remarks>
        /// It is up to the application to record the notation for later
        /// reference, if necessary;
        /// notations may appear as attribute values and in unparsed entity
        /// declarations, and are sometime used with processing instruction
        /// target names.
        /// <para/>
        /// At least one of publicId and systemId must be non-null.
        /// If a system identifier is present, and it is a URL, the SAX
        /// parser must resolve it fully before passing it to the
        /// application through this event.
        /// <para/>
        /// There is no guarantee that the notation declaration will be
        /// reported before any unparsed entities that use it.
        /// </remarks>
        /// <param name="name">The notation name.</param>
        /// <param name="publicId">The notation's public identifier, or <c>null</c> if none was given.</param>
        /// <param name="systemId">The notation's system identifier, or <c>null</c> if none was given.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly wrapping another exception.</exception>
        /// <seealso cref="UnparsedEntityDecl(string, string, string, string)"/>
        /// <seealso cref="IAttributes"/>
        void NotationDecl(string name,
                       string publicId,
                       string systemId);

        /// <summary>
        /// Receive notification of an unparsed entity declaration event.
        /// </summary>
        /// <remarks>
        /// Note that the notation name corresponds to a notation
        /// reported by the <see cref="NotationDecl(string, string, string)"/> event.  
        /// It is up to the application to record the entity for later
        /// reference, if necessary;
        /// unparsed entities may appear as attribute values.
        /// <para/>
        /// If the system identifier is a URL, the parser must resolve it
        /// fully before passing it to the application.
        /// </remarks>
        /// <exception cref="SAXException">Any SAX exception, possibly wrapping another exception.</exception>
        /// <param name="name">The unparsed entity's name.</param>
        /// <param name="publicId">The entity's public identifier, or null if none was given.</param>
        /// <param name="systemId">The entity's system identifier.</param>
        /// <param name="notationName">The name of the associated notation.</param>
        /// <seealso cref="NotationDecl(string, string, string)"/>
        /// <seealso cref="IAttributes"/>
        void UnparsedEntityDecl(string name,
                         string publicId,
                         string systemId,
                         string notationName);
    }
}
