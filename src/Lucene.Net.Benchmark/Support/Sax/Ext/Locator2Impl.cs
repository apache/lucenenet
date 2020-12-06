// Locator2Impl.java - extended LocatorImpl
// http://www.saxproject.org
// Public Domain: no warranty.
// $Id: Locator2Impl.java,v 1.3 2004/04/26 17:34:35 dmegginson Exp $

using Sax.Helpers;
using System.Text;

namespace Sax.Ext
{
    /// <summary>
    /// SAX2 extension helper for holding additional Entity information,
    /// implementing the <see cref="Locator2"/> interface.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with <strong>NO WARRANTY</strong>.</em>
    /// <para/>
    /// This is not part of core-only SAX2 distributions.
    /// </remarks>
    /// <since>SAX 2.0.2</since>
    /// <author>David Brownell</author>
    /// <version>TBS</version>
    public class Locator2 : Locator, ILocator2
    {
        private Encoding encoding;
        private string version;

        /// <summary>
        /// Construct a new, empty <see cref="Locator2"/> object.
        /// This will not normally be useful, since the main purpose
        /// of this class is to make a snapshot of an existing <see cref="Locator"/>.
        /// </summary>
        public Locator2() { }

        /// <summary>
        /// Copy an existing <see cref="Locator"/> or <see cref="Locator2"/> object.
        /// If the object implements <see cref="Locator2"/>, values of the
        /// <em>encoding</em> and <em>version</em>strings are copied,
        /// otherwise they set to <em>null</em>. 
        /// </summary>
        /// <param name="locator">The existing Locator object.</param>
        public Locator2(ILocator locator)
            : base(locator)
        {
            if (locator is Locator2 l2)
            {
                version = l2.XMLVersion;
                encoding = l2.Encoding;
            }
        }

        ////////////////////////////////////////////////////////////////////
        // Locator2 method implementations
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current value of the version property.
        /// </summary>
        public string XMLVersion
        { 
            get => version;
            set => version = value;
        }

        /// <summary>
        /// Gets the current value of the encoding property.
        /// </summary>
        public Encoding Encoding
        { 
            get => encoding;
            set => encoding = value;
        }
    }
}
