// Attributes2Impl.java - extended AttributesImpl
// http://www.saxproject.org
// Public Domain: no warranty.
// $Id: Attributes2Impl.java,v 1.5 2004/03/08 13:01:01 dmegginson Exp $

using Lucene.Net.Support;
using Sax.Helpers;
using System;

namespace Sax.Ext
{
    /// <summary>
    /// SAX2 extension helper for additional Attributes information,
    /// implementing the <see cref="Attributes2"/> interface.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// <para/>
    /// This is not part of core-only SAX2 distributions.
    /// <para/>
    /// The <em>specified</em> flag for each attribute will always
    /// be true, unless it has been set to false in the copy constructor
    /// or using <see cref="SetSpecified(int, bool)"/>.
    /// Similarly, the <em>declared</em> flag for each attribute will
    /// always be false, except for defaulted attributes (<em>specified</em>
    /// is false), non-CDATA attributes, or when it is set to true using
    /// <see cref="SetDeclared(int, bool)"/>.
    /// If you change an attribute's type by hand, you may need to modify
    /// its <em>declared</em> flag to match.
    /// </remarks>
    /// <since>SAX 2.0 (extensions 1.1 alpha)</since>
    /// <author>David Brownell</author>
    /// <version>TBS</version>
    public class Attributes2 : Attributes, IAttributes2
    {
        private bool[] declared;
        private bool[] specified;


        /// <summary>
        /// Construct a new, empty <see cref="Attributes2"/> object.
        /// </summary>
        public Attributes2() { }


        /// <summary>
        /// Copy an existing Attributes or Attributes2 object.
        /// If the object implements Attributes2, values of the
        /// <em>specified</em> and <em>declared</em> flags for each
        /// attribute are copied.
        /// Otherwise the flag values are defaulted to assume no DTD was used,
        /// unless there is evidence to the contrary (such as attributes with
        /// type other than CDATA, which must have been <em>declared</em>).
        /// <p>This constructor is especially useful inside a
        /// <see cref="IContentHandler.StartElement(string, string, string, IAttributes)"/> event.</p>
        /// </summary>
        /// <param name="atts">The existing <see cref="IAttributes"/> object.</param>
        public Attributes2(IAttributes atts)
            : base(atts)
        {
        }


        ////////////////////////////////////////////////////////////////////
        // Implementation of Attributes2
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Returns the current value of the attribute's "declared" flag.
        /// </summary>
        // javadoc mostly from interface
        public bool IsDeclared(int index)
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException(
                "No attribute at index: " + index);
            return declared[index];
        }


        /// <summary>
        /// Returns the current value of the attribute's "declared" flag.
        /// </summary>
        // javadoc mostly from interface
        public bool IsDeclared(string uri, string localName)
        {
            int index = GetIndex(uri, localName);

            if (index < 0)
                throw new ArgumentException(
                "No such attribute: local=" + localName
                + ", namespace=" + uri);
            return declared[index];
        }

        /// <summary>
        /// Returns the current value of the attribute's "declared" flag.
        /// </summary>
        // javadoc mostly from interface
        public bool IsDeclared(string qName)
        {
            int index = GetIndex(qName);

            if (index < 0)
                throw new ArgumentException(
                "No such attribute: " + qName);
            return declared[index];
        }

        /// <summary>
        /// Returns the current value of an attribute's "specified" flag.
        /// </summary>
        /// <param name="index">The attribute index (zero-based).</param>
        /// <returns>current flag value</returns>
        /// <exception cref="IndexOutOfRangeException">When the supplied index does not identify an attribute.</exception>
        public bool IsSpecified(int index)
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException(
                "No attribute at index: " + index);
            return specified[index];
        }

        /// <summary>
        /// Returns the current value of an attribute's "specified" flag.
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty string if the name has no Namespace URI.</param>
        /// <param name="localName">The attribute's local name.</param>
        /// <returns>current flag value</returns>      
        /// <exception cref="ArgumentException">When the supplied names do not identify an attribute.</exception>
        public bool IsSpecified(string uri, string localName)
        {
            int index = GetIndex(uri, localName);

            if (index < 0)
                throw new ArgumentException(
                "No such attribute: local=" + localName
                + ", namespace=" + uri);
            return specified[index];
        }

        /// <summary>
        /// Returns the current value of an attribute's "specified" flag.
        /// </summary>
        /// <param name="qName">The XML qualified (prefixed) name.</param>
        /// <returns>current flag value</returns>
        /// <exception cref="ArgumentException">When the supplied name does not identify an attribute.</exception>          
        public bool IsSpecified(string qName)
        {
            int index = GetIndex(qName);

            if (index < 0)
                throw new ArgumentException(
                "No such attribute: " + qName);
            return specified[index];
        }


        ////////////////////////////////////////////////////////////////////
        // Manipulators
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Copy an entire Attributes object.  The "specified" flags are
        /// assigned as true, and "declared" flags as false (except when
        /// an attribute's type is not CDATA),
        /// unless the object is an Attributes2 object.
        /// In that case those flag values are all copied.
        /// </summary>
        /// <seealso cref="Attributes.SetAttributes(IAttributes)"/>
        public override void SetAttributes(IAttributes atts)
        {
            int length = atts.Length;

            base.SetAttributes(atts);
            declared = new bool[length];
            specified = new bool[length];

            if (atts is Attributes2 a2)
            {
                for (int i = 0; i < length; i++)
                {
                    declared[i] = a2.IsDeclared(i);
                    specified[i] = a2.IsSpecified(i);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    declared[i] = !"CDATA".Equals(atts.GetType(i), StringComparison.Ordinal);
                    specified[i] = true;
                }
            }
        }

        /// <summary>
        /// Add an attribute to the end of the list, setting its
        /// "specified" flag to true.  To set that flag's value
        /// to false, use <see cref="SetSpecified(int, bool)"/>.
        /// <para/>
        /// Unless the attribute <em>type</em> is CDATA, this attribute
        /// is marked as being declared in the DTD.  To set that flag's value
        /// to true for CDATA attributes, use <see cref="SetDeclared(int, bool)"/>.
        /// </summary>
        /// <seealso cref="Attributes.AddAttribute(string, string, string, string, string)"/>
        public override void AddAttribute(string uri, string localName, string qName,
                      string type, string value)
        {
            base.AddAttribute(uri, localName, qName, type, value);

            int length = Length;

            if (length < specified.Length)
            {
                bool[] newFlags;

                newFlags = new bool[length];
                Arrays.Copy(declared, 0, newFlags, 0, declared.Length);
                declared = newFlags;

                newFlags = new bool[length];
                Arrays.Copy(specified, 0, newFlags, 0, specified.Length);
                specified = newFlags;
            }

            specified[length - 1] = true;
            declared[length - 1] = !"CDATA".Equals(type, StringComparison.Ordinal);
        }

        // javadoc entirely from superclass
        public override void RemoveAttribute(int index)
        {
            int origMax = Length - 1;

            base.RemoveAttribute(index);
            if (index != origMax)
            {
                Arrays.Copy(declared, index + 1, declared, index,
                    origMax - index);
                Arrays.Copy(specified, index + 1, specified, index,
                    origMax - index);
            }
        }

        /// <summary>
        /// Assign a value to the "declared" flag of a specific attribute.
        /// This is normally needed only for attributes of type CDATA,
        /// including attributes whose type is changed to or from CDATA.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="value">The desired flag value.</param>
        /// <exception cref="IndexOutOfRangeException">When the supplied index does not identify an attribute.</exception>
        public virtual void SetDeclared(int index, bool value)
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException(
                "No attribute at index: " + index);
            declared[index] = value;
        }

        /// <summary>
        /// Assign a value to the "specified" flag of a specific attribute.
        /// This is the only way this flag can be cleared, except clearing
        /// by initialization with the copy constructor.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="value">The desired flag value.</param>
        /// <exception cref="IndexOutOfRangeException">When the supplied index does not identify an attribute.</exception>
        public virtual void SetSpecified(int index, bool value)
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException(
                "No attribute at index: " + index);
            specified[index] = value;
        }
    }
}
