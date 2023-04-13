using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

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
    /// Provides utilities for turning query form input (such as from a web page or Swing gui) into
    /// Lucene XML queries by using XSL templates.  This approach offers a convenient way of externalizing
    /// and changing how user input is turned into Lucene queries.
    /// Database applications often adopt similar practices by externalizing SQL in template files that can
    /// be easily changed/optimized by a DBA.
    /// The static methods can be used on their own or by creating an instance of this class you can store and
    /// re-use compiled stylesheets for fast use (e.g. in a server environment)
    /// </summary>
    public class QueryTemplateManager
    {
        private readonly IDictionary<string, XslCompiledTransform> compiledTemplatesCache = new Dictionary<string, XslCompiledTransform>(); // LUCENENET: marked readonly
        private XslCompiledTransform defaultCompiledTemplates;

        public QueryTemplateManager()
        {
        }

        /// <summary>
        /// This class makes a virtual AddDefaultQueryTemplate call. If you need to subclass it
        /// and make this call at a time when it suits you, use <see cref="QueryTemplateManager()" /> instead
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        public QueryTemplateManager(Stream xslIs)
        {
            AddDefaultQueryTemplate(xslIs);
        }

        public virtual void AddDefaultQueryTemplate(Stream xslIs)
        {
            defaultCompiledTemplates = GetTemplates(xslIs);
        }

        public virtual void AddQueryTemplate(string name, Stream xslIs)
        {
            compiledTemplatesCache[name] = GetTemplates(xslIs);
        }

        public virtual string GetQueryAsXmlString(IDictionary<string, string> formProperties, string queryTemplateName)
        {
            XslCompiledTransform ts = compiledTemplatesCache[queryTemplateName];
            return GetQueryAsXmlString(formProperties, ts);
        }

        public virtual XmlDocument GetQueryAsDOM(IDictionary<string, string> formProperties, string queryTemplateName)
        {
            XslCompiledTransform ts = compiledTemplatesCache[queryTemplateName];
            return GetQueryAsDOM(formProperties, ts);
        }

        public virtual string GetQueryAsXmlString(IDictionary<string, string> formProperties)
        {
            return GetQueryAsXmlString(formProperties, defaultCompiledTemplates);
        }

        public virtual XmlDocument GetQueryAsDOM(IDictionary<string, string> formProperties)
        {
            return GetQueryAsDOM(formProperties, defaultCompiledTemplates);
        }

        /// <summary>
        /// Fast means of constructing query using a precompiled stylesheet
        /// </summary>
        public static string GetQueryAsXmlString(IDictionary<string, string> formProperties, XslCompiledTransform template)
        {
            // TODO: Suppress XML header with encoding (as Strings have no encoding)
            using var stream = new MemoryStream();
            TransformCriteria(formProperties, template, stream);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Slow means of constructing query parsing a stylesheet from an input stream
        /// </summary>
        public static string GetQueryAsXmlString(IDictionary<string, string> formProperties, Stream xslIs)
        {
            // TODO: Suppress XML header with encoding (as Strings have no encoding)
            using var stream = new MemoryStream();
            TransformCriteria(formProperties, xslIs, stream);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Fast means of constructing query using a cached,precompiled stylesheet
        /// </summary>
        public static XmlDocument GetQueryAsDOM(IDictionary<string, string> formProperties, XslCompiledTransform template)
        {
            XmlDocument result = new XmlDocument();
            using (var stream = new MemoryStream())
            {
                TransformCriteria(formProperties, template, stream);
                stream.Position = 0;
                result.Load(stream);
            }
            return result;
        }

        /// <summary>
        /// Slow means of constructing query - parses stylesheet from input stream
        /// </summary>
        public static XmlDocument GetQueryAsDOM(IDictionary<string, string> formProperties, Stream xslIs)
        {
            XmlDocument result = new XmlDocument();
            using (var stream = new MemoryStream())
            {
                TransformCriteria(formProperties, xslIs, stream);
                stream.Position = 0;
                result.Load(stream);
            }
            return result;
        }

        /// <summary>
        /// Slower transformation using an uncompiled stylesheet (suitable for development environment)
        /// </summary>
        public static void TransformCriteria(IDictionary<string, string> formProperties, Stream xslIs, Stream result)
        {
            XmlDocument xslDoc = new XmlDocument();
            xslDoc.Load(xslIs);

            XslCompiledTransform transformer = new XslCompiledTransform();
            transformer.Load(xslDoc);

            TransformCriteria(formProperties, transformer, result);
        }

        /// <summary>
        /// Fast transformation using a pre-compiled stylesheet (suitable for production environments)
        /// </summary>   
        public static void TransformCriteria(IDictionary<string, string> formProperties, XslCompiledTransform transformer, Stream result)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Document");
            doc.AppendChild(root);

            foreach (var prop in formProperties)
            {
                string propName = prop.Key;
                string value = prop.Value;
                if ((value != null) && (value.Length > 0))
                {
                    DOMUtils.InsertChild(root, propName, value);
                }
            }

            transformer.Transform(doc, null, result);
        }

        /// <summary>
        /// Parses a query stylesheet for repeated use
        /// </summary>
        public static XslCompiledTransform GetTemplates(Stream xslIs)
        {
            using var reader = XmlReader.Create(xslIs);
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(reader);
            return xslt;
        }
    }
}
