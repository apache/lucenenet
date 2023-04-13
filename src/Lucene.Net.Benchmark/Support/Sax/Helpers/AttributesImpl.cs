// AttributesImpl.java - default implementation of Attributes.
// http://www.saxproject.org
// Written by David Megginson
// NO WARRANTY!  This class is in the public domain.
// $Id: AttributesImpl.java,v 1.9 2002/01/30 20:52:24 dbrownell Exp $

using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Sax.Helpers
{
    /// <summary>
    /// Default implementation of the <see cref="Attributes"/> interface.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See <a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This class provides a default implementation of the SAX2
    /// <see cref="Attributes"/> interface, with the
    /// addition of manipulators so that the list can be modified or
    /// reused.
    /// <para/>There are two typical uses of this class:
    /// <list type="bullet">
    /// <item><description>to take a persistent snapshot of an Attributes object
    ///  in a <see cref="IContentHandler.StartElement(string, string, string, IAttributes)"/> event; or</description></item>
    /// <item><description>to construct or modify an Attributes object in a SAX2 driver or filter.</description></item>
    /// </list>
    /// <para/>
    /// This class replaces the now-deprecated SAX1 AttributeListImpl 
    /// class; in addition to supporting the updated Attributes
    /// interface rather than the deprecated IAttributeList 
    /// interface, it also includes a much more efficient
    /// implementation using a single array rather than a set of Vectors.
    /// </remarks>
    /// <since>SAX 2.0</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1 (sax2r2)</version>
    public class Attributes : IAttributes
    {
        ////////////////////////////////////////////////////////////////////
        // Constructors.
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Construct a new, empty <see cref="Attributes"/> object.
        /// </summary>
        public Attributes()
        {
            length = 0;
            data = null;
        }

        /// <summary>
        /// Copy an existing Attributes object.
        /// <para/>
        /// This constructor is especially useful inside a
        /// <see cref="IContentHandler.StartElement(string, string, string, IAttributes)"/>.
        ///
        /// Note, this constructor calls a virtual <see cref="SetAttributes(IAttributes)"/> to copy the attributes.
        /// If you are subclassing this class and don't want SetAttributes to be called, you should
        /// use the <see cref="Attributes()"/> constructor instead and call <see cref="SetAttributes(IAttributes)"/> or <see cref="AddAttribute(string,string,string,string,string)"/> yourself if needed.
        /// </summary>
        /// <param name="atts">The existing <see cref="Attributes"/> object.</param>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "There is an Attributes() constructor overload to work around the issue")]
        public Attributes(IAttributes atts)
        {
            SetAttributes(atts);
        }

        ////////////////////////////////////////////////////////////////////
        // Implementation of org.xml.sax.Attributes.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Return the number of attributes in the list.
        /// </summary>
        /// <seealso cref="Attributes.Length"/>
        public virtual int Length => length;

        /// <summary>
        /// Return an attribute's Namespace URI.
        /// </summary>
        /// <param name="index">The attribute's index (zero-based).</param>
        /// <returns>The Namespace URI, the empty string if none is
        /// available, or null if the index is out of range.</returns>
        /// <seealso cref="Attributes.GetURI(int)"/>
        public virtual string GetURI(int index)
        {
            if (index >= 0 && index < length)
            {
                return data[index * 5];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Return an attribute's local name.
        /// </summary>
        /// <param name="index">The attribute's index (zero-based).</param>
        /// <returns>The attribute's local name, the empty string if none is available, or null if the index if out of range.</returns>
        /// <seealso cref="Attributes.GetLocalName(int)"/>
        public virtual string GetLocalName(int index)
        {
            if (index >= 0 && index < length)
            {
                return data[index * 5 + 1];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Return an attribute's qualified (prefixed) name.
        /// </summary>
        /// <param name="index">The attribute's index (zero-based).</param>
        /// <returns>The attribute's qualified name, the empty string if
        /// none is available, or null if the index is out of bounds.</returns>
        /// <seealso cref="Attributes.GetQName(int)"/>
        public virtual string GetQName(int index)
        {
            if (index >= 0 && index < length)
            {
                return data[index * 5 + 2];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Return an attribute's type by index.
        /// </summary>
        /// <param name="index">The attribute's index (zero-based).</param>
        /// <returns>The attribute's type, "CDATA" if the type is unknown, or null
        /// if the index is out of bounds.</returns>
        /// <seealso cref="Attributes.GetType(int)"/>
        public virtual string GetType(int index)
        {
            if (index >= 0 && index < length)
            {
                return data[index * 5 + 3];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Return an attribute's value by index.
        /// </summary>
        /// <param name="index">The attribute's index (zero-based).</param>
        /// <returns>The attribute's value or null if the index is out of bounds.</returns>
        /// <seealso cref="Attributes.GetValue(int)"/>
        public virtual string GetValue(int index)
        {
            if (index >= 0 && index < length)
            {
                return data[index * 5 + 4];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Look up an attribute's index by Namespace name.
        /// </summary>
        /// <remarks>In many cases, it will be more efficient to look up the name once and
        /// use the index query methods rather than using the name query methods
        /// repeatedly.</remarks>
        /// <param name="uri">The attribute's Namespace URI, or the empty
        /// string if none is available.</param>
        /// <param name="localName">The attribute's local name.</param>
        /// <returns>The attribute's index, or -1 if none matches.</returns>
        /// <seealso cref="Attributes.GetIndex(string, string)"/>
        public virtual int GetIndex(string uri, string localName)
        {
            int max = length * 5;
            for (int i = 0; i < max; i += 5)
            {
                if (data[i].Equals(uri, StringComparison.Ordinal) && data[i + 1].Equals(localName, StringComparison.Ordinal))
                {
                    return i / 5;
                }
            }
            return -1;
        }


        /// <summary>
        /// Look up an attribute's index by qualified (prefixed) name.
        /// </summary>
        /// <param name="qName">The qualified name.</param>
        /// <returns>The attribute's index, or -1 if none matches.</returns>
        /// <seealso cref="Attributes.GetIndex(string)"/>
        public virtual int GetIndex(string qName)
        {
            int max = length * 5;
            for (int i = 0; i < max; i += 5)
            {
                if (data[i + 2].Equals(qName, StringComparison.Ordinal))
                {
                    return i / 5;
                }
            }
            return -1;
        }


        /// <summary>
        /// Look up an attribute's type by Namespace-qualified name.
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty string for a name
        /// with no explicit Namespace URI.</param>
        /// <param name="localName">The local name.</param>
        /// <returns>The attribute's type, or null if there is no matching attribute.</returns>
        /// <seealso cref="Attributes.GetType(string, string)"/>
        public virtual string GetType(string uri, string localName)
        {
            int max = length * 5;
            for (int i = 0; i < max; i += 5)
            {
                if (data[i].Equals(uri, StringComparison.Ordinal) && data[i + 1].Equals(localName, StringComparison.Ordinal))
                {
                    return data[i + 3];
                }
            }
            return null;
        }


        /// <summary>
        /// Look up an attribute's type by qualified (prefixed) name.
        /// </summary>
        /// <param name="qName">The qualified name.</param>
        /// <returns>The attribute's type, or null if there is no
        /// matching attribute.</returns>
        /// <seealso cref="Attributes.GetType(string)"/>
        public virtual string GetType(string qName)
        {
            int max = length * 5;
            for (int i = 0; i < max; i += 5)
            {
                if (data[i + 2].Equals(qName, StringComparison.Ordinal))
                {
                    return data[i + 3];
                }
            }
            return null;
        }


        /// <summary>
        /// Look up an attribute's value by Namespace-qualified name.
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty string for a name
        /// with no explicit Namespace URI.</param>
        /// <param name="localName">The local name.</param>
        /// <returns>The attribute's value, or null if there is no matching attribute.</returns>
        /// <seealso cref="Attributes.GetValue(string, string)"/>
        public virtual string GetValue(string uri, string localName)
        {
            int max = length * 5;
            for (int i = 0; i < max; i += 5)
            {
                if (data[i].Equals(uri, StringComparison.Ordinal) && data[i + 1].Equals(localName, StringComparison.Ordinal))
                {
                    return data[i + 4];
                }
            }
            return null;
        }


        /// <summary>
        /// Look up an attribute's value by qualified (prefixed) name.
        /// </summary>
        /// <param name="qName">The qualified name.</param>
        /// <returns>The attribute's value, or null if there is no
        /// matching attribute.</returns>
        /// <seealso cref="Attributes.GetValue(string)"/>
        public virtual string GetValue(string qName)
        {
            int max = length * 5;
            for (int i = 0; i < max; i += 5)
            {
                if (data[i + 2].Equals(qName, StringComparison.Ordinal))
                {
                    return data[i + 4];
                }
            }
            return null;
        }



        ////////////////////////////////////////////////////////////////////
        // Manipulators.
        ////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Clear the attribute list for reuse.
        /// <para/>
        /// Note that little memory is freed by this call:
        /// the current array is kept so it can be 
        /// reused.
        /// </summary>
        public virtual void Clear()
        {
            if (data != null)
            {
                for (int i = 0; i < (length * 5); i++)
                    data[i] = null;
            }
            length = 0;
        }

        /// <summary>
        /// Copy an entire Attributes object.
        /// <para/>
        /// It may be more efficient to reuse an existing object
        /// rather than constantly allocating new ones.
        /// </summary>
        /// <param name="atts">The attributes to copy.</param>
        public virtual void SetAttributes(IAttributes atts)
        {
            Clear();
            length = atts.Length;
            if (length > 0)
            {
                data = new string[length * 5];
                for (int i = 0; i < length; i++)
                {
                    data[i * 5] = atts.GetURI(i);
                    data[i * 5 + 1] = atts.GetLocalName(i);
                    data[i * 5 + 2] = atts.GetQName(i);
                    data[i * 5 + 3] = atts.GetType(i);
                    data[i * 5 + 4] = atts.GetValue(i);
                }
            }
        }


        /// <summary>
        /// Add an attribute to the end of the list.
        /// <para/>For the sake of speed, this method does no checking
        /// to see if the attribute is already in the list: that is
        /// the responsibility of the application.
        /// </summary>
        /// <param name="uri">The Namespace URI, or the empty string if
        /// none is available or Namespace processing is not
        /// being performed.</param>
        /// <param name="localName">The local name, or the empty string if
        /// Namespace processing is not being performed.</param>
        /// <param name="qName">The qualified (prefixed) name, or the empty string
        /// if qualified names are not available.</param>
        /// <param name="type">The attribute type as a string.</param>
        /// <param name="value">The attribute value.</param>
        public virtual void AddAttribute(string uri, string localName, string qName,
                      string type, string value)
        {
            EnsureCapacity(length + 1);
            data[length * 5] = uri;
            data[length * 5 + 1] = localName;
            data[length * 5 + 2] = qName;
            data[length * 5 + 3] = type;
            data[length * 5 + 4] = value;
            length++;
        }


        /// <summary>
        /// Set an attribute in the list.
        /// 
        /// <para/>For the sake of speed, this method does no checking
        /// for name conflicts or well-formedness: such checks are the
        /// responsibility of the application.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="uri">The Namespace URI, or the empty string if
        /// none is available or Namespace processing is not
        /// being performed.</param>
        /// <param name="localName">The local name, or the empty string if
        /// Namespace processing is not being performed.</param>
        /// <param name="qName">The qualified name, or the empty string
        /// if qualified names are not available.</param>
        /// <param name="type">The attribute type as a string.</param>
        /// <param name="value">The attribute value.</param>
        /// <exception cref="IndexOutOfRangeException">When the
        /// supplied index does not point to an attribute
        /// in the list.</exception>    
        public virtual void SetAttribute(int index, string uri, string localName,
                      string qName, string type, string value)
        {
            if (index >= 0 && index < length)
            {
                data[index * 5] = uri;
                data[index * 5 + 1] = localName;
                data[index * 5 + 2] = qName;
                data[index * 5 + 3] = type;
                data[index * 5 + 4] = value;
            }
            else
            {
                BadIndex(index);
            }
        }

        /// <summary>
        /// Remove an attribute from the list.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <exception cref="IndexOutOfRangeException">When the supplied index does not point to an attribute in the list.</exception>
        public virtual void RemoveAttribute(int index)
        {
            if (index >= 0 && index < length)
            {
                if (index < length - 1)
                {
                    Arrays.Copy(data, (index + 1) * 5, data, index * 5,
                             (length - index - 1) * 5);
                }
                index = (length - 1) * 5;
                data[index++] = null;
                data[index++] = null;
                data[index++] = null;
                data[index++] = null;
                data[index] = null;
                length--;
            }
            else
            {
                BadIndex(index);
            }
        }

        /// <summary>
        /// Set the Namespace URI of a specific attribute.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="uri">The attribute's Namespace URI, or the empty
        /// string for none.</param>
        /// <exception cref="IndexOutOfRangeException">When the
        /// supplied index does not point to an attribute
        /// in the list.</exception>      
        public virtual void SetURI(int index, string uri)
        {
            if (index >= 0 && index < length)
            {
                data[index * 5] = uri;
            }
            else
            {
                BadIndex(index);
            }
        }

        /// <summary>
        /// Set the local name of a specific attribute.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="localName">The attribute's local name, or the empty
        /// string for none.</param>
        /// <exception cref="IndexOutOfRangeException">When the
        /// supplied index does not point to an attribute
        /// in the list.</exception>         
        public virtual void SetLocalName(int index, string localName)
        {
            if (index >= 0 && index < length)
            {
                data[index * 5 + 1] = localName;
            }
            else
            {
                BadIndex(index);
            }
        }

        /// <summary>
        /// Set the qualified name of a specific attribute.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="qName">The attribute's qualified name, or the empty
        /// string for none.</param>
        /// <exception cref="IndexOutOfRangeException">When the
        /// supplied index does not point to an attribute
        /// in the list.</exception>    
        public virtual void SetQName(int index, string qName)
        {
            if (index >= 0 && index < length)
            {
                data[index * 5 + 2] = qName;
            }
            else
            {
                BadIndex(index);
            }
        }

        /// <summary>
        /// Set the type of a specific attribute.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="type">The attribute's type.</param>
        /// <exception cref="IndexOutOfRangeException">When the
        /// supplied index does not point to an attribute
        /// in the list.</exception>         
        public virtual void SetType(int index, string type)
        {
            if (index >= 0 && index < length)
            {
                data[index * 5 + 3] = type;
            }
            else
            {
                BadIndex(index);
            }
        }

        /// <summary>
        /// Set the value of a specific attribute.
        /// </summary>
        /// <param name="index">The index of the attribute (zero-based).</param>
        /// <param name="value">The attribute's value.</param>
        /// <exception cref="IndexOutOfRangeException">When the
        /// supplied index does not point to an attribute
        /// in the list.</exception>   
        public virtual void SetValue(int index, string value)
        {
            if (index >= 0 && index < length)
            {
                data[index * 5 + 4] = value;
            }
            else
            {
                BadIndex(index);
            }
        }

        ////////////////////////////////////////////////////////////////////
        // Internal methods.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Ensure the internal array's capacity.
        /// </summary>
        /// <param name="n">The minimum number of attributes that the array must be able to hold.</param>
        private void EnsureCapacity(int n)
        {
            if (n <= 0)
            {
                return;
            }
            int max;
            if (data is null || data.Length == 0)
            {
                max = 25;
            }
            else if (data.Length >= n * 5)
            {
                return;
            }
            else
            {
                max = data.Length;
            }
            while (max < n * 5)
            {
                max *= 2;
            }

            string[] newData = new string[max];
            if (length > 0)
            {
                Arrays.Copy(data, 0, newData, 0, length * 5);
            }
            data = newData;
        }

        /// <summary>
        /// Report a bad array index in a manipulator.
        /// </summary>
        /// <param name="index">The index to report.</param>
        /// <exception cref="IndexOutOfRangeException">Always.</exception>
        private static void BadIndex(int index) // LUCENENET: CA1822: Mark members as static
        {
            string msg =
                "Attempt to modify attribute at illegal index: " + index;
            throw new IndexOutOfRangeException(msg);
        }


        ////////////////////////////////////////////////////////////////////
        // Internal state.
        ////////////////////////////////////////////////////////////////////

        private int length;
        private string[] data;
    }
}
