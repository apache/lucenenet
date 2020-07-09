// SAX error handler.
// http://www.saxproject.org
// No warranty; no copyright -- use this as you will.
// $Id: ErrorHandler.java,v 1.10 2004/03/08 13:01:00 dmegginson Exp $

using System.IO;

namespace Sax
{
    /// <summary>
    /// Basic interface for SAX error handlers.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// If a SAX application needs to implement customized error
    /// handling, it must implement this interface and then register an
    /// instance with the XML reader using the
    /// <see cref="IXMLReader.ErrorHandler"/>
    /// property. The parser will then report all errors and warnings
    /// through this interface.
    /// <para/>
    /// <strong>WARNING:</strong> If an application does <em>not</em>
    /// register an ErrorHandler, XML parsing errors will go unreported,
    /// except that<em> SAXParseException</em>s will be thrown for fatal errors.
    /// In order to detect validity errors, an ErrorHandler that does something
    /// with <see cref="Error(SAXParseException)"/> calls must be registered.
    /// <para/>
    /// For XML processing errors, a SAX driver must use this interface 
    /// in preference to throwing an exception: it is up to the application
    /// to decide whether to throw an exception for different types of
    /// errors and warnings.Note, however, that there is no requirement that
    /// the parser continue to report additional errors after a call to 
    /// <see cref="FatalError(SAXParseException)"/>.  In other words, a SAX driver class 
    /// may throw an exception after reporting any fatalError.
    /// Also parsers may throw appropriate exceptions for non - XML errors.
    /// For example, <see cref="IXMLReader.Parse(InputSource)"/> would throw
    /// an <see cref="IOException"/> for errors accessing entities or the document.
    /// </remarks>
    /// <since>SAX 1.0</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1+ (sax2r3pre1)</version>
    /// <seealso cref="IXMLReader.ErrorHandler"/>
    /// <seealso cref="SAXParseException"/>
    public interface IErrorHandler
    {
        /// <summary>
        /// Receive notification of a warning.
        /// </summary>
        /// <remarks>
        /// SAX parsers will use this method to report conditions that
        /// are not errors or fatal errors as defined by the XML
        /// recommendation.The default behaviour is to take no
        /// action.
        /// <para/>
        /// The SAX parser must continue to provide normal parsing events
        /// after invoking this method: it should still be possible for the
        /// application to process the document through to the end.
        /// <para/>
        /// Filters may use this method to report other, non-XML warnings
        /// as well.
        /// </remarks>
        /// <param name="exception">The warning information encapsulated in a SAX parse exception.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly wrapping another exception.</exception>
        /// <seealso cref="SAXParseException"/>
        void Warning(SAXParseException exception);

        /// <summary>
        /// Receive notification of a recoverable error.
        /// </summary>
        /// <remarks>
        /// This corresponds to the definition of "error" in section 1.2
        /// of the W3C XML 1.0 Recommendation.For example, a validating
        /// parser would use this callback to report the violation of a
        /// validity constraint.The default behaviour is to take no
        /// action.
        /// <para/>
        /// The SAX parser must continue to provide normal parsing
        /// events after invoking this method: it should still be possible
        /// for the application to process the document through to the end.
        /// If the application cannot do so, then the parser should report
        /// a fatal error even if the XML recommendation does not require
        /// it to do so.
        /// <para/>
        /// Filters may use this method to report other, non-XML errors
        /// as well.
        /// </remarks>
        /// <param name="exception">The error information encapsulated in a SAX parse exception.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly wrapping another exception.</exception>
        /// <seealso cref="SAXParseException"/>
        void Error(SAXParseException exception);

        /// <summary>
        /// Receive notification of a non-recoverable error.
        /// </summary>
        /// <remarks>
        /// <strong>There is an apparent contradiction between the
        /// documentation for this method and the documentation for
        /// <see cref="IContentHandler.EndDocument()"/>.  Until this ambiguity
        /// is resolved in a future major release, clients should make no
        /// assumptions about whether EndDocument() will or will not be
        /// invoked when the parser has reported a FatalError() or thrown
        /// an exception.</strong>
        /// <para/>
        /// This corresponds to the definition of "fatal error" in
        /// section 1.2 of the W3C XML 1.0 Recommendation.For example, a
        /// parser would use this callback to report the violation of a
        /// well-formedness constraint.
        /// <para/>
        /// The application must assume that the document is unusable
        /// after the parser has invoked this method, and should continue
        /// (if at all) only for the sake of collecting additional error
        /// messages: in fact, SAX parsers are free to stop reporting any
        /// other events once this method has been invoked.
        /// </remarks>
        /// <param name="exception">The error information encapsulated in a SAX parse exception.</param>
        /// <exception cref="SAXException">Any SAX exception, possibly wrapping another exception.</exception>
        /// <seealso cref="SAXParseException"/>
        void FatalError(SAXParseException exception);
    }
}
