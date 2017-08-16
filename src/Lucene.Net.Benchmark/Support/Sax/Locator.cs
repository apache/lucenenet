// SAX locator interface for document events.
// http://www.saxproject.org
// No warranty; no copyright -- use this as you will.
// $Id: Locator.java,v 1.8 2002/01/30 21:13:47 dbrownell Exp $

namespace Sax
{
    /// <summary>
    /// Interface for associating a SAX event with a document location.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with <strong>NO WARRANTY</strong>.</em>
    /// See <a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// If a SAX parser provides location information to the SAX
    /// application, it does so by implementing this interface and then
    /// passing an instance to the application using the content
    /// handler's <see cref="IContentHandler.SetDocumentLocator(ILocator)"/>
    /// method.The application can use the
    /// object to obtain the location of any other SAX event
    /// in the XML source document.
    /// <para/>
    /// Note that the results returned by the object will be valid only
    /// during the scope of each callback method: the application
    /// will receive unpredictable results if it attempts to use the
    /// locator at any other time, or after parsing completes.
    /// <para/>
    /// SAX parsers are not required to supply a locator, but they are
    /// very strongly encouraged to do so.  If the parser supplies a
    /// locator, it must do so before reporting any other document events.
    /// If no locator has been set by the time the application receives
    /// the <see cref="IContentHandler.StartDocument()"/>
    /// event, the application should assume that a locator is not
    /// available.
    /// </remarks>
    /// <since>SAX 1.0</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1 (sax2r2)</version>
    /// <seealso cref="IContentHandler.SetDocumentLocator(ILocator)"/>
    public interface ILocator
    {
        /// <summary>
        /// Gets the public identifier for the current document event.
        /// <para/>
        /// The return value is the public identifier of the document
        /// entity or of the external parsed entity in which the markup
        /// triggering the event appears.
        /// <para/>
        /// Returns a string containing the public identifier, or null if none is available.
        /// </summary>
        /// <seealso cref="SystemId"/>
        string PublicId { get; }

        /// <summary>
        /// Return the system identifier for the current document event.
        /// <para/>
        /// The return value is the system identifier of the document
        /// entity or of the external parsed entity in which the markup
        /// triggering the event appears.
        /// <para/>If the system identifier is a URL, the parser must resolve it
        /// fully before passing it to the application.  For example, a file
        /// name must always be provided as a <em>file:...</em> URL, and other
        /// kinds of relative URI are also resolved against their bases.
        /// <para/>
        /// Returns a string containing the system identifier, or null
        /// if none is available.
        /// </summary>
        /// <seealso cref="PublicId"/>
        string SystemId { get; }

        /// <summary>
        /// Return the line number where the current document event ends.
        /// Lines are delimited by line ends, which are defined in
        /// the XML specification.
        /// <para/>
        /// <strong>Warning:</strong> The return value from the method
        /// is intended only as an approximation for the sake of diagnostics;
        /// it is not intended to provide sufficient information
        /// to edit the character content of the original XML document.
        /// In some cases, these "line" numbers match what would be displayed
        /// as columns, and in others they may not match the source text
        /// due to internal entity expansion. 
        /// <para/>
        /// The return value is an approximation of the line number
        /// in the document entity or external parsed entity where the
        /// markup triggering the event appears.
        /// <para/>
        /// If possible, the SAX driver should provide the line position 
        /// of the first character after the text associated with the document 
        /// event.  The first line is line 1.
        /// <para/>
        /// Returns the line number, or -1 if none is available.
        /// </summary>
        /// <seealso cref="ColumnNumber"/>
        int LineNumber { get; }

        /// <summary>
        /// Return the column number where the current document event ends.
        /// This is one-based number of Java<code>char</code> values since
        /// the last line end.
        /// <para/>
        /// <strong>Warning:</strong> The return value from the method
        /// is intended only as an approximation for the sake of diagnostics;
        /// it is not intended to provide sufficient information
        /// to edit the character content of the original XML document.
        /// For example, when lines contain combining character sequences, wide
        /// characters, surrogate pairs, or bi-directional text, the value may
        /// not correspond to the column in a text editor's display. 
        /// <para/>
        /// The return value is an approximation of the column number
        /// in the document entity or external parsed entity where the
        /// markup triggering the event appears.
        /// <para/>
        /// If possible, the SAX driver should provide the line position 
        /// of the first character after the text associated with the document 
        /// event.  The first column in each line is column 1.
        /// <para/>
        /// Returns the column number, or -1 if none is available.
        /// </summary>
        /// <seealso cref="LineNumber"/>
        int ColumnNumber { get; }
    }
}
