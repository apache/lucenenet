using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Lucene.Net.QueryParsers.Xml
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Helper methods for parsing XML
    /// </summary>
    public static class DOMUtils // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public static XmlElement GetChildByTagOrFail(XmlElement e, string name)
        {
            XmlElement kid = GetChildByTagName(e, name);
            if (null == kid)
            {
                throw new ParserException(e.ToString() + " missing \"" + name
                    + "\" child element");
            }
            return kid;
        }

        public static XmlElement GetFirstChildOrFail(XmlElement e)
        {
            XmlElement kid = GetFirstChildElement(e);
            if (null == kid)
            {
                throw new ParserException(e.ToString()
                    + " does not contain a child element");
            }
            return kid;
        }

        public static string GetAttributeOrFail(XmlElement e, string name)
        {
            string v = e.GetAttribute(name);
            if (null == v)
            {
                throw new ParserException(e.ToString() + " missing \"" + name
                    + "\" attribute");
            }
            return v;
        }

        public static string GetAttributeWithInheritanceOrFail(XmlElement e, string name)
        {
            string v = GetAttributeWithInheritance(e, name);
            if (null == v)
            {
                throw new ParserException(e.ToString() + " missing \"" + name
                    + "\" attribute");
            }
            return v;
        }

        public static string GetNonBlankTextOrFail(XmlElement e)
        {
            string v = GetText(e);
            if (null != v)
                v = v.Trim();
            if (null == v || 0 == v.Length)
            {
                throw new ParserException(e.ToString() + " has no text");
            }
            return v;
        }

        /// <summary>Convenience method where there is only one child <see cref="XmlElement"/> of a given name</summary>
        public static XmlElement GetChildByTagName(XmlElement e, string name)
        {
            for (XmlNode kid = e.FirstChild; kid != null; kid = kid.NextSibling)
            {
                if ((kid.NodeType == XmlNodeType.Element) && (name.Equals(kid.Name, StringComparison.Ordinal)))
                {
                    return (XmlElement)kid;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns an attribute value from this node, or first parent node with this attribute defined
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeName"></param>
        /// <returns>A non-zero-length value if defined, otherwise null</returns>
        public static string GetAttributeWithInheritance(XmlElement element, string attributeName)
        {
            string result = element.GetAttribute(attributeName);
            if ((result is null) || ("".Equals(result, StringComparison.Ordinal)))
            {
                XmlNode n = element.ParentNode;
                if ((n == element) || (n is null))
                {
                    return null;
                }
                if (n is XmlElement parent)
                {
                    return GetAttributeWithInheritance(parent, attributeName);
                }
                return null; //we reached the top level of the document without finding attribute
            }
            return result;
        }


        /// <summary>Convenience method where there is only one child <see cref="XmlElement"/> of a given name</summary>
        public static string GetChildTextByTagName(XmlElement e, string tagName)
        {
            XmlElement child = GetChildByTagName(e, tagName);
            return child != null ? GetText(child) : null;
        }

        /// <summary>Convenience method to append a new child with text</summary>
        public static XmlElement InsertChild(XmlElement parent, string tagName, string text)
        {
            XmlElement child = parent.OwnerDocument.CreateElement(tagName);
            parent.AppendChild(child);
            if (text != null)
            {
                child.AppendChild(child.OwnerDocument.CreateTextNode(text));
            }
            return child;
        }

        public static string GetAttribute(XmlElement element, string attributeName, string deflt)
        {
            string result = element.GetAttribute(attributeName);
            return (result is null) || ("".Equals(result, StringComparison.Ordinal)) ? deflt : result;
        }

        public static float GetAttribute(XmlElement element, string attributeName, float deflt)
        {
            string result = element.GetAttribute(attributeName);
            return (result is null) || ("".Equals(result, StringComparison.Ordinal)) ? deflt : Convert.ToSingle(result, CultureInfo.InvariantCulture);
        }

        public static int GetAttribute(XmlElement element, string attributeName, int deflt)
        {
            string result = element.GetAttribute(attributeName);
            return (result is null) || ("".Equals(result, StringComparison.Ordinal)) ? deflt : Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }

        public static bool GetAttribute(XmlElement element, string attributeName,
                                           bool deflt)
        {
            string result = element.GetAttribute(attributeName);
            return (result is null) || ("".Equals(result, StringComparison.Ordinal)) ? deflt : Convert.ToBoolean(result, CultureInfo.InvariantCulture);
        }

        /* Returns text of node and all child nodes - without markup */
        //MH changed to Node from Element 25/11/2005

        public static string GetText(XmlNode e)
        {
            StringBuilder sb = new StringBuilder();
            GetTextBuffer(e, sb);
            return sb.ToString();
        }

        public static XmlElement GetFirstChildElement(XmlElement element)
        {
            for (XmlNode kid = element.FirstChild; kid != null; kid = kid.NextSibling)
            {
                if (kid.NodeType == XmlNodeType.Element)
                {
                    return (XmlElement)kid;
                }
            }
            return null;
        }

        private static void GetTextBuffer(XmlNode e, StringBuilder sb)
        {
            for (XmlNode kid = e.FirstChild; kid != null; kid = kid.NextSibling)
            {
                switch (kid.NodeType)
                {
                    case XmlNodeType.Text:
                        {
                            sb.Append(kid.Value);
                            break;
                        }
                    case XmlNodeType.Element:
                        {
                            GetTextBuffer(kid, sb);
                            break;
                        }
                    case XmlNodeType.EntityReference:
                        {
                            GetTextBuffer(kid, sb);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Helper method to parse an XML file into a DOM tree, given a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="input">reader of the XML file to be parsed</param>
        /// <returns>an <see cref="XmlDocument"/> object</returns>
        public static XmlDocument LoadXML(TextReader input)
        {
            XmlDocument result = new XmlDocument();
            try
            {
                result.Load(input);
            }
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
            {
                throw RuntimeException.Create("Error parsing file:" + se, se);
            }
            return result;
        }

        /// <summary>
        /// Helper method to parse an XML file into a DOM tree, given a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">reader of the XML file to be parsed</param>
        /// <returns>an <see cref="XmlDocument"/> object</returns>
        // LUCENENET specific
        public static XmlDocument LoadXML(Stream input)
        {
            XmlDocument result = new XmlDocument();
            try
            {
                result.Load(input);
            }
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
            {
                throw RuntimeException.Create("Error parsing file:" + se, se);
            }
            return result;
        }

        /// <summary>
        /// Helper method to parse an XML file into a DOM tree, given an <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="input">reader of the XML file to be parsed</param>
        /// <returns>an <see cref="XmlDocument"/> object</returns>
        // LUCENENET specific
        public static XmlDocument LoadXML(XmlReader input)
        {
            XmlDocument result = new XmlDocument();
            try
            {
                result.Load(input);
            }
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
            {
                throw RuntimeException.Create("Error parsing file:" + se, se);
            }
            return result;
        }
    }
}
