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

using J2N.Text;
using Sax.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TagSoup
{
    /// <summary>
    /// This class represents an element type in the schema.
    /// An element type has a name, a content model vector, a member-of vector,
    /// a flags vector, default attributes, and a schema to which it belongs.
    /// </summary>
    /// <seealso cref="Schema" />
    public class ElementType
    {
        private readonly Attributes atts; // default attributes
        private readonly string localName; // element type local name
        private readonly string name; // element type name (Qname)
        private readonly string @namespace; // element type namespace name
        private readonly Schema schema; // schema to which this belongs

        /// <summary>
        /// Construct an <see cref="ElementType"/>:
        /// but it's better to use <see cref="Schema.ElementType(string, int, int, int)"/> instead.
        /// The content model, member-of, and flags vectors are specified as ints.
        /// </summary>
        /// <param name="name">The element type name</param>
        /// <param name="model">ORed-together bits representing the content 
        /// models allowed in the content of this element type</param>
        /// <param name="memberOf">ORed-together bits representing the content models
        /// to which this element type belongs</param>
        /// <param name="flags">ORed-together bits representing the flags associated
        /// with this element type</param>
        /// <param name="schema">
        /// The schema with which this element type will be associated
        /// </param>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        public ElementType(string name, int model, int memberOf, int flags, Schema schema)
        {
            this.name = name;
            Model = model;
            MemberOf = memberOf;
            Flags = flags;
            atts = new Attributes();
            this.schema = schema;
            @namespace = GetNamespace(name, false);
            localName = GetLocalName(name);
        }

        /// <summary>
        /// LUCENENET specific constructor that allows the caller to specify the namespace and local name
        /// and is provided to subclasses as an alternative to <see cref="ElementType(string, int, int, int, Schema)"/>
        /// in order to avoid virtual method calls.
        /// </summary>
        public ElementType(
            string name, string @namespace, string localName,
            int model, int memberOf, int flags, Schema schema
            )
        {
            this.name = name;
            Model = model;
            MemberOf = memberOf;
            Flags = flags;
            atts = new Attributes();
            this.schema = schema;
            this.@namespace = @namespace;
            this.localName = localName;
        }

        /// <summary>
        /// Gets the name of this element type.
        /// </summary>
        public virtual string Name => name;

        /// <summary>
        /// Gets the namespace name of this element type.
        /// </summary>
        public virtual string Namespace => @namespace;

        /// <summary>
        /// Gets the local name of this element type.
        /// </summary>
        public virtual string LocalName => localName;

        /// <summary>
        /// Gets or sets the content models of this element type as a vector of bits
        /// </summary>
        public virtual int Model { get; set; }

        /// <summary>
        /// Gets or sets the content models to which this element type belongs as a vector of bits
        /// </summary>
        public virtual int MemberOf { get; set; }

        /// <summary>
        /// Gets or sets the flags associated with this element type as a vector of bits
        /// </summary>
        public virtual int Flags { get; set; }

        /// <summary>
        /// Returns the default attributes associated with this element type.
        /// Attributes of type CDATA that don't have default values are
        /// typically not included.  Other attributes without default values
        /// have an internal value of <c>null</c>.
        /// The return value is an Attributes to allow the caller to mutate
        /// the attributes.
        /// </summary>
        public virtual Attributes Attributes => atts;

        /// <summary>
        /// Gets or sets the parent element type of this element type.
        /// </summary>
        public virtual ElementType Parent { get; set; }

        /// <summary>
        /// Gets the schema which this element type is associated with.
        /// </summary>
        public virtual Schema Schema => schema;

        /// <summary>
        /// Return a namespace name from a Qname.
        /// The attribute flag tells us whether to return an empty namespace
        /// name if there is no prefix, or use the schema default instead.
        /// </summary>
        /// <param name="name">The Qname</param>
        /// <param name="attribute">True if name is an attribute name</param>
        /// <returns>The namespace name</returns>
        public virtual string GetNamespace(string name, bool attribute)
        {
            int colon = name.IndexOf(':');
            if (colon == -1)
            {
                return attribute ? "" : schema.Uri;
            }
            string prefix = name.Substring(0, colon);
            if (prefix.Equals("xml", StringComparison.Ordinal))
            {
                return "http://www.w3.org/XML/1998/namespace";
            }
            return "urn:x-prefix:" + prefix.Intern();
        }

        /// <summary>
        /// Return a local name from a Qname.
        /// </summary>
        /// <param name="name">The Qname</param>
        /// <returns>The local name</returns>
        public virtual string GetLocalName(string name)
        {
            int colon = name.IndexOf(':');
            if (colon == -1)
            {
                return name;
            }
            return name.Substring(colon + 1).Intern();
        }

        /// <summary>
        /// Returns <c>true</c> if this element type can contain another element type.
        /// That is, if any of the models in this element's model vector
        /// match any of the models in the other element type's member-of
        /// vector.
        /// </summary>
        /// <param name="other">The other element type</param>
        public virtual bool CanContain(ElementType other)
        {
            return (Model & other.MemberOf) != 0;
        }

        /// <summary>
        /// Sets an attribute and its value into an <see cref="Sax.IAttributes"/> object.
        /// Attempts to set a namespace declaration are ignored.
        /// </summary>
        /// <param name="atts">The <see cref="Sax.Helpers.Attributes"/> object</param>
        /// <param name="name">The name (Qname) of the attribute</param>
        /// <param name="type">The type of the attribute</param>
        /// <param name="value">The value of the attribute</param>
        public virtual void SetAttribute(Attributes atts, string name, string type, string value)
        {
            if (name.Equals("xmlns", StringComparison.Ordinal) || name.StartsWith("xmlns:", StringComparison.Ordinal))
            {
                return;
            }

            string ns = GetNamespace(name, true);
            string localName = GetLocalName(name);
            int i = atts.GetIndex(name);
            if (i == -1)
            {
                name = name.Intern();
                if (type is null)
                {
                    type = "CDATA";
                }
                if (!type.Equals("CDATA", StringComparison.Ordinal))
                {
                    value = Normalize(value);
                }
                atts.AddAttribute(ns, localName, name, type, value);
            }
            else
            {
                if (type is null)
                {
                    type = atts.GetType(i);
                }
                if (!type.Equals("CDATA", StringComparison.Ordinal))
                {
                    value = Normalize(value);
                }
                atts.SetAttribute(i, ns, localName, name, type, value);
            }
        }

        /// <summary>
        /// Normalize an attribute value (ID-style).
        /// CDATA-style attribute normalization is already done.
        /// </summary>
        /// <param name="value">The value to normalize</param>
        public static string Normalize(string value)
        {
            if (value is null)
            {
                return null;
            }
            value = value.Trim();
            if (value.IndexOf("  ", StringComparison.Ordinal) == -1)
            {
                return value;
            }
            bool space = false;
            var b = new StringBuilder(value.Length);
            foreach (char v in value)
            {
                if (v == ' ')
                {
                    if (!space)
                    {
                        b.Append(v);
                    }
                    space = true;
                }
                else
                {
                    b.Append(v);
                    space = false;
                }
            }
            return b.ToString();
        }

        /// <summary>
        /// Sets an attribute and its value into this element type.
        /// </summary>
        /// <param name="name">The name of the attribute</param>
        /// <param name="type">The type of the attribute</param>
        /// <param name="value">The value of the attribute</param>
        public virtual void SetAttribute(string name, string type, string value)
        {
            SetAttribute(atts, name, type, value);
        }
    }
}
