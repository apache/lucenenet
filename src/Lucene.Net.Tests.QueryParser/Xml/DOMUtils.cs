/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Javax.Xml.Parsers;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.W3c.Dom;
using Org.Xml.Sax;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml
{
	/// <summary>Helper methods for parsing XML</summary>
	public class DOMUtils
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public static Element GetChildByTagOrFail(Element e, string name)
		{
			Element kid = GetChildByTagName(e, name);
			if (null == kid)
			{
				throw new ParserException(e.GetTagName() + " missing \"" + name + "\" child element"
					);
			}
			return kid;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public static Element GetFirstChildOrFail(Element e)
		{
			Element kid = GetFirstChildElement(e);
			if (null == kid)
			{
				throw new ParserException(e.GetTagName() + " does not contain a child element");
			}
			return kid;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public static string GetAttributeOrFail(Element e, string name)
		{
			string v = e.GetAttribute(name);
			if (null == v)
			{
				throw new ParserException(e.GetTagName() + " missing \"" + name + "\" attribute");
			}
			return v;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public static string GetAttributeWithInheritanceOrFail(Element e, string name)
		{
			string v = GetAttributeWithInheritance(e, name);
			if (null == v)
			{
				throw new ParserException(e.GetTagName() + " missing \"" + name + "\" attribute");
			}
			return v;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public static string GetNonBlankTextOrFail(Element e)
		{
			string v = GetText(e);
			if (null != v)
			{
				v = v.Trim();
			}
			if (null == v || 0 == v.Length)
			{
				throw new ParserException(e.GetTagName() + " has no text");
			}
			return v;
		}

		public static Element GetChildByTagName(Element e, string name)
		{
			for (Node kid = e.GetFirstChild(); kid != null; kid = kid.GetNextSibling())
			{
				if ((kid.GetNodeType() == Node.ELEMENT_NODE) && (name.Equals(kid.GetNodeName())))
				{
					return (Element)kid;
				}
			}
			return null;
		}

		/// <summary>Returns an attribute value from this node, or first parent node with this attribute defined
		/// 	</summary>
		/// <returns>A non-zero-length value if defined, otherwise null</returns>
		public static string GetAttributeWithInheritance(Element element, string attributeName
			)
		{
			string result = element.GetAttribute(attributeName);
			if ((result == null) || (string.Empty.Equals(result)))
			{
				Node n = element.GetParentNode();
				if ((n == element) || (n == null))
				{
					return null;
				}
				if (n is Element)
				{
					Element parent = (Element)n;
					return GetAttributeWithInheritance(parent, attributeName);
				}
				return null;
			}
			//we reached the top level of the document without finding attribute
			return result;
		}

		public static string GetChildTextByTagName(Element e, string tagName)
		{
			Element child = GetChildByTagName(e, tagName);
			return child != null ? GetText(child) : null;
		}

		public static Element InsertChild(Element parent, string tagName, string text)
		{
			Element child = parent.GetOwnerDocument().CreateElement(tagName);
			parent.AppendChild(child);
			if (text != null)
			{
				child.AppendChild(child.GetOwnerDocument().CreateTextNode(text));
			}
			return child;
		}

		public static string GetAttribute(Element element, string attributeName, string deflt
			)
		{
			string result = element.GetAttribute(attributeName);
			return (result == null) || (string.Empty.Equals(result)) ? deflt : result;
		}

		public static float GetAttribute(Element element, string attributeName, float deflt
			)
		{
			string result = element.GetAttribute(attributeName);
			return (result == null) || (string.Empty.Equals(result)) ? deflt : float.ParseFloat
				(result);
		}

		public static int GetAttribute(Element element, string attributeName, int deflt)
		{
			string result = element.GetAttribute(attributeName);
			return (result == null) || (string.Empty.Equals(result)) ? deflt : System.Convert.ToInt32
				(result);
		}

		public static bool GetAttribute(Element element, string attributeName, bool deflt
			)
		{
			string result = element.GetAttribute(attributeName);
			return (result == null) || (string.Empty.Equals(result)) ? deflt : Sharpen.Extensions.ValueOf
				(result);
		}

		//MH changed to Node from Element 25/11/2005
		public static string GetText(Node e)
		{
			StringBuilder sb = new StringBuilder();
			GetTextBuffer(e, sb);
			return sb.ToString();
		}

		public static Element GetFirstChildElement(Element element)
		{
			for (Node kid = element.GetFirstChild(); kid != null; kid = kid.GetNextSibling())
			{
				if (kid.GetNodeType() == Node.ELEMENT_NODE)
				{
					return (Element)kid;
				}
			}
			return null;
		}

		private static void GetTextBuffer(Node e, StringBuilder sb)
		{
			for (Node kid = e.GetFirstChild(); kid != null; kid = kid.GetNextSibling())
			{
				switch (kid.GetNodeType())
				{
					case Node.TEXT_NODE:
					{
						sb.Append(kid.GetNodeValue());
						break;
					}

					case Node.ELEMENT_NODE:
					{
						GetTextBuffer(kid, sb);
						break;
					}

					case Node.ENTITY_REFERENCE_NODE:
					{
						GetTextBuffer(kid, sb);
						break;
					}
				}
			}
		}

		/// <summary>Helper method to parse an XML file into a DOM tree, given a reader.</summary>
		/// <remarks>Helper method to parse an XML file into a DOM tree, given a reader.</remarks>
		/// <param name="is">reader of the XML file to be parsed</param>
		/// <returns>an org.w3c.dom.Document object</returns>
		public static Document LoadXML(StreamReader @is)
		{
			DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
			DocumentBuilder db = null;
			try
			{
				db = dbf.NewDocumentBuilder();
			}
			catch (Exception se)
			{
				throw new RuntimeException("Parser configuration error", se);
			}
			// Step 3: parse the input file
			Document doc = null;
			try
			{
				doc = db.Parse(new InputSource(@is));
			}
			catch (Exception se)
			{
				//doc = db.parse(is);
				throw new RuntimeException("Error parsing file:" + se, se);
			}
			return doc;
		}
	}
}
