// SAX default implementation for Locator.
// http://www.saxproject.org
// No warranty; no copyright -- use this as you will.
// $Id: LocatorImpl.java,v 1.6 2002/01/30 20:52:27 dbrownell Exp $

namespace Sax.Helpers
{
    /// <summary>
    /// Provide an optional convenience implementation of <see cref="ILocator"/>.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This class is available mainly for application writers, who
    /// can use it to make a persistent snapshot of a locator at any
    /// point during a document parse:
    /// <code>
    /// ILocator locator;
    /// ILocator startloc;
    /// 
    /// public void SetLocator(ILocator locator)
    /// {
    ///    // note the locator
    ///    this.locator = locator;
    /// }
    /// 
    /// public void StartDocument()
    /// {
    ///    // save the location of the start of the document
    ///    // for future use.
    ///    ILocator startloc = new Locator(locator);
    /// }
    /// </code>
    /// <para/>
    /// Normally, parser writers will not use this class, since it
    /// is more efficient to provide location information only when
    /// requested, rather than constantly updating a <see cref="ILocator"/> object.
    /// </remarks>
    public class Locator : ILocator
    {
        /// <summary>
        /// Zero-argument constructor.
        /// <para/>This will not normally be useful, since the main purpose
        /// of this class is to make a snapshot of an existing <see cref="ILocator"/>.
        /// </summary>
        public Locator()
        {
        }

        /// <summary>
        /// Copy constructor.
        /// <para/>
        /// Create a persistent copy of the current state of a locator.
        /// When the original locator changes, this copy will still keep
        /// the original values (and it can be used outside the scope of
        /// DocumentHandler methods).
        /// </summary>
        /// <param name="locator">The locator to copy.</param>
        public Locator(ILocator locator)
        {
            publicId = locator.PublicId;
            systemId = locator.SystemId;
            lineNumber = locator.LineNumber;
            columnNumber = locator.ColumnNumber;
        }

        ////////////////////////////////////////////////////////////////////
        // Implementation of org.xml.sax.Locator
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Gets the public identifier as a string, or null if none
        /// is available.
        /// </summary>
        /// <seealso cref="ILocator.PublicId"/>
        public string PublicId
        {
            get => publicId;
            set => publicId = value;
        }


        /// <summary>
        /// Gets the system identifier as a string, or null if none
        /// is available.
        /// </summary>
        /// <seealso cref="ILocator.SystemId"/>
        public string SystemId
        {
            get => systemId;
            set => systemId = value;
        }


        /// <summary>
        /// Gets the saved line number (1-based).
        /// Returns the line number as an integer, or -1 if none is available.
        /// </summary>
        /// <seealso cref="ILocator.LineNumber"/>
        public int LineNumber
        {
            get => lineNumber;
            set => lineNumber = value;
        }


        /// <summary>
        /// Gets the saved column number (1-based).
        /// Returns the column number as an integer, or -1 if none is available.
        /// </summary>
        /// <seealso cref="ILocator.ColumnNumber"/>
        public int ColumnNumber
        {
            get => columnNumber;
            set => columnNumber = value;
        }

        ////////////////////////////////////////////////////////////////////
        // Internal state.
        ////////////////////////////////////////////////////////////////////

        private string publicId;
        private string systemId;
        private int lineNumber;
        private int columnNumber;
    }
}
