// LUCENENET TODO: Use HTML Agility pack instead of SAX ?

using J2N.Collections.Generic.Extensions;
using Sax;
using Sax.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Simple HTML Parser extracting title, meta tags, and body text
    /// that is based on <a href="http://nekohtml.sourceforge.net/">NekoHTML</a>.
    /// </summary>
    public class DemoHTMLParser : IHTMLParser
    {
        /// <summary>The actual parser to read HTML documents.</summary>
        public sealed class Parser
        {
            private readonly IDictionary<string, string> metaTags = new Dictionary<string, string>();
            private readonly string title, body;

            // LUCENENET specific - expose field through property
            public IDictionary<string, string> MetaTags => metaTags;

            // LUCENENET specific - expose field through property
            public string Title => title;

            // LUCENENET specific - expose field through property
            public string Body => body;

            public Parser(TextReader reader)
                : this(new InputSource(reader))
            {
            }

            public Parser(InputSource source)
            {
                TagSoup.Parser parser = new TagSoup.Parser();

                parser.SetFeature(TagSoup.Parser.NAMESPACES_FEATURE, true);

                StringBuilder title = new StringBuilder(), body = new StringBuilder();
                DefaultHandler handler = new DefaultHandlerAnonymousClass(this, title, body);

                parser.ContentHandler = handler;
                parser.ErrorHandler = handler;
                parser.Parse(source);

                // the javacc-based parser trimmed title (which should be done for HTML in all cases):
                this.title = title.ToString().Trim();

                // assign body text
                this.body = body.ToString();
            }

            private sealed class DefaultHandlerAnonymousClass : DefaultHandler
            {
                private int inBODY = 0, inHEAD = 0, inTITLE = 0, suppressed = 0;

                private readonly Parser outerInstance;
                private readonly StringBuilder title;
                private readonly StringBuilder body;

                public DefaultHandlerAnonymousClass(Parser outerInstance, StringBuilder title, StringBuilder body)
                {
                    this.outerInstance = outerInstance;
                    this.title = title;
                    this.body = body;
                }

                public override void StartElement(string uri, string localName, string qName, IAttributes atts)
                {
                    if (inHEAD > 0)
                    {
                        if ("title".Equals(localName, StringComparison.OrdinalIgnoreCase))
                        {
                            inTITLE++;
                        }
                        else
                        {
                            if ("meta".Equals(localName, StringComparison.OrdinalIgnoreCase))
                            {
                                string name = atts.GetValue("name");
                                if (name is null)
                                {
                                    name = atts.GetValue("http-equiv");
                                }
                                string val = atts.GetValue("content");
                                if (name != null && val != null)
                                {
                                    outerInstance.metaTags[name.ToLowerInvariant()] = val;
                                }
                            }
                        }
                    }
                    else if (inBODY > 0)
                    {
                        if (SUPPRESS_ELEMENTS.Contains(localName))
                        {
                            suppressed++;
                        }
                        else if ("img".Equals(localName, StringComparison.OrdinalIgnoreCase))
                        {
                            // the original javacc-based parser preserved <IMG alt="..."/>
                            // attribute as body text in [] parenthesis:
                            string alt = atts.GetValue("alt");
                            if (alt != null)
                            {
                                body.Append('[').Append(alt).Append(']');
                            }
                        }
                    }
                    else if ("body".Equals(localName, StringComparison.OrdinalIgnoreCase))
                    {
                        inBODY++;
                    }
                    else if ("head".Equals(localName, StringComparison.OrdinalIgnoreCase))
                    {
                        inHEAD++;
                    }
                    else if ("frameset".Equals(localName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SAXException("This parser does not support HTML framesets.");
                    }
                }

                public override void EndElement(string uri, string localName, string qName)
                {
                    if (inBODY > 0)
                    {
                        if ("body".Equals(localName, StringComparison.OrdinalIgnoreCase))
                        {
                            inBODY--;
                        }
                        else if (ENDLINE_ELEMENTS.Contains(localName))
                        {
                            body.Append('\n');
                        }
                        else if (SUPPRESS_ELEMENTS.Contains(localName))
                        {
                            suppressed--;
                        }
                    }
                    else if (inHEAD > 0)
                    {
                        if ("head".Equals(localName, StringComparison.OrdinalIgnoreCase))
                        {
                            inHEAD--;
                        }
                        else if (inTITLE > 0 && "title".Equals(localName, StringComparison.OrdinalIgnoreCase))
                        {
                            inTITLE--;
                        }
                    }
                }

                public override void Characters(char[] ch, int start, int length)
                {
                    if (inBODY > 0 && suppressed == 0)
                    {
                        body.Append(ch, start, length);
                    }
                    else if (inTITLE > 0)
                    {
                        title.Append(ch, start, length);
                    }
                }

                public override InputSource ResolveEntity(string publicId, string systemId)
                {
                    // disable network access caused by DTDs
                    return new InputSource(new StringReader(""));
                }
            }

            private static ISet<string> CreateElementNameSet(params string[] names)
            {
                return new JCG.HashSet<string>(names).AsReadOnly();
            }

            /// <summary>HTML elements that cause a line break (they are block-elements).</summary>
            internal static readonly ISet<string> ENDLINE_ELEMENTS = CreateElementNameSet(
                "p", "h1", "h2", "h3", "h4", "h5", "h6", "div", "ul", "ol", "dl",
                "pre", "hr", "blockquote", "address", "fieldset", "table", "form",
                "noscript", "li", "dt", "dd", "noframes", "br", "tr", "select", "option"
            );

            /// <summary>HTML elements with contents that are ignored.</summary>
            internal static readonly ISet<string> SUPPRESS_ELEMENTS = CreateElementNameSet(
                "style", "script"
            );
        }
        public virtual DocData Parse(DocData docData, string name, DateTime? date, TextReader reader, TrecContentSource trecSrc)
        {
            try
            {
                return Parse(docData, name, date, new InputSource(reader), trecSrc);
            }
            catch (SAXException saxe)
            {
                throw new IOException("SAX exception occurred while parsing HTML document.", saxe);
            }
        }

        public virtual DocData Parse(DocData docData, string name, DateTime? date, InputSource source, TrecContentSource trecSrc)
        {
            Parser p = new Parser(source);

            // properties 
            IDictionary<string, string> props = p.MetaTags;
            if (props.TryGetValue("date", out string dateStr) && dateStr != null)
            {
                DateTime? newDate = trecSrc.ParseDate(dateStr);
                if (newDate != null)
                {
                    date = newDate;
                }
            }

            docData.Clear();
            docData.Name = name;
            docData.Body = p.Body;
            docData.Title = p.Title;
            docData.Props = props;
            docData.SetDate(date);
            return docData;
        }
    }
}
