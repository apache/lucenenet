// Locator2.java - extended Locator
// http://www.saxproject.org
// Public Domain: no warranty.
// $Id: Locator2.java,v 1.5 2004/03/17 14:30:10 dmegginson Exp $

using System.Text;

namespace Sax.Ext
{
    /// <summary>
    /// SAX2 extension to augment the entity information provided 
    /// though a <see cref="ILocator"/>.
    /// </summary>
    /// <remarks>
    /// If an implementation supports this extension, the Locator
    /// provided in <see cref="IContentHandler.SetDocumentLocator(ILocator)"/>
    /// will implement this interface, and the
    /// <a href="http://xml.org/sax/features/use-locator2">http://xml.org/sax/features/use-locator2</a> feature
    /// flag will have the value <em>true</em>.
    /// <para/>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// <para/> 
    /// XMLReader implementations are not required to support this
    /// information, and it is not part of core-only SAX2 distributions.
    /// </remarks>
    /// <since>SAX 2.0 (extensions 1.1 alpha)</since>
    /// <author>David Brownell</author>
    /// <version>TBS</version>
    public interface ILocator2 : ILocator
    {
        /// <summary>
        /// Returns the version of XML used for the entity.  This will
        /// normally be the identifier from the current entity's
        /// <em>&lt;?xml version='...' ...?&gt;</em> declaration,
        /// or be defaulted by the parser.
        /// </summary>
        string XMLVersion { get; }

        /// <summary>
        /// Returns the name of the character encoding for the entity.
        /// If the encoding was declared externally(for example, in a MIME
        /// Content-Type header), that will be the name returned.Else if there
        /// was an<em>&lt;?xml ...encoding='...'?&gt;</em> declaration at
        /// the start of the document, that encoding name will be returned.
        /// Otherwise the encoding will been inferred (normally to be UTF-8, or
        /// some UTF-16 variant), and that inferred name will be returned.
        /// <para/>
        /// When an <see cref="InputSource"/> is used
        /// to provide an entity's character stream, this method returns the
        /// encoding provided in that input stream.
        /// <para/> 
        /// Note that some recent W3C specifications require that text
        /// in some encodings be normalized, using Unicode Normalization
        /// Form C, before processing.Such normalization must be performed
        /// by applications, and would normally be triggered based on the
        /// value returned by this method.
        /// <para/> 
        /// Encoding names may be those used by the underlying JVM,
        /// and comparisons should be case-insensitive.
        /// </summary>
        Encoding Encoding { get; }
    }
}
