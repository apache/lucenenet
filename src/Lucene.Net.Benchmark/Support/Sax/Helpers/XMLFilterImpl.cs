// XMLFilterImpl.java - base SAX2 filter implementation.
// http://www.saxproject.org
// Written by David Megginson
// NO WARRANTY!  This class is in the Public Domain.
// $Id: XMLFilterImpl.java,v 1.9 2004/04/26 17:34:35 dmegginson Exp $

using System;
using System.IO;

namespace Sax.Helpers
{
    /// <summary>
    /// Base class for deriving an XML filter.
    /// <para/>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with <strong>NO WARRANTY</strong>.</em>
    /// See <a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This class is designed to sit between an <see cref="IXMLReader" />
    /// and the client application's event handlers.  By default, it
    /// does nothing but pass requests up to the reader and events
    /// on to the handlers unmodified, but subclasses can override
    /// specific methods to modify the event stream or the configuration
    /// requests as they pass through.
    /// </summary>
    /// <seealso cref="IXMLFilter" />
    /// <seealso cref="IXMLReader" />
    /// <seealso cref="IEntityResolver" />
    /// <seealso cref="IDTDHandler" />
    /// <seealso cref="IContentHandler" />
    /// <seealso cref="IErrorHandler" />
    public class XMLFilter : IXMLFilter, IEntityResolver, IDTDHandler, IContentHandler, IErrorHandler
    {
        /// <summary>
        /// Construct an empty XML filter, with no parent.
        /// <para>
        /// This filter will have no parent: you must assign a parent
        /// before you start a parse or do any configuration with
        /// setFeature or setProperty, unless you use this as a pure event
        /// consumer rather than as an <see cref="IXMLReader" />.
        /// </para>
        /// </summary>
        /// <seealso cref="IXMLReader.SetFeature" />
        /// <seealso cref="IXMLReader.SetProperty" />
        /// <seealso cref="Parent" />
        public XMLFilter()
        {
        }

        /// <summary>
        /// Construct an XML filter with the specified parent.
        /// </summary>
        /// <param name="parent">The parent</param>
        /// <seealso cref="Parent" />
        public XMLFilter(IXMLReader parent)
        {
            this.parent = parent;
        }

        ////////////////////////////////////////////////////////////////////
        // Implementation of IXMLFilter.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the parent reader.
        /// </summary>
        /// <remarks>
        /// This is the <see cref="IXMLReader"/> from which 
        /// this filter will obtain its events and to which it will pass its 
        /// configuration requests.  The parent may itself be another filter.
        /// <para/>
        /// If there is no parent reader set, any attempt to parse
        /// or to set or get a feature or property will fail.
        /// </remarks>
        public IXMLReader Parent
        {
            get => parent;
            set => parent = value;
        }

        ////////////////////////////////////////////////////////////////////
        // Implementation of IXMLReader.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set the value of a feature.
        /// <para/>
        /// This will always fail if the parent is null.
        /// </summary>
        /// <param name="name">The feature name.</param>
        /// <param name="value">The requested feature value.</param>
        /// <exception cref="SAXNotRecognizedException">If the feature
        /// value can't be assigned or retrieved from the parent.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// parent recognizes the feature name but
        /// cannot set the requested value.</exception>
        public virtual void SetFeature(string name, bool value)
        {
            if (parent != null)
            {
                parent.SetFeature(name, value);
            }
            else
            {
                throw new SAXNotRecognizedException("Feature: " + name);
            }
        }

        /// <summary>
        /// Look up the value of a feature.
        /// <para/>
        /// This will always fail if the parent is null.
        /// </summary>
        /// <param name="name">The feature name.</param>
        /// <returns>The current value of the feature.</returns>
        /// <exception cref="SAXNotRecognizedException">If the feature
        /// value can't be assigned or retrieved from the parent.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// parent recognizes the feature name but
        /// cannot determine its value at this time.</exception>
        public virtual bool GetFeature(string name)
        {
            if (parent != null)
            {
                return parent.GetFeature(name);
            }
            throw new SAXNotRecognizedException("Feature: " + name);
        }

        /// <summary>
        /// Set the value of a property.
        /// <para/>
        /// This will always fail if the parent is null.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The requested property value.</param>
        /// <exception cref="SAXNotRecognizedException">If the property
        /// value can't be assigned or retrieved from the parent.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// parent recognizes the property name but
        /// cannot set the requested value.</exception>
        public virtual void SetProperty(string name, object value)
        {
            if (parent != null)
            {
                parent.SetProperty(name, value);
            }
            else
            {
                throw new SAXNotRecognizedException("Property: " + name);
            }
        }

        /// <summary>
        /// Look up the value of a property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The current value of the property.</returns>
        /// <exception cref="SAXNotRecognizedException">If the property
        /// value can't be assigned or retrieved from the parent.</exception>
        /// <exception cref="SAXNotSupportedException">When the
        /// parent recognizes the property name but
        /// cannot determine its value at this time.</exception>
        public virtual object GetProperty(string name)
        {
            if (parent != null)
            {
                return parent.GetProperty(name);
            }
            throw new SAXNotRecognizedException("Property: " + name);
        }

        /// <summary>
        /// Gets or Sets the entity resolver.
        /// </summary>
        public IEntityResolver EntityResolver
        {
            get => entityResolver;
            set => entityResolver = value;
        }

        /// <summary>
        /// Gets or Sets the DTD event handler.
        /// </summary>
        public IDTDHandler DTDHandler
        {
            get => dtdHandler;
            set => dtdHandler = value;
        }

        /// <summary>
        /// Gets or Sets the content event handler.
        /// </summary>
        public IContentHandler ContentHandler
        {
            get => contentHandler;
            set => contentHandler = value;
        }

        /// <summary>
        /// Gets or Sets the error event handler.
        /// </summary>
        public IErrorHandler ErrorHandler
        {
            get => errorHandler;
            set => errorHandler = value;
        }

        /// <summary>
        /// Parse an XML document.
        /// <para>
        /// The application can use this method to instruct the XML
        /// reader to begin parsing an XML document from any valid input
        /// source (a character stream, a byte stream, or a URI).
        /// </para>
        /// <para>
        /// Applications may not invoke this method while a parse is in
        /// progress (they should create a new XMLReader instead for each
        /// nested XML document).  Once a parse is complete, an
        /// application may reuse the same XMLReader object, possibly with a
        /// different input source.
        /// Configuration of the XMLReader object (such as handler bindings and
        /// values established for feature flags and properties) is unchanged
        /// by completion of a parse, unless the definition of that aspect of
        /// the configuration explicitly specifies other behavior.
        /// (For example, feature flags or properties exposing
        /// characteristics of the document being parsed.)
        /// </para>
        /// <para>
        /// During the parse, the XMLReader will provide information
        /// about the XML document through the registered event
        /// handlers.
        /// </para>
        /// <para>
        /// This method is synchronous: it will not return until parsing
        /// has ended.  If a client application wants to terminate
        /// parsing early, it should throw an exception.
        /// </para>
        /// </summary>
        /// <param name="input">
        /// The input source for the top-level of the
        /// XML document.
        /// </param>
        /// <exception cref="SAXException">
        /// Any SAX exception, possibly
        /// wrapping another exception.
        /// </exception>
        /// <exception cref="IOException">
        /// An IO exception from the parser,
        /// possibly from a byte stream or character stream
        /// supplied by the application.
        /// </exception>
        /// <seealso cref="InputSource" />
        /// <seealso cref="Parse(string)" />
        /// <seealso cref="IEntityResolver" />
        /// <seealso cref="IDTDHandler" />
        /// <seealso cref="IContentHandler" />
        /// <seealso cref="IErrorHandler" />
        public virtual void Parse(InputSource input)
        {
            SetupParse();
            parent.Parse(input);
        }

        /// <summary>
        /// Parse a document.
        /// </summary>
        /// <param name="systemId">The system identifier as a fully-qualified URI.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly
        /// wrapping another exception.</exception>
        /// <exception cref="IOException">An IO exception from the parser,
        /// possibly from a byte stream or character stream
        /// supplied by the application.</exception>
        public virtual void Parse(string systemId)
        {
            Parse(new InputSource(systemId));
        }


        ////////////////////////////////////////////////////////////////////
        // Implementation of IEntityResolver.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Filter an external entity resolution.
        /// </summary>
        /// <param name="publicId">The entity's public identifier, or null.</param>
        /// <param name="systemId">The entity's system identifier.</param>
        /// <returns>A new <see cref="InputSource"/> or null for the default.</returns>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        /// <exception cref="IOException">The client may throw an
        /// I/O-related exception while obtaining the
        /// new <see cref="InputSource"/>.</exception>
        public virtual InputSource ResolveEntity(string publicId, string systemId)
        {
            if (entityResolver != null)
            {
                return entityResolver.ResolveEntity(publicId, systemId);
            }
            return null;
        }

        ////////////////////////////////////////////////////////////////////
        // Implementation of IDTDHandler.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Filter a notation declaration event.
        /// </summary>
        /// <param name="name">The notation name.</param>
        /// <param name="publicId">The notation's public identifier, or null.</param>
        /// <param name="systemId">The notation's system identifier, or null.</param>
        /// <seealso cref="SAXException">The client may throw
        /// an exception during processing.</seealso>
        public virtual void NotationDecl(string name, string publicId, string systemId)
        {
            if (dtdHandler != null)
            {
                dtdHandler.NotationDecl(name, publicId, systemId);
            }
        }

        /// <summary>
        /// Filter an unparsed entity declaration event.
        /// </summary>
        /// <param name="name">The entity name.</param>
        /// <param name="publicId">The entity's public identifier, or null.</param>
        /// <param name="systemId">The entity's system identifier, or null.</param>
        /// <param name="notationName">The name of the associated notation.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void UnparsedEntityDecl(string name, string publicId, string systemId, string notationName)
        {
            if (dtdHandler != null)
            {
                dtdHandler.UnparsedEntityDecl(name, publicId, systemId, notationName);
            }
        }

        ////////////////////////////////////////////////////////////////////
        // Implementation of IContentHandler.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Filter a new document locator event.
        /// </summary>
        /// <param name="locator">The document locator.</param>
        public virtual void SetDocumentLocator(ILocator locator)
        {
            //this.locator = locator; // LUCENENET: Never read
            if (contentHandler != null)
            {
                contentHandler.SetDocumentLocator(locator);
            }
        }

        /// <summary>
        /// Filter a start document event.
        /// </summary>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void StartDocument()
        {
            if (contentHandler != null)
            {
                contentHandler.StartDocument();
            }
        }

        /// <summary>
        /// Filter an end document event.
        /// </summary>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void EndDocument()
        {
            if (contentHandler != null)
            {
                contentHandler.EndDocument();
            }
        }

        /// <summary>
        /// Filter a start Namespace prefix mapping event.
        /// </summary>
        /// <param name="prefix">The Namespace prefix.</param>
        /// <param name="uri">The Namespace URI.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void StartPrefixMapping(string prefix, string uri)
        {
            if (contentHandler != null)
            {
                contentHandler.StartPrefixMapping(prefix, uri);
            }
        }

        /// <summary>
        /// Filter an end Namespace prefix mapping event.
        /// </summary>
        /// <param name="prefix">The Namespace prefix.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void EndPrefixMapping(string prefix)
        {
            if (contentHandler != null)
            {
                contentHandler.EndPrefixMapping(prefix);
            }
        }

        /// <summary>
        /// Filter a start element event.
        /// </summary>
        /// <param name="uri">The element's Namespace URI, or the empty string.</param>
        /// <param name="localName">The element's local name, or the empty string.</param>
        /// <param name="qName">The element's qualified (prefixed) name, or the empty string.</param>
        /// <param name="atts">The element's attributes.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void StartElement(string uri, string localName, string qName, IAttributes atts)
        {
            if (contentHandler != null)
            {
                contentHandler.StartElement(uri, localName, qName, atts);
            }
        }

        /// <summary>
        /// Filter an end element event.
        /// </summary>
        /// <param name="uri">The element's Namespace URI, or the empty string.</param>
        /// <param name="localName">The element's local name, or the empty string.</param>
        /// <param name="qName">The element's qualified (prefixed) name, or the empty string.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void EndElement(string uri, string localName, string qName)
        {
            if (contentHandler != null)
            {
                contentHandler.EndElement(uri, localName, qName);
            }
        }

        /// <summary>
        /// Filter a character data event.
        /// </summary>
        /// <param name="ch">An array of characters.</param>
        /// <param name="start">The starting position in the array.</param>
        /// <param name="length">The number of characters to use from the array.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void Characters(char[] ch, int start, int length)
        {
            if (contentHandler != null)
            {
                contentHandler.Characters(ch, start, length);
            }
        }

        /// <summary>
        /// Filter an ignorable whitespace event.
        /// </summary>
        /// <param name="ch">An array of characters.</param>
        /// <param name="start">The starting position in the array.</param>
        /// <param name="length">The number of characters to use from the array.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void IgnorableWhitespace(char[] ch, int start, int length)
        {
            if (contentHandler != null)
            {
                contentHandler.IgnorableWhitespace(ch, start, length);
            }
        }

        /// <summary>
        /// Filter a processing instruction event.
        /// </summary>
        /// <param name="target">The processing instruction target.</param>
        /// <param name="data">The text following the target.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void ProcessingInstruction(string target, string data)
        {
            if (contentHandler != null)
            {
                contentHandler.ProcessingInstruction(target, data);
            }
        }

        /// <summary>
        /// Filter a skipped entity event.
        /// </summary>
        /// <param name="name">The name of the skipped entity.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void SkippedEntity(string name)
        {
            if (contentHandler != null)
            {
                contentHandler.SkippedEntity(name);
            }
        }

        ////////////////////////////////////////////////////////////////////
        // Implementation of IErrorHandler.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Filter a warning event.
        /// </summary>
        /// <param name="e">The warning as an exception.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void Warning(SAXParseException e)
        {
            if (errorHandler != null)
            {
                errorHandler.Warning(e);
            }
        }

        /// <summary>
        /// Filter an error event.
        /// </summary>
        /// <param name="e">The error as an exception.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void Error(SAXParseException e)
        {
            if (errorHandler != null)
            {
                errorHandler.Error(e);
            }
        }

        /// <summary>
        /// Filter a fatal error event.
        /// </summary>
        /// <param name="e">The error as an exception.</param>
        /// <exception cref="SAXException">The client may throw
        /// an exception during processing.</exception>
        public virtual void FatalError(SAXParseException e)
        {
            if (errorHandler != null)
            {
                errorHandler.FatalError(e);
            }
        }

        ////////////////////////////////////////////////////////////////////
        // Internal methods.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set up before a parse.
        /// <para/>
        /// Before every parse, check whether the parent is
        /// non-null, and re-register the filter for all of the
        /// events.
        /// </summary>
        private void SetupParse()
        {
            if (parent is null)
            {
                throw new InvalidOperationException("No parent for filter");
            }
            parent.EntityResolver = this;
            parent.DTDHandler = this;
            parent.ContentHandler = this;
            parent.ErrorHandler = this;
        }

        ////////////////////////////////////////////////////////////////////
        // Internal state.
        ////////////////////////////////////////////////////////////////////

        private IXMLReader parent = null;
        //private ILocator locator = null; // LUCENENET: Never read
        private IEntityResolver entityResolver = null;
        private IDTDHandler dtdHandler = null;
        private IContentHandler contentHandler = null;
        private IErrorHandler errorHandler = null;
    }
}
