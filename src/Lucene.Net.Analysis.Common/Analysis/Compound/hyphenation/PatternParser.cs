using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Lucene.Net.Analysis.Compound.Hyphenation
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     * 
     *      http://www.apache.org/licenses/LICENSE-2.0
     * 
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A XMLReader document handler to read and parse hyphenation patterns from a XML
    /// file.
    /// 
    /// LUCENENET: This class has been refactored from its Java counterpart to use XmlReader rather
    /// than a SAX parser.
    /// </summary>
    public class PatternParser
    {
        internal int currElement;

        internal IPatternConsumer consumer;

        internal StringBuilder token;

        internal List<object> exception;

        internal char hyphenChar;

        internal string errMsg;

        internal const int ELEM_CLASSES = 1;

        internal const int ELEM_EXCEPTIONS = 2;

        internal const int ELEM_PATTERNS = 3;

        internal const int ELEM_HYPHEN = 4;

        public PatternParser()
        {
            token = new StringBuilder();
            hyphenChar = '-'; // default
        }

        public PatternParser(IPatternConsumer consumer) : this()
        {
            this.consumer = consumer;
        }

        public virtual IPatternConsumer Consumer
        {
            set
            {
                this.consumer = value;
            }
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="filename"> the filename </param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(string filename)
        {
            var xmlReaderSettings = GetXmlReaderSettings();

            // LUCENENET TODO: Create overloads that allow XmlReaderSettings to be passed in.
            using (var src = XmlReader.Create(filename, xmlReaderSettings))
            {
                Parse(src);
            }
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="file"> the pattern file </param>
        public virtual void Parse(FileInfo file)
        {
            Parse(file, Encoding.UTF8);
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="file"> the pattern file </param>
        public virtual void Parse(FileInfo file, Encoding encoding)
        {
            var xmlReaderSettings = GetXmlReaderSettings();

            using (var src = XmlReader.Create(new StreamReader(file.OpenRead(), encoding), xmlReaderSettings))
            {
                Parse(src);
            }
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="file"> the pattern file </param>
        public virtual void Parse(Stream xmlStream)
        {
            var xmlReaderSettings = GetXmlReaderSettings();

            using (var src = XmlReader.Create(xmlStream, xmlReaderSettings))
            {
                Parse(src);
            }
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="source"> the InputSource for the file </param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(XmlReader source)
        {
            source.MoveToContent();
            while (source.Read())
            {
                ParseNode(source);
            }
        }

        private void ParseNode(XmlReader node)
        {
            string uri, name, raw;
            switch (node.NodeType)
            {
                case XmlNodeType.Element:

                    // Element start
                    uri = node.NamespaceURI;
                    name = node.Name;
                    bool isEmptyElement = node.IsEmptyElement;
                    var attributes = GetAttributes(node);
                    raw = string.Empty; // node.ReadOuterXml(); - not used, but was messing with the node pointer

                    this.StartElement(uri, name, raw, attributes);
                    if (isEmptyElement)
                    {
                        this.EndElement(uri, name, raw);
                    }
                    break;

                case XmlNodeType.Text:

                    this.Characters(node.Value.ToCharArray(), 0, node.Value.Length);
                    break;

                case XmlNodeType.EndElement:
                    uri = node.NamespaceURI;
                    name = node.Name;
                    raw = string.Empty; // node.ReadOuterXml(); - not used, but was messing with the node pointer

                    // Element end
                    this.EndElement(uri, name, raw);
                    break;
            }
        }

        private XmlReaderSettings GetXmlReaderSettings()
        {
            return
#if !FEATURE_DTD_PROCESSING
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = new DtdResolver()
                }
#else
                new XmlReaderSettings();
#endif
        }

        private IDictionary<string, string> GetAttributes(XmlReader node)
        {
            var result = new Dictionary<string, string>();
            if (node.HasAttributes)
            {
                for (int i = 0; i < node.AttributeCount; i++)
                {
                    node.MoveToAttribute(i);
                    result.Add(node.Name, node.Value);
                }
            }

            return result;
        }

        protected internal virtual string ReadToken(StringBuilder chars)
        {
            string word;
            bool space = false;
            int i;
            for (i = 0; i < chars.Length; i++)
            {
                if (char.IsWhiteSpace(chars[i]))
                {
                    space = true;
                }
                else
                {
                    break;
                }
            }
            if (space)
            {
                // chars.delete(0,i);
                for (int countr = i; countr < chars.Length; countr++)
                {
                    chars[countr - i] = chars[countr];
                }
                chars.Length = chars.Length - i;
                if (token.Length > 0)
                {
                    word = token.ToString();
                    token.Length = 0;
                    return word;
                }
            }
            space = false;
            for (i = 0; i < chars.Length; i++)
            {
                if (char.IsWhiteSpace(chars[i]))
                {
                    space = true;
                    break;
                }
            }
            token.Append(chars.ToString(0, i - 0));
            // chars.delete(0,i);
            for (int countr = i; countr < chars.Length; countr++)
            {
                chars[countr - i] = chars[countr];
            }
            chars.Length = chars.Length - i;
            if (space)
            {
                word = token.ToString();
                token.Length = 0;
                return word;
            }
            token.Append(chars.ToString());
            return null;
        }

        protected internal static string GetPattern(string word)
        {
            StringBuilder pat = new StringBuilder();
            int len = word.Length;
            for (int i = 0; i < len; i++)
            {
                if (!char.IsDigit(word[i]))
                {
                    pat.Append(word[i]);
                }
            }
            return pat.ToString();
        }

        protected internal virtual List<object> NormalizeException<T1>(List<T1> ex)
        {
            List<object> res = new List<object>();
            for (int i = 0; i < ex.Count; i++)
            {
                object item = ex[i];
                if (item is string)
                {
                    string str = (string)item;
                    StringBuilder buf = new StringBuilder();
                    for (int j = 0; j < str.Length; j++)
                    {
                        char c = str[j];
                        if (c != hyphenChar)
                        {
                            buf.Append(c);
                        }
                        else
                        {
                            res.Add(buf.ToString());
                            buf.Length = 0;
                            char[] h = new char[1];
                            h[0] = hyphenChar;
                            // we use here hyphenChar which is not necessarily
                            // the one to be printed
                            res.Add(new Hyphen(new string(h), null, null));
                        }
                    }
                    if (buf.Length > 0)
                    {
                        res.Add(buf.ToString());
                    }
                }
                else
                {
                    res.Add(item);
                }
            }
            return res;
        }

        protected internal virtual string GetExceptionWord<T1>(List<T1> ex)
        {
            StringBuilder res = new StringBuilder();
            for (int i = 0; i < ex.Count; i++)
            {
                object item = ex[i];
                if (item is string)
                {
                    res.Append((string)item);
                }
                else
                {
                    if (((Hyphen)item).noBreak != null)
                    {
                        res.Append(((Hyphen)item).noBreak);
                    }
                }
            }
            return res.ToString();
        }

        protected internal static string GetInterletterValues(string pat)
        {
            StringBuilder il = new StringBuilder();
            string word = pat + "a"; // add dummy letter to serve as sentinel
            int len = word.Length;
            for (int i = 0; i < len; i++)
            {
                char c = word[i];
                if (char.IsDigit(c))
                {
                    il.Append(c);
                    i++;
                }
                else
                {
                    il.Append('0');
                }
            }
            return il.ToString();
        }

#if FEATURE_XML_RESOLVER
        /// <summary>
        /// LUCENENET specific helper class to force the DTD file to be read from the embedded resource
        /// rather than from the file system.
        /// </summary>
        internal class DtdResolver : XmlUrlResolver
        {
            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                string dtdFilename = "hyphenation.dtd";
                if (dtdFilename.Equals(absoluteUri.Segments.LastOrDefault(), StringComparison.OrdinalIgnoreCase))
                {
                    var qualifedDtdFilename = string.Concat(GetType().Namespace, ".", dtdFilename);
                    return GetType().Assembly.GetManifestResourceStream(qualifedDtdFilename);
                }

                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }
#endif

        //
        // ContentHandler methods
        //

        /// <seealso cref= org.xml.sax.ContentHandler#startElement(java.lang.String,
        ///      java.lang.String, java.lang.String, org.xml.sax.Attributes) </seealso>
        public void StartElement(string uri, string local, string raw, IDictionary<string, string> attrs)
        {
            if (local.Equals("hyphen-char"))
            {
                string h = attrs.ContainsKey("value") ? attrs["value"] : null;
                if (h != null && h.Length == 1)
                {
                    hyphenChar = h[0];
                }
            }
            else if (local.Equals("classes"))
            {
                currElement = ELEM_CLASSES;
            }
            else if (local.Equals("patterns"))
            {
                currElement = ELEM_PATTERNS;
            }
            else if (local.Equals("exceptions"))
            {
                currElement = ELEM_EXCEPTIONS;
                exception = new List<object>();
            }
            else if (local.Equals("hyphen"))
            {
                if (token.Length > 0)
                {
                    exception.Add(token.ToString());
                }
                exception.Add(new Hyphen(attrs["pre"], attrs["no"], attrs["post"]));
                currElement = ELEM_HYPHEN;
            }
            token.Length = 0;
        }

        /// <seealso cref= org.xml.sax.ContentHandler#endElement(java.lang.String,
        ///      java.lang.String, java.lang.String) </seealso>
        public void EndElement(string uri, string local, string raw)
        {
            if (token.Length > 0)
            {
                string word = token.ToString();
                switch (currElement)
                {
                    case ELEM_CLASSES:
                        consumer.AddClass(word);
                        break;
                    case ELEM_EXCEPTIONS:
                        exception.Add(word);
                        exception = NormalizeException(exception);
                        consumer.AddException(GetExceptionWord(exception), new List<object>(exception));
                        break;
                    case ELEM_PATTERNS:
                        consumer.AddPattern(GetPattern(word), GetInterletterValues(word));
                        break;
                    case ELEM_HYPHEN:
                        // nothing to do
                        break;
                }
                if (currElement != ELEM_HYPHEN)
                {
                    token.Length = 0;
                }
            }
            if (currElement == ELEM_HYPHEN)
            {
                currElement = ELEM_EXCEPTIONS;
            }
            else
            {
                currElement = 0;
            }
        }

        /// <seealso cref= org.xml.sax.ContentHandler#characters(char[], int, int) </seealso>
        public void Characters(char[] ch, int start, int length)
        {
            StringBuilder chars = new StringBuilder(length);
            chars.Append(ch, start, length);
            string word = ReadToken(chars);
            while (word != null)
            {
                // System.out.println("\"" + word + "\"");
                switch (currElement)
                {
                    case ELEM_CLASSES:
                        consumer.AddClass(word);
                        break;
                    case ELEM_EXCEPTIONS:
                        exception.Add(word);
                        exception = NormalizeException(exception);
                        consumer.AddException(GetExceptionWord(exception), new List<object>(exception));
                        exception.Clear();
                        break;
                    case ELEM_PATTERNS:
                        consumer.AddPattern(GetPattern(word), GetInterletterValues(word));
                        break;
                }
                word = ReadToken(chars);
            }
        }
    }
}