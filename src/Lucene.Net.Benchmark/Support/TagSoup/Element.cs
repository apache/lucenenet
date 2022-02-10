// This file is part of TagSoup and is Copyright 2002-2008 by John Cowan.
//
// TagSoup is licensed under the Apache License,
// Version 2.0.  You may obtain a copy of this license at
// http://www.apache.org/licenses/LICENSE-2.0 .  You may also have
// additional legal rights not granted by this license.
//
// TagSoup is distributed in the hope that it will be useful, but
// unless required by applicable law or agreed to in writing, TagSoup
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, either express or implied; not even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

using Sax.Helpers;
using System;

namespace TagSoup
{
    /// <summary>
    /// The internal representation of an actual element (not an element type).
    /// An Element has an element type, attributes, and a successor Element
    /// for use in constructing stacks and queues of Elements.
    /// </summary>
    /// <seealso cref="ElementType" />
    /// <seealso cref="Sax.Helpers.Attributes" />
    public class Element
    {
        private readonly Attributes _atts; // attributes of element
        private readonly ElementType _type; // type of element
        private bool _preclosed; // this element has been preclosed

        /// <summary>
        /// Return an Element from a specified ElementType.
        /// </summary>
        /// <param name="type">
        /// The element type of the newly constructed element
        /// </param>
        /// <param name="defaultAttributes">
        /// True if default attributes are wanted
        /// </param>
        public Element(ElementType type, bool defaultAttributes)
        {
            _type = type;
            if (defaultAttributes)
            {
                _atts = new Attributes(type.Attributes);
            }
            else
            {
                _atts = new Attributes();
            }
            Next = null;
            _preclosed = false;
        }

        /// <summary>
        /// Gets the element type.
        /// </summary>
        public virtual ElementType Type => _type;

        /// <summary>
        /// Gets the attributes as an Attributes object.
        /// Returning an Attributes makes the attributes mutable.
        /// </summary>
        /// <seealso cref="Attributes" />
        public virtual Attributes Attributes => _atts;

        /// <summary>
        /// Gets or sets the next element in an element stack or queue.
        /// </summary>
        public virtual Element Next { get; set; }

        /// <summary>
        /// Gets the name of the element's type.
        /// </summary>
        public virtual string Name => _type.Name;

        /// <summary>
        /// Gets the namespace name of the element's type.
        /// </summary>
        public virtual string Namespace => _type.Namespace;

        /// <summary>
        /// Gets the local name of the element's type.
        /// </summary>
        public virtual string LocalName => _type.LocalName;

        /// <summary>
        /// Gets the content model vector of the element's type.
        /// </summary>
        public virtual int Model => _type.Model;

        /// <summary>
        /// Gets the member-of vector of the element's type.
        /// </summary>
        public virtual int MemberOf => _type.MemberOf;

        /// <summary>
        /// Gets the flags vector of the element's type.
        /// </summary>
        public virtual int Flags => _type.Flags;

        /// <summary>
        /// Gets the parent element type of the element's type.
        /// </summary>
        public virtual ElementType Parent => _type.Parent;

        /// <summary>
        /// Return true if this element has been preclosed.
        /// </summary>
        public virtual bool IsPreclosed => _preclosed;

        /// <summary>
        /// Return true if the type of this element can contain the type of
        /// another element.
        /// Convenience method.
        /// </summary>
        /// <param name="other">
        /// The other element
        /// </param>
        public virtual bool CanContain(Element other)
        {
            return _type.CanContain(other._type);
        }

        /// <summary>
        /// Set an attribute and its value into this element.
        /// </summary>
        /// <param name="name">
        /// The attribute name (Qname)
        /// </param>
        /// <param name="type">
        /// The attribute type
        /// </param>
        /// <param name="value">
        /// The attribute value
        /// </param>
        public virtual void SetAttribute(string name, string type, string value)
        {
            _type.SetAttribute(_atts, name, type, value);
        }

        /// <summary>
        /// Make this element anonymous.
        /// Remove any <c>id</c> or <c>name</c> attribute present
        /// in the element's attributes.
        /// </summary>
        public virtual void Anonymize()
        {
            for (int i = _atts.Length - 1; i >= 0; i--)
            {
                if (_atts.GetType(i).Equals("ID", StringComparison.Ordinal) || _atts.GetQName(i).Equals("name", StringComparison.Ordinal))
                {
                    _atts.RemoveAttribute(i);
                }
            }
        }

        /// <summary>
        /// Clean the attributes of this element.
        /// Attributes with null name (the name was ill-formed)
        /// or null value (the attribute was present in the element type but
        /// not in this actual element) are removed.
        /// </summary>
        public virtual void Clean()
        {
            for (int i = _atts.Length - 1; i >= 0; i--)
            {
                string name = _atts.GetLocalName(i);
                if (_atts.GetValue(i) is null || string.IsNullOrEmpty(name))
                {
                    _atts.RemoveAttribute(i);
                }
            }
        }

        /// <summary>
        /// Force this element to preclosed status, meaning that an end-tag has
        /// been seen but the element cannot yet be closed for structural reasons.
        /// </summary>
        public virtual void Preclose()
        {
            _preclosed = true;
        }
    }
}
