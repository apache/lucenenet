// Attributes2.java - extended Attributes
// http://www.saxproject.org
// Public Domain: no warranty.
// $Id: Attributes2.java,v 1.6 2004/03/08 13:01:00 dmegginson Exp $

using System;

namespace Sax.Ext
{
    /// <summary>
    /// SAX2 extension to augment the per-attribute information
    /// provided though <see cref="IAttributes"/>.
    /// If an implementation supports this extension, the attributes
    /// provided in <see cref="IContentHandler"/>
    /// will implement this interface,
    /// and the<em> http://xml.org/sax/features/use-attributes2</em>
    /// feature flag will have the value<em>true</em>.
    /// <para/>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// <para/>
    /// XMLReader implementations are not required to support this
    /// information, and it is not part of core-only SAX2 distributions.
    /// <para/>
    /// Note that if an attribute was defaulted (<em>!IsSpecified()</em>)
    /// it will of necessity also have been declared(<em>IsDeclared()</em>)
    /// in the DTD.
    /// Similarly if an attribute's type is anything except CDATA, then it
    /// must have been declared.
    /// </summary>
    /// <since>SAX 2.0 (extensions 1.1 alpha)</since>
    /// <author>David Brownell</author>
    /// <version>TBS</version>
    public interface IAttributes2 : IAttributes
    {
        /// <summary>
        /// Returns false unless the attribute was declared in the DTD.
        /// This helps distinguish two kinds of attributes that SAX reports
        /// as CDATA:  ones that were declared (and hence are usually valid),
        /// and those that were not (and which are never valid).
        /// </summary>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>true if the attribute was declared in the DTD, false otherwise.</returns>
        /// <exception cref="IndexOutOfRangeException">When the supplied index does not identify an attribute.</exception>
        bool IsDeclared(int index);

        /// <summary>
        /// Returns false unless the attribute was declared in the DTD.
        /// This helps distinguish two kinds of attributes that SAX reports
        /// as CDATA:  ones that were declared (and hence are usually valid),
        /// and those that were not (and which are never valid).
        /// </summary>
        /// <param name="qName">The XML qualified (prefixed) name.</param>
        /// <returns>true if the attribute was declared in the DTD, false otherwise.</returns>
        /// <exception cref="ArgumentException">When the supplied name does not identify an attribute.</exception>
        bool IsDeclared(string qName);

        /// <summary>
        /// Returns false unless the attribute was declared in the DTD.
        /// This helps distinguish two kinds of attributes that SAX reports
        /// as CDATA:  ones that were declared (and hence are usually valid),
        /// and those that were not (and which are never valid).
        /// </summary>
        /// <remarks>
        /// Remember that since DTDs do not "understand" namespaces, the
        /// namespace URI associated with an attribute may not have come from
        /// the DTD.The declaration will have applied to the attribute's
        /// <em>qName</em>
        /// </remarks>
        /// <param name="uri">The Namespace URI, or the empty string if the name has no Namespace URI.</param>
        /// <param name="localName">The attribute's local name.</param>
        /// <returns>true if the attribute was declared in the DTD, false otherwise.</returns>
        /// <exception cref="ArgumentException">When the supplied names do not identify an attribute.</exception>
        bool IsDeclared(string uri, string localName);

        /// <summary>
        /// Returns true unless the attribute value was provided
        /// by DTD defaulting.
        /// </summary>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>true if the value was found in the XML text, false if the value was provided by DTD defaulting.</returns>
        /// <exception cref="IndexOutOfRangeException">When the supplied index does not identify an attribute.</exception>
        bool IsSpecified(int index);

        /// <summary>
        /// Returns true unless the attribute value was provided
        /// by DTD defaulting.
        /// </summary>
        /// <remarks>
        /// Remember that since DTDs do not "understand" namespaces, the
        /// namespace URI associated with an attribute may not have come from
        /// the DTD.The declaration will have applied to the attribute's
        /// <em>qName</em>.
        /// </remarks>
        /// <param name="uri">The Namespace URI, or the empty string if the name has no Namespace URI.</param>
        /// <param name="localName">The attribute's local name.</param>
        /// <returns>true if the value was found in the XML text, false if the value was provided by DTD defaulting.</returns>
        /// <exception cref="ArgumentException">When the supplied names do not identify an attribute.</exception>
        bool IsSpecified(string uri, string localName);

        /// <summary>
        /// Returns true unless the attribute value was provided
        /// by DTD defaulting.
        /// </summary>
        /// <param name="qName">The XML qualified (prefixed) name.</param>
        /// <returns>true if the value was found in the XML text, false if the value was provided by DTD defaulting.</returns>
        /// <exception cref="ArgumentException">When the supplied name does not identify an attribute.</exception>
        bool IsSpecified(string qName);
    }
}
