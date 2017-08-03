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
// 
// 
// Model of document

using System;
using System.Collections;

namespace TagSoup
{
    /// <summary>
    /// Abstract class representing a TSSL schema.
    /// Actual TSSL schemas are compiled into concrete subclasses of this class.
    /// </summary>
    public abstract class Schema
    {
        public const int M_ANY = -1;//0xFFFFFFFF;
        public const int M_EMPTY = 0;
        public const int M_PCDATA = 1 << 30;
        public const int M_ROOT = 1 << 31;

        public const int F_RESTART = 1;
        public const int F_CDATA = 2;
        public const int F_NOFORCE = 4;

        private readonly Hashtable theEntities = new Hashtable(); // string -> Character
        private readonly Hashtable theElementTypes = new Hashtable(); // string -> ElementType

        private string theURI = "";
        private string thePrefix = "";
        private ElementType theRoot;
        
        /// <summary>
        /// Add or replace an element type for this schema.
        /// </summary>
        /// <param name="name"> Name (Qname) of the element</param>
        /// <param name="model">Models of the element's content as a vector of bits</param>
        /// <param name="memberOf">Models the element is a member of as a vector of bits</param>
        /// <param name="flags">Flags for the element</param>
        public virtual void ElementType(string name, int model, int memberOf, int flags)
        {
            var e = new ElementType(name, model, memberOf, flags, this);
            theElementTypes[name.ToLower()] = e;
            if (memberOf == M_ROOT)
            {
                theRoot = e;
            }
        }

        /// <summary>
        /// Gets or sets the root element of this schema
        /// </summary>
        public virtual ElementType RootElementType
        {
            get { return theRoot; }
        }

        /// <summary>
        /// Add or replace a default attribute for an element type in this schema.
        /// </summary>
        /// <param name="elemName">Name (Qname) of the element type</param>
        /// <param name="attrName">Name (Qname) of the attribute</param>
        /// <param name="type">Type of the attribute</param>
        /// <param name="value">Default value of the attribute; null if no default</param>
        public virtual void Attribute(string elemName, string attrName, string type, string value)
        {
            ElementType e = GetElementType(elemName);
            if (e == null)
            {
                throw new Exception("Attribute " + attrName + " specified for unknown element type " + elemName);
            }
            e.SetAttribute(attrName, type, value);
        }

        /// <summary>
        /// Specify natural parent of an element in this schema.
        /// </summary>
        /// <param name="name">Name of the child element</param>
        /// <param name="parentName">Name of the parent element</param>
        public virtual void Parent(string name, string parentName)
        {
            ElementType child = GetElementType(name);
            ElementType parent = GetElementType(parentName);
            if (child == null)
            {
                throw new Exception("No child " + name + " for parent " + parentName);
            }
            if (parent == null)
            {
                throw new Exception("No parent " + parentName + " for child " + name);
            }
            child.Parent = parent;
        }

        /// <summary>
        /// Add to or replace a character entity in this schema.
        /// </summary>
        /// <param name="name">Name of the entity</param>
        /// <param name="value">Value of the entity</param>
        public virtual void Entity(string name, int value)
        {
            theEntities[name] = value;
        }

        /// <summary>
        /// Get an <see cref="TagSoup.ElementType"/> by name.
        /// </summary>
        /// <param name="name">Name (Qname) of the element type</param>
        /// <returns>The corresponding <see cref="TagSoup.ElementType"/></returns>
        public virtual ElementType GetElementType(string name)
        {
            return (ElementType)(theElementTypes[name.ToLower()]);
        }

        /// <summary>
        /// Get an entity value by name.
        /// </summary>
        /// <param name="name">Name of the entity</param>
        /// <returns>The corresponding character, or 0 if none</returns>
        public virtual int GetEntity(string name)
        {
            //		System.err.println("%% Looking up entity " + name);
            if (theEntities.ContainsKey(name))
            {
                return (int)theEntities[name];
            }
            return 0;
        }

        /// <summary>
        /// Gets or sets the URI (namespace name) of this schema.
        /// </summary>
        public virtual string Uri
        {
            get { return theURI; }
            set { theURI = value; }
        }

        /// <summary>
        /// Gets ot sets the prefix of this schema.
        /// </summary>
        public virtual string Prefix
        {
            get { return thePrefix; }
            set { thePrefix = value; }
        }
    }
}
