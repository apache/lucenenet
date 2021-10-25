using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    /// This class illustrates how form input (such as from a web page or Swing gui) can be
    /// turned into Lucene queries using a choice of XSL templates for different styles of queries.
    /// 
    /// LUCENENET (.NET Standard 1.x):  This is not compiled this because .NET Standard 1.x
    /// does not support XSL Transform.
    /// </summary>
    public class TestQueryTemplateManager : LuceneTestCase
    {
        private CoreParser builder;
        private Analyzer analyzer;
        private IndexSearcher searcher;
        private IndexReader reader;
        private Directory dir;

        //A collection of documents' field values for use in our tests
        String[] docFieldValues =
            {
                "artist=Jeff Buckley \talbum=Grace \treleaseDate=1999 \tgenre=rock",
                "artist=Fugazi \talbum=Repeater \treleaseDate=1990 \tgenre=alternative",
                "artist=Fugazi \talbum=Red Medicine \treleaseDate=1995 \tgenre=alternative",
                "artist=Peeping Tom \talbum=Peeping Tom \treleaseDate=2006 \tgenre=rock",
                "artist=Red Snapper \talbum=Prince Blimey \treleaseDate=1996 \tgenre=electronic"
            };

        //A collection of example queries, consisting of name/value pairs representing form content plus
        // a choice of query style template to use in the test, with expected number of hits
        String[] queryForms =
            {
                "artist=Fugazi \texpectedMatches=2 \ttemplate=albumBooleanQuery",
                "artist=Fugazi \treleaseDate=1990 \texpectedMatches=1 \ttemplate=albumBooleanQuery",
                "artist=Buckley \tgenre=rock \texpectedMatches=1 \ttemplate=albumFilteredQuery",
                "artist=Buckley \tgenre=electronic \texpectedMatches=0 \ttemplate=albumFilteredQuery",
                "queryString=artist:buckly~ NOT genre:electronic \texpectedMatches=1 \ttemplate=albumLuceneClassicQuery"
            };

        [Test]
        public void TestFormTransforms()
        {
            //// Sun 1.5 suffers from http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6240963
            //if (Constants.JAVA_VENDOR.StartsWith("Sun", StringComparison.Ordinal) && Constants.JAVA_VERSION.StartsWith("1.5", StringComparison.Ordinal)) {
            //  String defLang = Locale.getDefault().getLanguage();
            //  assumeFalse("Sun JRE 1.5 suffers from http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6240963 under Turkish locale", defLang.equals("tr", StringComparison.Ordinal) || defLang.equals("az", StringComparison.Ordinal));
            //}
            //Cache all the query templates we will be referring to.
            QueryTemplateManager qtm = new QueryTemplateManager();
            using (var stream = GetType().getResourceAsStream("albumBooleanQuery.xsl"))
            {
                qtm.AddQueryTemplate("albumBooleanQuery", stream);
            }
            using (var stream = GetType().getResourceAsStream("albumFilteredQuery.xsl"))
            {
                qtm.AddQueryTemplate("albumFilteredQuery", stream);
            }
            using (var stream = GetType().getResourceAsStream("albumLuceneClassicQuery.xsl"))
            {
                qtm.AddQueryTemplate("albumLuceneClassicQuery", stream);
            }

            //Run all of our test queries
            foreach (String queryForm in queryForms)
            {
                IDictionary<string, string> queryFormProperties = getPropsFromString(queryForm);

                //Get the required query XSL template for this test
                //      Templates template=getTemplate(queryFormProperties.getProperty("template"));

                //Transform the queryFormProperties into a Lucene XML query
                XmlDocument doc = qtm.GetQueryAsDOM(queryFormProperties, queryFormProperties["template"]);

                //Parse the XML query using the XML parser
                Query q = builder.GetQuery(doc.DocumentElement);

                //Run the query
                int h = searcher.Search(q, null, 1000).TotalHits;

                //Check we have the expected number of results
                int expectedHits = int.Parse(queryFormProperties["expectedMatches"]);
                assertEquals("Number of results should match for query " + queryForm, expectedHits, h);

            }
        }

        //Helper method to construct Lucene query forms used in our test
        IDictionary<string, string> getPropsFromString(String nameValuePairs)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            StringTokenizer st = new StringTokenizer(nameValuePairs, "\t=");
            while (st.MoveNext())
            {
                String name = st.Current.Trim();
                if (st.MoveNext())
                {
                    String value = st.Current.Trim();
                    result[name] = value;
                }
            }
            return result;
        }

        //Helper method to construct Lucene documents used in our tests
        Document getDocumentFromString(String nameValuePairs)
        {
            Document result = new Document();
            StringTokenizer st = new StringTokenizer(nameValuePairs, "\t=");
            while (st.MoveNext())
            {
                String name = st.Current.Trim();
                if (st.MoveNext())
                {
                    String value = st.Current.Trim();
                    result.Add(NewTextField(name, value, Field.Store.YES));
                }
            }
            return result;
        }

        /*
          * @see TestCase#setUp()
          */

        public override void SetUp()
        {
            base.SetUp();

            analyzer = new MockAnalyzer(Random);
            //Create an index
            dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            foreach (String docFieldValue in docFieldValues)
            {
                w.AddDocument(getDocumentFromString(docFieldValue));
            }
            w.ForceMerge(1);
            w.Dispose();
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);

            //initialize the parser
            builder = new CorePlusExtensionsParser("artist", analyzer);

        }


        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }
    }
}