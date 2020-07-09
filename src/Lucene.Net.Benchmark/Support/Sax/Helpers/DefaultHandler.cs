// DefaultHandler.java - default implementation of the core handlers.
// http://www.saxproject.org
// Written by David Megginson
// NO WARRANTY!  This class is in the public domain.
// $Id: DefaultHandler.java,v 1.9 2004/04/26 17:34:35 dmegginson Exp $

using System.IO;

namespace Sax.Helpers
{
    /// <summary>
    /// Default base class for SAX2 event handlers.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This class is available as a convenience base class for SAX2
    /// applications: it provides default implementations for all of the
    /// callbacks in the four core SAX2 handler classes:
    /// <list type="number">
    ///     <item><description><see cref="IEntityResolver"/></description></item>
    ///     <item><description><see cref="IDTDHandler"/></description></item>
    ///     <item><description><see cref="IContentHandler"/></description></item>
    ///     <item><description><see cref="IErrorHandler"/></description></item>
    /// </list>
    /// <para/>
    /// Application writers can extend this class when they need to
    /// implement only part of an interface; parser writers can
    /// instantiate this class to provide default handlers when the
    /// application has not supplied its own.
    /// <para/>
    /// This class replaces the deprecated SAX1
    /// Sax.HandlerBase class.
    /// </remarks>
    /// <since>SAX 2.0</since>
    /// <author>David Megginson,</author>
    /// <version>2.0.1 (sax2r2)</version>
    /// <seealso cref="IEntityResolver"/>
    /// <seealso cref="IDTDHandler"/>
    /// <seealso cref="IContentHandler"/>
    /// <seealso cref="IErrorHandler"/>
    public class DefaultHandler : IEntityResolver, IDTDHandler, IContentHandler, IErrorHandler
    {
        ////////////////////////////////////////////////////////////////////
        // Default implementation of the EntityResolver interface.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolve an external entity.
        /// <para/>
        /// Always return null, so that the parser will use the system
        /// identifier provided in the XML document.  This method implements
        /// the SAX default behaviour: application writers can override it
        /// in a subclass to do special translations such as catalog lookups
        /// or URI redirection.
        /// </summary>
        /// <param name="publicId">The public identifer, or null if none is available.</param>
        /// <param name="systemId">The system identifier provided in the XML document.</param>
        /// <remarks>The new input source, or null to require the default behaviour.</remarks>
        /// <exception cref="IOException">If there is an error setting
        /// up the new input source.</exception>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IEntityResolver.ResolveEntity(string, string)"/>
        public virtual InputSource ResolveEntity(string publicId, string systemId)
        {
            return null;
        }

        ////////////////////////////////////////////////////////////////////
        // Default implementation of DTDHandler interface.
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Receive notification of a notation declaration.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass if they wish to keep track of the notations
        /// declared in a document.
        /// </summary>
        /// <param name="name">The notation name.</param>
        /// <param name="publicId">The notation public identifier, or null if not
        /// available.</param>
        /// <param name="systemId">The notation system identifier.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IDTDHandler.NotationDecl(string, string, string)"/>
        public virtual void NotationDecl(string name, string publicId, string systemId)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of an unparsed entity declaration.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to keep track of the unparsed entities
        /// declared in a document.
        /// </summary>
        /// <param name="name">The entity name.</param>
        /// <param name="publicId">The entity public identifier, or null if not available.</param>
        /// <param name="systemId">The entity system identifier.</param>
        /// <param name="notationName">The name of the associated notation.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IDTDHandler.UnparsedEntityDecl(string, string, string, string)"/>
        public virtual void UnparsedEntityDecl(string name, string publicId,
                        string systemId, string notationName)
        {
            // no op
        }

        ////////////////////////////////////////////////////////////////////
        // Default implementation of ContentHandler interface.
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Receive a Locator object for document events.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass if they wish to store the locator for use
        /// with other document events.
        /// </summary>
        /// <param name="locator">A locator for all SAX document events.</param>
        /// <seealso cref="IContentHandler.SetDocumentLocator(ILocator)"/>
        /// <seealso cref="ILocator"/>
        public virtual void SetDocumentLocator(ILocator locator)
        {
            // no op
        }

        /// <summary>
        /// Receive notification of the beginning of the document.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions at the beginning
        /// of a document (such as allocating the root node of a tree or
        /// creating an output file).
        /// </summary>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.StartDocument()"/>
        public virtual void StartDocument()
        {
            // no op
        }


        /// <summary>
        /// Receive notification of the end of the document.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions at the end
        /// of a document (such as finalising a tree or closing an output
        /// file).
        /// </summary>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.EndDocument()"/>
        public virtual void EndDocument()
        {
            // no op
        }


        /// <summary>
        /// Receive notification of the start of a Namespace mapping.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions at the start of
        /// each Namespace prefix scope (such as storing the prefix mapping).
        /// </summary>
        /// <param name="prefix">The Namespace prefix being declared.</param>
        /// <param name="uri">The Namespace URI mapped to the prefix.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.StartPrefixMapping(string, string)"/>
        public virtual void StartPrefixMapping(string prefix, string uri)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of the end of a Namespace mapping.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions at the end of
        /// each prefix mapping.
        /// </summary>
        /// <param name="prefix">The Namespace prefix being declared.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.EndPrefixMapping(string)"/>
        public virtual void EndPrefixMapping(string prefix)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of the start of an element.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions at the start of
        /// each element (such as allocating a new tree node or writing
        /// output to a file).
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty string if the
        /// element has no Namespace URI or if Namespace
        /// processing is not being performed.</param>
        /// <param name="localName">The local name (without prefix), or the
        /// empty string if Namespace processing is not being
        /// performed.</param>
        /// <param name="qName">The qualified name (with prefix), or the
        /// empty string if qualified names are not available.</param>
        /// <param name="attributes">The attributes attached to the element.  If
        /// there are no attributes, it shall be an empty
        /// <see cref="Attributes"/> object.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.StartElement(string, string, string, IAttributes)"/>
        public virtual void StartElement(string uri, string localName,
                      string qName, IAttributes attributes)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of the end of an element.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions at the end of
        /// each element (such as finalising a tree node or writing
        /// output to a file).
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty string if the
        /// element has no Namespace URI or if Namespace
        /// processing is not being performed.</param>
        /// <param name="localName">The local name (without prefix), or the
        /// empty string if Namespace processing is not being
        /// performed.</param>
        /// <param name="qName">The qualified name (with prefix), or the
        /// empty string if qualified names are not available.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.EndElement(string, string, string)"/>
        public virtual void EndElement(string uri, string localName, string qName)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of character data inside an element.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method to take specific actions for each chunk of character data
        /// (such as adding the data to a node or buffer, or printing it to
        /// a file).
        /// </summary>
        /// <param name="ch">The characters.</param>
        /// <param name="start">The start position in the character array.</param>
        /// <param name="length">The number of characters to use from the character array.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.Characters(char[], int, int)"/>
        public virtual void Characters(char[] ch, int start, int length)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of ignorable whitespace in element content.
        /// <para/>
        /// By default, do nothing.  Application writers may override this
        /// method to take specific actions for each chunk of ignorable
        /// whitespace (such as adding data to a node or buffer, or printing
        /// it to a file).
        /// </summary>
        /// <param name="ch">The whitespace characters.</param>
        /// <param name="start">The start position in the character array.</param>
        /// <param name="length">The number of characters to use from the character array.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.IgnorableWhitespace(char[], int, int)"/>
        public virtual void IgnorableWhitespace(char[] ch, int start, int length)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of a processing instruction.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions for each
        /// processing instruction, such as setting status variables or
        /// invoking other methods.
        /// </summary>
        /// <param name="target">The processing instruction target.</param>
        /// <param name="data">The processing instruction data, or null if
        /// none is supplied.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.ProcessingInstruction(string, string)"/>
        public virtual void ProcessingInstruction(string target, string data)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of a skipped entity.
        /// <para/>By default, do nothing.  Application writers may override this
        /// method in a subclass to take specific actions for each
        /// processing instruction, such as setting status variables or
        /// invoking other methods.
        /// </summary>
        /// <param name="name">The name of the skipped entity.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IContentHandler.ProcessingInstruction(string, string)"/>
        public virtual void SkippedEntity(string name)
        {
            // no op
        }



        ////////////////////////////////////////////////////////////////////
        // Default implementation of the ErrorHandler interface.
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Receive notification of a parser warning.
        /// <para/>
        /// The default implementation does nothing.  Application writers
        /// may override this method in a subclass to take specific actions
        /// for each warning, such as inserting the message in a log file or
        /// printing it to the console.
        /// </summary>
        /// <param name="e">The warning information encoded as an exception.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IErrorHandler.Warning(SAXParseException)"/>
        /// <seealso cref="SAXParseException"/>
        public virtual void Warning(SAXParseException e)
        {
            // no op
        }


        /// <summary>
        /// Receive notification of a recoverable parser error.
        /// <para/>The default implementation does nothing.  Application writers
        /// may override this method in a subclass to take specific actions
        /// for each error, such as inserting the message in a log file or
        /// printing it to the console.
        /// </summary>
        /// <param name="e">The warning information encoded as an exception.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IErrorHandler.Warning(SAXParseException)"/>
        /// <seealso cref="SAXParseException"/>
        public virtual void Error(SAXParseException e)
        {
            // no op
        }


        /// <summary>
        /// Report a fatal XML parsing error.
        /// <para/>
        /// The default implementation throws a <see cref="SAXParseException"/>.
        /// Application writers may override this method in a subclass if
        /// they need to take specific actions for each fatal error (such as
        /// collecting all of the errors into a single report): in any case,
        /// the application must stop all regular processing when this
        /// method is invoked, since the document is no longer reliable, and
        /// the parser may no longer report parsing events.
        /// </summary>
        /// <param name="e">The error information encoded as an exception.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <seealso cref="IErrorHandler.FatalError(SAXParseException)"/>
        /// <seealso cref="SAXParseException"/>
        public virtual void FatalError(SAXParseException e)
        {
            throw e;
        }
    }
}
