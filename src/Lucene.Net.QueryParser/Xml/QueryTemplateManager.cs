/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Javax.Xml.Parsers;
using Javax.Xml.Transform;
using Javax.Xml.Transform.Dom;
using Javax.Xml.Transform.Stream;
using Lucene.Net.Queryparser.Xml;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml
{
	/// <summary>
	/// Provides utilities for turning query form input (such as from a web page or Swing gui) into
	/// Lucene XML queries by using XSL templates.
	/// </summary>
	/// <remarks>
	/// Provides utilities for turning query form input (such as from a web page or Swing gui) into
	/// Lucene XML queries by using XSL templates.  This approach offers a convenient way of externalizing
	/// and changing how user input is turned into Lucene queries.
	/// Database applications often adopt similar practices by externalizing SQL in template files that can
	/// be easily changed/optimized by a DBA.
	/// The static methods can be used on their own or by creating an instance of this class you can store and
	/// re-use compiled stylesheets for fast use (e.g. in a server environment)
	/// </remarks>
	public class QueryTemplateManager
	{
		internal static readonly DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance
			();

		internal static readonly TransformerFactory tFactory = TransformerFactory.NewInstance
			();

		internal Dictionary<string, Templates> compiledTemplatesCache = new Dictionary<string
			, Templates>();

		internal Templates defaultCompiledTemplates = null;

		public QueryTemplateManager()
		{
		}

		/// <exception cref="Javax.Xml.Transform.TransformerConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public QueryTemplateManager(InputStream xslIs)
		{
			AddDefaultQueryTemplate(xslIs);
		}

		/// <exception cref="Javax.Xml.Transform.TransformerConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AddDefaultQueryTemplate(InputStream xslIs)
		{
			defaultCompiledTemplates = GetTemplates(xslIs);
		}

		/// <exception cref="Javax.Xml.Transform.TransformerConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AddQueryTemplate(string name, InputStream xslIs)
		{
			compiledTemplatesCache.Put(name, GetTemplates(xslIs));
		}

		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public virtual string GetQueryAsXmlString(Properties formProperties, string queryTemplateName
			)
		{
			Templates ts = compiledTemplatesCache.Get(queryTemplateName);
			return GetQueryAsXmlString(formProperties, ts);
		}

		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public virtual Document GetQueryAsDOM(Properties formProperties, string queryTemplateName
			)
		{
			Templates ts = compiledTemplatesCache.Get(queryTemplateName);
			return GetQueryAsDOM(formProperties, ts);
		}

		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public virtual string GetQueryAsXmlString(Properties formProperties)
		{
			return GetQueryAsXmlString(formProperties, defaultCompiledTemplates);
		}

		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public virtual Document GetQueryAsDOM(Properties formProperties)
		{
			return GetQueryAsDOM(formProperties, defaultCompiledTemplates);
		}

		/// <summary>Fast means of constructing query using a precompiled stylesheet</summary>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public static string GetQueryAsXmlString(Properties formProperties, Templates template
			)
		{
			// TODO: Suppress XML header with encoding (as Strings have no encoding)
			StringWriter writer = new StringWriter();
			StreamResult result = new StreamResult(writer);
			TransformCriteria(formProperties, template, result);
			return writer.ToString();
		}

		/// <summary>Slow means of constructing query parsing a stylesheet from an input stream
		/// 	</summary>
		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public static string GetQueryAsXmlString(Properties formProperties, InputStream xslIs
			)
		{
			// TODO: Suppress XML header with encoding (as Strings have no encoding)
			StringWriter writer = new StringWriter();
			StreamResult result = new StreamResult(writer);
			TransformCriteria(formProperties, xslIs, result);
			return writer.ToString();
		}

		/// <summary>Fast means of constructing query using a cached,precompiled stylesheet</summary>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public static Document GetQueryAsDOM(Properties formProperties, Templates template
			)
		{
			DOMResult result = new DOMResult();
			TransformCriteria(formProperties, template, result);
			return (Document)result.GetNode();
		}

		/// <summary>Slow means of constructing query - parses stylesheet from input stream</summary>
		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public static Document GetQueryAsDOM(Properties formProperties, InputStream xslIs
			)
		{
			DOMResult result = new DOMResult();
			TransformCriteria(formProperties, xslIs, result);
			return (Document)result.GetNode();
		}

		/// <summary>Slower transformation using an uncompiled stylesheet (suitable for development environment)
		/// 	</summary>
		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public static void TransformCriteria(Properties formProperties, InputStream xslIs
			, Result result)
		{
			dbf.SetNamespaceAware(true);
			DocumentBuilder builder = dbf.NewDocumentBuilder();
			Document xslDoc = builder.Parse(xslIs);
			DOMSource ds = new DOMSource(xslDoc);
			Transformer transformer = null;
			lock (tFactory)
			{
				transformer = tFactory.NewTransformer(ds);
			}
			TransformCriteria(formProperties, transformer, result);
		}

		/// <summary>Fast transformation using a pre-compiled stylesheet (suitable for production environments)
		/// 	</summary>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public static void TransformCriteria(Properties formProperties, Templates template
			, Result result)
		{
			TransformCriteria(formProperties, template.NewTransformer(), result);
		}

		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerException"></exception>
		public static void TransformCriteria(Properties formProperties, Transformer transformer
			, Result result)
		{
			dbf.SetNamespaceAware(true);
			//Create an XML document representing the search index document.
			DocumentBuilder db = dbf.NewDocumentBuilder();
			Document doc = db.NewDocument();
			Element root = doc.CreateElement("Document");
			doc.AppendChild(root);
			IEnumeration keysEnum = formProperties.Keys;
			while (keysEnum.MoveNext())
			{
				string propName = (string)keysEnum.Current;
				string value = formProperties.GetProperty(propName);
				if ((value != null) && (value.Length > 0))
				{
					DOMUtils.InsertChild(root, propName, value);
				}
			}
			//Use XSLT to to transform into an XML query string using the  queryTemplate
			DOMSource xml = new DOMSource(doc);
			transformer.Transform(xml, result);
		}

		/// <summary>Parses a query stylesheet for repeated use</summary>
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Javax.Xml.Transform.TransformerConfigurationException"></exception>
		public static Templates GetTemplates(InputStream xslIs)
		{
			dbf.SetNamespaceAware(true);
			DocumentBuilder builder = dbf.NewDocumentBuilder();
			Document xslDoc = builder.Parse(xslIs);
			DOMSource ds = new DOMSource(xslDoc);
			return tFactory.NewTemplates(ds);
		}
	}
}
