// DefaultHandler2.java - extended DefaultHandler
// http://www.saxproject.org
// Public Domain: no warranty.
// $Id: DefaultHandler2.java,v 1.3 2002/01/12 19:04:19 dbrownell Exp $

using Sax.Helpers;

namespace Sax.Ext
{
    /// <summary>
    /// This class extends the SAX2 base handler class to support the
    /// SAX2 <see cref="ILexicalHandler"/>, <see cref="IDeclHandler"/>, and
    /// <see cref="IEntityResolver2"/> extensions.  Except for overriding the
    /// original SAX1 <see cref="DefaultHandler.ResolveEntity(string, string)"/>
    /// method the added handler methods just return.  Subclassers may
    /// override everything on a method-by-method basis.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with <strong>NO WARRANTY</strong>.</em>
    /// <para/>
    /// <em>Note:</em> this class might yet learn that the
    /// <see cref="IContentHandler.SetDocumentLocator(ILocator)"/> call might be passed a
    /// <see cref="Locator2"/> object, and that the
    /// <em>ContentHandler.startElement()</em> call might be passed a
    /// <see cref="Attributes2"/> object.
    /// </remarks>
    /// <since>2.0 (extensions 1.1 alpha)</since>
    /// <author>David Brownell</author>
    /// <version>TBS</version>
    public class DefaultHandler2 : DefaultHandler, ILexicalHandler, IDeclHandler, IEntityResolver2
    {
        /// <summary>Constructs a handler which ignores all parsing events.</summary>
        public DefaultHandler2() { }


        // SAX2 ext-1.0 LexicalHandler

        public virtual void StartCDATA()
        { }

        public virtual void EndCDATA()
        { }

        public virtual void StartDTD(string name, string publicId, string systemId)
        { }

        public virtual void EndDTD()
        { }

        public virtual void StartEntity(string name)
        { }

        public virtual void EndEntity(string name)
        { }

        public virtual void Comment(char[] ch, int start, int length)
        { }


        // SAX2 ext-1.0 DeclHandler

        public virtual void AttributeDecl(string eName, string aName,
            string type, string mode, string value)
        { }

        public virtual void ElementDecl(string name, string model)
        { }

        public virtual void ExternalEntityDecl(string name,
            string publicId, string systemId)
        { }

        public virtual void InternalEntityDecl(string name, string value)
        { }

        // SAX2 ext-1.1 EntityResolver2

        /// <summary>
        /// Tells the parser that if no external subset has been declared
        /// in the document text, none should be used.
        /// </summary>
        public virtual InputSource GetExternalSubset(string name, string baseURI)
        {
            return null;
        }

        /// <summary>
        /// Tells the parser to resolve the systemId against the baseURI
        /// and read the entity text from that resulting absolute URI.
        /// Note that because the older <see cref="DefaultHandler.ResolveEntity(string, string)"/>,
        /// method is overridden to call this one, this method may sometimes 
        /// be invoked with null <paramref name="name"/> and <paramref name="baseURI"/>, and
        /// with the <paramref name="systemId"/> already absolutized.
        /// </summary>
        public virtual InputSource ResolveEntity(string name, string publicId,
            string baseURI, string systemId)
        { return null; }

        // SAX1 EntityResolver

        /// <summary>
        /// Invokes <see cref="IEntityResolver2.ResolveEntity(string, string, string, string)"/>
        /// with null entity name and base URI.
        /// You only need to override that method to use this class.
        /// </summary>
        public override InputSource ResolveEntity(string publicId, string systemId)
        {
            return ResolveEntity(null, publicId, null, systemId);
        }
    }
}
