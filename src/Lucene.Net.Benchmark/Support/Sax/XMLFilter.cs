// XMLFilter.java - filter SAX2 events.
// http://www.saxproject.org
// Written by David Megginson
// NO WARRANTY!  This class is in the Public Domain.
// $Id: XMLFilter.java,v 1.6 2002/01/30 21:13:48 dbrownell Exp $

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
    /// The XMLFilterImpl helper class provides a convenient base
    /// for creating SAX2 filters, by passing on all <see cref="IEntityResolver"/>, <see cref="IDTDHandler"/>,
    /// <see cref="IContentHandler"/> and <see cref="IErrorHandler"/>
    /// events automatically.
    /// </remarks>
    public interface IXMLFilter : IXMLReader
    {
        /// <summary>
        /// Gets or sets the parent reader. Returns the parent filter, or null if none has been set.
        /// </summary>
        /// <remarks>
        /// This method allows the application to link or query the parent
        /// reader (which may be another filter).  It is generally a
        /// bad idea to perform any operations on the parent reader
        /// directly: they should all pass through this filter.
        /// </remarks>
        IXMLReader Parent { get; set; }
    }
}
