// Attributes.java - attribute list with Namespace support
// http://www.saxproject.org
// Written by David Megginson
// NO WARRANTY!  This class is in the public domain.
// $Id: Attributes.java,v 1.13 2004/03/18 12:28:05 dmegginson Exp $

namespace Sax
{
    /// <summary>
    /// Interface for a list of XML attributes.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This interface allows access to a list of attributes in
    /// three different ways:
    /// <list type="number">
    ///     <item><description>by attribute index;</description></item>
    ///     <item><description>by Namespace-qualified name; or</description></item>
    ///     <item><description>by qualified (prefixed) name.</description></item>
    /// </list>
    /// <para/>
    /// The list will not contain attributes that were declared
    /// #IMPLIED but not specified in the start tag.  It will also not
    /// contain attributes used as Namespace declarations(xmlns*) unless
    /// the <a href="http://xml.org/sax/features/namespace-prefixes">http://xml.org/sax/features/namespace-prefixes</a>
    /// feature is set to <var>true</var> (it is <var>false</var> by
    /// default).
    /// Because SAX2 conforms to the original "Namespaces in XML"
    /// recommendation, it normally does not
    /// give namespace declaration attributes a namespace URI.
    /// <para/>
    /// Some SAX2 parsers may support using an optional feature flag
    /// (<a href="http://xml.org/sax/features/xmlns-uris">http://xml.org/sax/features/xmlns-uris</a>) to request
    /// that those attributes be given URIs, conforming to a later
    /// backwards-incompatible revision of that recommendation.  (The
    /// attribute's "local name" will be the prefix, or "xmlns" when
    /// defining a default element namespace.)  For portability, handler
    /// code should always resolve that conflict, rather than requiring
    /// parsers that can change the setting of that feature flag.
    /// If the namespace-prefixes feature (see above) is
    /// <var>false</var>, access by qualified name may not be available; if
    /// the<code>http://xml.org/sax/features/namespaces</code> feature is
    /// <var>false</var>, access by Namespace-qualified names may not be
    /// available.
    /// <para/>This interface replaces the now-deprecated SAX1 { @link
    /// org.xml.sax.AttributeList AttributeList } interface, which does not
    /// contain Namespace support.In addition to Namespace support, it
    /// adds the<var> getIndex</var> methods (below).
    /// <para/>The order of attributes in the list is unspecified, and will
    /// vary from implementation to implementation.
    /// </remarks>
    /// <since>SAX 2.0</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1 (sax2r2)</version>
    /// <seealso cref="Helpers.Attributes"/>
    /// <seealso cref="Ext.IDeclHandler"/>
    public interface IAttributes
    {
        ////////////////////////////////////////////////////////////////////
        // Indexed access.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Return the number of attributes in the list.
        /// <para/>
        /// Once you know the number of attributes, you can iterate
        /// through the list.
        /// </summary>
        /// <returns>The number of attributes in the list.</returns>
        /// <seealso cref="GetURI(int)"/>
        /// <seealso cref="GetLocalName(int)"/>
        /// <seealso cref="GetQName(int)"/>
        /// <seealso cref="GetType(int)"/>
        /// <seealso cref="GetValue(int)"/>
        int Length { get; }

        /// <summary>
        /// Look up an attribute's Namespace URI by index.
        /// </summary>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>The Namespace URI, or the empty string if none
        /// is available, or null if the index is out of
        /// range.</returns>
        /// <seealso cref="GetURI(int)"/>
        string GetURI(int index);

        /// <summary>
        /// Look up an attribute's local name by index.
        /// </summary>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>The local name, or the empty string if Namespace
        /// processing is not being performed, or null
        /// if the index is out of range.</returns>
        /// <seealso cref="Length"/>
        string GetLocalName(int index);

        /// <summary>
        /// Look up an attribute's XML qualified (prefixed) name by index.
        /// </summary>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>The XML qualified name, or the empty string
        /// if none is available, or null if the index
        /// is out of range.</returns>
        /// <seealso cref="Length"/>
        string GetQName(int index);

        /// <summary>
        /// Look up an attribute's type by index.
        /// </summary>
        /// <remarks>
        /// The attribute type is one of the strings "CDATA", "ID",
        /// "IDREF", "IDREFS", "NMTOKEN", "NMTOKENS", "ENTITY", "ENTITIES",
        /// or "NOTATION" (always in upper case).
        /// <para/>
        /// If the parser has not read a declaration for the attribute,
        /// or if the parser does not report attribute types, then it must
        /// return the value "CDATA" as stated in the XML 1.0 Recommendation
        /// (clause 3.3.3, "Attribute-Value Normalization").
        /// <para/>
        /// For an enumerated attribute that is not a notation, the
        /// parser will report the type as "NMTOKEN".
        /// </remarks>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>The attribute's type as a string, or null if the
        /// index is out of range.</returns>
        /// <seealso cref="Length"/>
        string GetType(int index);

        /// <summary>
        /// Look up an attribute's value by index.
        /// </summary>
        /// <remarks>
        /// If the attribute value is a list of tokens (IDREFS,
        /// ENTITIES, or NMTOKENS), the tokens will be concatenated
        /// into a single string with each token separated by a
        /// single space.
        /// </remarks>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>The attribute's value as a string, or null if the
        /// index is out of range.</returns>
        /// <seealso cref="Length"/>
        string GetValue(int index);

        ////////////////////////////////////////////////////////////////////
        // Name-based query.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Look up the index of an attribute by Namespace name.
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty string if
        /// the name has no Namespace URI.</param>
        /// <param name="localName">The attribute's local name.</param>
        /// <returns>The index of the attribute, or -1 if it does not
        /// appear in the list.</returns>
        int GetIndex(string uri, string localName);

        /// <summary>
        /// Look up the index of an attribute by XML qualified (prefixed) name.
        /// </summary>
        /// <param name="qName">The qualified (prefixed) name.</param>
        /// <returns>The index of the attribute, or -1 if it does not
        /// appear in the list.</returns>
        int GetIndex(string qName);

        /// <summary>
        /// Look up an attribute's type by Namespace name.
        /// <para/>
        /// See <see cref="GetType(int)"/> for a description
        /// of the possible types.
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty String if the
        /// name has no Namespace URI.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns>The attribute type as a string, or null if the
        /// attribute is not in the list or if Namespace
        /// processing is not being performed.</returns>
        string GetType(string uri, string localName);

        /// <summary>
        /// Look up an attribute's type by XML qualified (prefixed) name.
        /// <para/>
        /// See <see cref="GetType(int)"/> for a description
        /// of the possible types.
        /// </summary>
        /// <param name="qName">The XML qualified name.</param>
        /// <returns>The attribute type as a string, or null if the
        /// attribute is not in the list or if qualified names
        /// are not available.</returns>
        string GetType(string qName);

        /// <summary>
        /// Look up an attribute's value by Namespace name.
        /// <para/>
        /// See <see cref="GetValue(int)"/> for a description
        /// of the possible values.
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty String if the
        /// name has no Namespace URI.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns>The attribute value as a string, or null if the
        /// attribute is not in the list.</returns>
        string GetValue(string uri, string localName);

        /// <summary>
        /// Look up an attribute's value by XML qualified (prefixed) name.
        /// <para/>
        /// See <see cref="GetValue(int)"/> for a description
        /// of the possible values.
        /// </summary>
        /// <param name="qName"></param>
        /// <returns></returns>
        string GetValue(string qName);
    }
}
