using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml
{
    public static class DOMUtils
    {
        public static XElement GetChildByTagOrFail(XElement e, string name)
        {
            XElement kid = GetChildByTagName(e, name);
            if (null == kid)
            {
                throw new ParserException(e.Name.LocalName + " missing \"" + name
                    + "\" child element");
            }
            return kid;
        }

        public static XElement GetFirstChildOrFail(XElement e)
        {
            XElement kid = GetFirstChildElement(e);
            if (null == kid)
            {
                throw new ParserException(e.Name.LocalName
                    + " does not contain a child element");
            }
            return kid;
        }

        public static string GetAttributeOrFail(XElement e, string name)
        {
            string v = e.Attributes(XName.Get(name)).Select(i => i.Value).FirstOrDefault();
            if (null == v)
            {
                throw new ParserException(e.Name.LocalName + " missing \"" + name
                    + "\" attribute");
            }
            return v;
        }

        public static string AetAttributeWithInheritanceOrFail(XElement e, string name)
        {
            string v = GetAttributeWithInheritance(e, name);
            if (null == v)
            {
                throw new ParserException(e.Name.LocalName + " missing \"" + name
                    + "\" attribute");
            }
            return v;
        }

        public static string GetNonBlankTextOrFail(XElement e)
        {
            string v = GetText(e);
            if (null != v)
                v = v.Trim();
            if (null == v || 0 == v.Length)
            {
                throw new ParserException(e.Name.LocalName + " has no text");
            }
            return v;
        }

        /* Convenience method where there is only one child Element of a given name */
        public static XElement GetChildByTagName(XElement e, string name)
        {
            for (XNode kid = e.FirstNode; kid != null; kid = kid.NextNode)
            {
                if ((kid.NodeType == XmlNodeType.Element) && (name.Equals(((XElement)kid).Name.LocalName)))
                {
                    return (XElement)kid;
                }
            }
            return null;
        }

        public static string GetAttributeWithInheritance(XElement element, string attributeName)
        {
            var result = element.Attribute(XName.Get(attributeName));
            if ((result == null) || ("".Equals(result.Value)))
            {
                XNode n = element.Parent;
                if ((n == element) || (n == null))
                {
                    return null;
                }
                if (n is XElement)
                {
                    XElement parent = (XElement)n;
                    return GetAttributeWithInheritance(parent, attributeName);
                }
                return null; //we reached the top level of the document without finding attribute
            }
            return result.Value;
        }

        public static string GetChildTextByTagName(XElement e, string tagName)
        {
            XElement child = GetChildByTagName(e, tagName);
            return child != null ? GetText(child) : null;
        }

        public static XElement InsertChild(XElement parent, string tagName, string text)
        {
            XElement child = new XElement(XName.Get(tagName));
            parent.Add(child);
            if (text != null)
            {
                child.Add(new XText(text));
            }
            return child;
        }

        public static string GetAttribute(XElement element, string attributeName, string deflt)
        {
            var result = element.Attribute(XName.Get(attributeName));
            return (result == null) || ("".Equals(result.Value)) ? deflt : result.Value;
        }

        public static float GetAttribute(XElement element, string attributeName, float deflt)
        {
            var result = element.Attribute(XName.Get(attributeName));
            return (result == null) || ("".Equals(result.Value)) ? deflt : float.Parse(result.Value);
        }

        public static int GetAttribute(XElement element, string attributeName, int deflt)
        {
            var result = element.Attribute(XName.Get(attributeName));
            return (result == null) || ("".Equals(result.Value)) ? deflt : int.Parse(result.Value);
        }

        public static bool GetAttribute(XElement element, string attributeName, bool deflt)
        {
            var result = element.Attribute(XName.Get(attributeName));
            return (result == null) || ("".Equals(result.Value)) ? deflt : bool.Parse(result.Value);
        }

        public static string GetText(XNode e)
        {
            StringBuilder sb = new StringBuilder();
            GetTextBuffer(e, sb);
            return sb.ToString();
        }

        public static XElement GetFirstChildElement(XElement element)
        {
            for (XNode kid = element.FirstNode; kid != null; kid = kid.NextNode)
            {
                if (kid.NodeType == XmlNodeType.Element)
                {
                    return (XElement)kid;
                }
            }
            return null;
        }

        private static void GetTextBuffer(XNode e, StringBuilder sb)
        {
            XText text = e as XText;

            if (text != null)
                sb.Append(text.Value);

            XElement element = e as XElement;

            if (element != null)
            {
                for (XNode kid = element.FirstNode; kid != null; kid = kid.NextNode)
                {
                    GetTextBuffer(kid, sb);
                }
            }
        }

        public static XDocument LoadXML(TextReader input)
        {
            return XDocument.Load(input);
        }
    }
}
