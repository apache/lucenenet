// Lucene version compatibility level 4.8.1 (added fix for SOLR-5983: HTMLStripCharFilter is treating CDATA sections incorrectly)
using J2N.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.CharFilters
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

    public class HTMLStripCharFilterTest : BaseTokenStreamTestCase
    {

        private static Analyzer NewTestAnalyzer()
        {
            return Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }, initReader: (fieldName, reader) =>
            {
                return new HTMLStripCharFilter(reader);
            });
        }

        //this is some text  here is a  link  and another  link . This is an entity: & plus a <.  Here is an &
        //
        [Test]
        public virtual void Test()
        {
            string html = "<div class=\"foo\">this is some text</div> here is a <a href=\"#bar\">link</a> and " + 
                "another <a href=\"http://lucene.apache.org/\">link</a>. " + 
                "This is an entity: &amp; plus a &lt;.  Here is an &. <!-- is a comment -->";
            string gold = "\nthis is some text\n here is a link and " + 
                "another link. " + 
                "This is an entity: & plus a <.  Here is an &. ";
            AssertHTMLStripsTo(html, gold, null);
        }

        //Some sanity checks, but not a full-fledged check
        [Test]
        public virtual void TestHTML()
        {
            System.IO.Stream stream = this.GetType().getResourceAsStream("htmlStripReaderTest.html");
            HTMLStripCharFilter reader = new HTMLStripCharFilter(new System.IO.StreamReader(stream, Encoding.UTF8));
            StringBuilder builder = new StringBuilder();
            int ch = -1;
            while ((ch = reader.Read()) > 0)
            {
                builder.Append((char)ch);
            }
            string str = builder.ToString();
            assertTrue("Entity not properly escaped", str.IndexOf("&lt;", StringComparison.Ordinal) == -1); //there is one > in the text
            assertTrue("Forrest should have been stripped out", str.IndexOf("forrest", StringComparison.Ordinal) == -1 && str.IndexOf("Forrest", StringComparison.Ordinal) == -1);
            assertTrue("File should start with 'Welcome to Solr' after trimming", str.Trim().StartsWith("Welcome to Solr", StringComparison.Ordinal));

            assertTrue("File should start with 'Foundation.' after trimming", str.Trim().EndsWith("Foundation.", StringComparison.Ordinal));

        }

        [Test]
        public virtual void TestMSWord14GeneratedHTML()
        {
            System.IO.Stream stream = this.GetType().getResourceAsStream("MS-Word14generated.htm");
            HTMLStripCharFilter reader = new HTMLStripCharFilter(new System.IO.StreamReader(stream, Encoding.UTF8));
            string gold = "This is a test";
            StringBuilder builder = new StringBuilder();
            int ch = 0;
            while ((ch = reader.Read()) > 0)
            {
                builder.Append((char)ch);
            }
            // Compare trim()'d output to gold
            assertEquals("'" + builder.ToString().Trim() + "' is not equal to '" + gold + "'", 
                gold, builder.ToString().Trim());
        }

        [Test]
        public virtual void TestGamma()
        {
            AssertHTMLStripsTo("&Gamma;", "\u0393", new JCG.HashSet<string> { "reserved" });
        }

        [Test]
        public virtual void TestEntities()
        {
            string test = "&nbsp; &lt;foo&gt; &Uuml;bermensch &#61; &Gamma; bar &#x393;";
            string gold = "  <foo> \u00DCbermensch = \u0393 bar \u0393";
            AssertHTMLStripsTo(test, gold, new JCG.HashSet<string> { "reserved" });
        }

        [Test]
        public virtual void TestMoreEntities()
        {
            string test = "&nbsp; &lt;junk/&gt; &nbsp; &#33; &#64; and &#8217;";
            string gold = "  <junk/>   ! @ and ’";
            AssertHTMLStripsTo(test, gold, new JCG.HashSet<string> { "reserved" });
        }

        [Test]
        public virtual void TestReserved()
        {
            string test = "aaa bbb <reserved ccc=\"ddddd\"> eeee </reserved> ffff <reserved ggg=\"hhhh\"/> <other/>";
            ISet<string> set = new JCG.HashSet<string>();
            set.Add("reserved");
            TextReader reader = new HTMLStripCharFilter(new StringReader(test), set);
            StringBuilder builder = new StringBuilder();
            int ch = 0;
            while ((ch = reader.Read()) > 0)
            {
                builder.Append((char)ch);
            }
            string result = builder.ToString();
            // System.out.println("Result: " + result);
            assertTrue("Escaped tag not preserved: " + result.IndexOf("reserved", StringComparison.Ordinal), result.IndexOf("reserved", StringComparison.Ordinal) == 9);
            assertTrue("Escaped tag not preserved: " + result.IndexOf("reserved", 15, StringComparison.Ordinal), result.IndexOf("reserved", 15, StringComparison.Ordinal) == 38);
            assertTrue("Escaped tag not preserved: " + result.IndexOf("reserved", 41, StringComparison.Ordinal), result.IndexOf("reserved", 41, StringComparison.Ordinal) == 54);
            assertTrue("Other tag should be removed", result.IndexOf("other", StringComparison.Ordinal) == -1);
        }

        [Test]
        public virtual void TestMalformedHTML()
        {
            string[] testGold = {
                "a <a hr<ef=aa<a>> </close</a>",
                "a <a hr<ef=aa> </close",

                "<a href=http://dmoz.org/cgi-bin/add.cgi?where=/arts/\" class=lu style=\"font-size: 9px\" target=dmoz>Submit a Site</a>",
                "Submit a Site",

                "<a href=javascript:ioSwitch('p8','http://www.csmonitor.com/') title=expand id=e8 class=expanded rel=http://www.csmonitor.com/>Christian Science",
                "Christian Science",

                "<link rel=\"alternate\" type=\"application/rss+xml\" title=\"San Francisco \" 2008 RSS Feed\" href=\"http://2008.sf.wordcamp.org/feed/\" />",
                "\n",

                "<a href=\" http://www.surgery4was.happyhost.org/video-of-arthroscopic-knee-surgery symptoms.html, heat congestive heart failure <a href=\" http://www.symptoms1bad.happyhost.org/canine",
                "<a href=\" http://www.surgery4was.happyhost.org/video-of-arthroscopic-knee-surgery symptoms.html, heat congestive heart failure <a href=\" http://www.symptoms1bad.happyhost.org/canine",

                "<a href=\"http://ucblibraries.colorado.edu/how/index.htm\"class=\"pageNavAreaText\">",
                "",

                "<link title=\"^\\\" 21Sta's Blog\" rel=\"search\"  type=\"application/opensearchdescription+xml\"  href=\"http://21sta.com/blog/inc/opensearch.php\" />",
                "\n",

                "<a href=\"#postcomment\" title=\"\"Leave a comment\";\">?",
                "?",

                "<a href='/modern-furniture'   ' id='21txt' class='offtab'   onMouseout=\"this.className='offtab';  return true;\" onMouseover=\"this.className='ontab';  return true;\">",
                "",

                "<a href='http://alievi.wordpress.com/category/01-todos-posts/' style='font-size: 275%; padding: 1px; margin: 1px;' title='01 - Todos Post's (83)'>",
                "",

                "The <a href=<a href=\"http://www.advancedmd.com>medical\">http://www.advancedmd.com>medical</a> practice software</a>",
                "The <a href=medical\">http://www.advancedmd.com>medical practice software",

                "<a href=\"node/21426\" class=\"clipTitle2\" title=\"Levi.com/BMX 2008 Clip of the Week 29 \"Morgan Wade Leftover Clips\"\">Levi.com/BMX 2008 Clip of the Week 29...",
                "Levi.com/BMX 2008 Clip of the Week 29...",

                "<a href=\"printer_friendly.php?branch=&year=&submit=go&screen=\";\">Printer Friendly",
                "Printer Friendly",

                "<a href=#\" ondragstart=\"return false\" onclick=\"window.external.AddFavorite('http://www.amazingtextures.com', 'Amazing Textures');return false\" onmouseover=\"window.status='Add to Favorites';return true\">Add to Favorites",
                "Add to Favorites",

                "<a href=\"../at_home/at_home_search.html\"../_home/at_home_search.html\">At",
                "At",

                "E-mail: <a href=\"\"mailto:XXXXXX@example.com\" \">XXXXXX@example.com </a>",
                "E-mail: XXXXXX@example.com ",

                "<li class=\"farsi\"><a title=\"A'13?\" alt=\"A'13?\" href=\"http://www.america.gov/persian\" alt=\"\" name=\"A'13?\"A'13? title=\"A'13?\">A'13?</a></li>",
                "\nA'13?\n",

                "<li><a href=\"#28\" title=\"Hubert \"Geese\" Ausby\">Hubert \"Geese\" Ausby</a></li>",
                "\nHubert \"Geese\" Ausby\n",

                "<href=\"http://anbportal.com/mms/login.asp\">",
                "\n",

                "<a href=\"",
                "<a href=\"",

                "<a href=\">",
                "",

                "<a rel=\"nofollow\" href=\"http://anissanina31.skyrock.com/1895039493-Hi-tout-le-monde.html\" title=\" Hi, tout le monde !>#</a>",
                "#",

                "<a href=\"http://annunciharleydavidsonusate.myblog.it/\" title=\"Annunci Moto e Accessori Harley Davidson\" target=\"_blank\"><img src=\"http://annunciharleydavidsonusate.myblog.it/images/Antipixel.gif\" /></a>",
                "",

                "<a href=\"video/addvideo&v=120838887181\" onClick=\"return confirm('Are you sure you want  add this video to your profile? If it exists some video in your profile will be overlapped by this video!!')\" \" onmouseover=\"this.className='border2'\" onmouseout=\"this.className=''\">",
                "",

                "<a href=#Services & Support>",
                "",

                "<input type=\"image\" src=\"http://apologyindex.com/ThemeFiles/83401-72905/images/btn_search.gif\"value=\"Search\" name=\"Search\" alt=\"Search\" class=\"searchimage\" onclick=\"incom ='&sc=' + document.getElementById('sel').value ; var dt ='&dt=' + document.getElementById('dt').value; var searchKeyword = document.getElementById('q').value ; searchKeyword = searchKeyword.replace(/\\s/g,''); if (searchKeyword.length < 3){alert('Nothing to search. Search keyword should contain atleast 3 chars.'); return false; } var al='&al=' +  document.getElementById('advancedlink').style.display ;  document.location.href='http://apologyindex.com/search.aspx?q=' + document.getElementById('q').value + incom + dt + al;\" />",
                "",

                "<input type=\"image\" src=\"images/afbe.gif\" width=\"22\" height=\"22\"  hspace=\"4\" title=\"Add to Favorite\" alt=\"Add to Favorite\"onClick=\" if(window.sidebar){ window.sidebar.addPanel(document.title,location.href,''); }else if(window.external){ window.external.AddFavorite(location.href,document.title); }else if(window.opera&&window.print) { return true; }\">",
                "",

                "<area shape=\"rect\" coords=\"12,153,115,305\" href=\"http://statenislandtalk.com/v-web/gallery/Osmundsen-family\"Art's Norwegian Roots in Rogaland\">",
                "\n",

                "<a rel=\"nofollow\" href=\"http://arth26.skyrock.com/660188240-bonzai.html\" title=\"bonza>#",
                "#",

                "<a href=  >",
                "",

                "<ahref=http:..",
                "<ahref=http:..",

                "<ahref=http:..>",
                "\n",

                "<ahref=\"http://aseigo.bddf.ca/cms/1025\">A",
                "\nA",

                "<a href=\"javascript:calendar_window=window.open('/calendar.aspx?formname=frmCalendar.txtDate','calendar_window','width=154,height=188');calendar_window.focus()\">",
                "",

                "<a href=\"/applications/defenseaerospace/19+rackmounts\" title=\"19\" Rackmounts\">",
                "",

                "<a href=http://www.azimprimerie.fr/flash/backup/lewes-zip-code/savage-model-110-manual.html title=savage model 110 manual rel=dofollow>",
                "",

                "<a class=\"at\" name=\"Lamborghini  href=\"http://lamborghini.coolbegin.com\">Lamborghini /a>",
                "Lamborghini /a>",

                "<A href='newslink.php?news_link=http%3A%2F%2Fwww.worldnetdaily.com%2Findex.php%3Ffa%3DPAGE.view%26pageId%3D85729&news_title=Florida QB makes 'John 3:16' hottest Google search Tebow inscribed Bible reference on eye black for championship game' TARGET=_blank>",
                "",

                "<a href=/myspace !style='color:#993333'>",
                "",

                "<meta name=3DProgId content=3DExcel.Sheet>",
                "\n",

                "<link id=3D\"shLink\" href=3D\"PSABrKelly-BADMINTONCupResults08FINAL2008_09_19=_files/sheet004.htm\">",
                "\n",

                "<td bgcolor=3D\"#FFFFFF\" nowrap>",
                "\n",

                "<a href=\"http://basnect.info/usersearch/\"predicciones-mundiales-2009\".html\">\"predicciones mundiales 2009\"</a>",
                "\"predicciones mundiales 2009\"",

                "<a class=\"comment-link\" href=\"https://www.blogger.com/comment.g?blogID=19402125&postID=114070605958684588\"location.href=https://www.blogger.com/comment.g?blogID=19402125&postID=114070605958684588;>",
                "",

                "<a href = \"/videos/Bishop\"/\" title = \"click to see more Bishop\" videos\">Bishop\"</a>",
                "Bishop\"",

                "<a href=\"http://bhaa.ie/calendar/event.php?eid=20081203150127531\"\">BHAA Eircom 2 &amp; 5 miles CC combined start</a>",
                "BHAA Eircom 2 & 5 miles CC combined start",

                "<a href=\"http://people.tribe.net/wolfmana\" onClick='setClick(\"Application[tribe].Person[bb7df210-9dc0-478c-917f-436b896bcb79]\")'\" title=\"Mana\">",
                "",

                "<a  href=\"http://blog.edu-cyberpg.com/ct.ashx?id=6143c528-080c-4bb2-b765-5ec56c8256d3&url=http%3a%2f%2fwww.gsa.ac.uk%2fmackintoshsketchbook%2f\"\" eudora=\"autourl\">",
                "",

                "<input type=\"text\" value=\"<search here>\">",
                "<input type=\"text\" value=\"\n\">",

                "<input type=\"text\" value=\"<search here\">",
                "<input type=\"text\" value=\"\n",

                "<input type=\"text\" value=\"search here>\">",
                "\">",

                "<input type=\"text\" value=\"&lt;search here&gt;\" onFocus=\"this.value='<search here>'\">",
                "",

                "<![if ! IE]>\n<link href=\"http://i.deviantart.com/icons/favicon.png\" rel=\"shortcut icon\"/>\n<![endif]>",
                "\n\n\n",

                "<![if supportMisalignedColumns]>\n<tr height=0 style='display:none'>\n<td width=64 style='width:48pt'></td>\n</tr>\n<![endif]>",
                "\n\n\n\n\n\n\n\n"
            };
            for (int i = 0; i < testGold.Length; i += 2)
            {
                AssertHTMLStripsTo(testGold[i], testGold[i + 1], null);
            }
        }

        [Test]
        public virtual void TestBufferOverflow()
        {
            StringBuilder testBuilder = new StringBuilder(HTMLStripCharFilter.InitialBufferSize + 50);
            testBuilder.Append("ah<?> ??????");
            AppendChars(testBuilder, HTMLStripCharFilter.InitialBufferSize + 500);
            TextReader reader = new HTMLStripCharFilter(new StreamReader(new MemoryStream(testBuilder.ToString().GetBytes(Encoding.UTF8))));
            AssertHTMLStripsTo(reader, testBuilder.ToString(), null);

            testBuilder.Length = 0;
            testBuilder.Append("<!--"); //comments
            AppendChars(testBuilder, 3 * HTMLStripCharFilter.InitialBufferSize + 500); //comments have two lookaheads

            testBuilder.Append("-->foo");
            string gold = "foo";
            AssertHTMLStripsTo(testBuilder.ToString(), gold, null);

            testBuilder.Length = 0;
            testBuilder.Append("<?");
            AppendChars(testBuilder, HTMLStripCharFilter.InitialBufferSize + 500);
            testBuilder.Append("?>");
            gold = "";
            AssertHTMLStripsTo(testBuilder.ToString(), gold, null);

            testBuilder.Length = 0;
            testBuilder.Append("<b ");
            AppendChars(testBuilder, HTMLStripCharFilter.InitialBufferSize + 500);
            testBuilder.Append("/>");
            gold = "";
            AssertHTMLStripsTo(testBuilder.ToString(), gold, null);
        }

        private void AppendChars(StringBuilder testBuilder, int numChars)
        {
            int i1 = numChars / 2;
            for (int i = 0; i < i1; i++)
            {
                testBuilder.Append('a').Append(' '); //tack on enough to go beyond the mark readahead limit, since <?> makes HTMLStripCharFilter think it is a processing instruction
            }
        }

        [Test]
        public virtual void TestComment()
        {
            string test = "<!--- three dashes, still a valid comment ---> ";
            string gold = " ";
            AssertHTMLStripsTo(test, gold, null);

            test = "<! -- blah > "; // should not be recognized as a comment
            gold = " ";
            AssertHTMLStripsTo(test, gold, null);

            StringBuilder testBuilder = new StringBuilder("<!--");
            AppendChars(testBuilder, TestUtil.NextInt32(Random, 0, 1000));
            gold = "";
            AssertHTMLStripsTo(testBuilder.ToString(), gold, null);
        }

        public virtual void DoTestOffsets(string @in)
        {
            HTMLStripCharFilter reader = new HTMLStripCharFilter(new StreamReader(new MemoryStream(@in.GetBytes(Encoding.UTF8))));
            int ch = 0;
            int off = 0; // offset in the reader
            int strOff = -1; // offset in the original string
            while ((ch = reader.Read()) > 0)
            {
                int correctedOff = reader.CorrectOffset(off);

                if (ch == 'X')
                {
                    strOff = @in.IndexOf('X', strOff + 1);
                    assertEquals(strOff, correctedOff);
                }

                off++;
            }
        }

        [Test]
        public virtual void TestOffsets()
        {
            //    DoTestOffsets("hello X how X are you");
            DoTestOffsets("hello <p> X<p> how <p>X are you");
            DoTestOffsets("X &amp; X &#40; X &lt; &gt; X");

            // test backtracking
            DoTestOffsets("X < &zz >X &# < X > < &l > &g < X");
        }

        internal static void AssertLegalOffsets(string @in)
        {
            int length = @in.Length;
            HTMLStripCharFilter reader = new HTMLStripCharFilter(new StreamReader(new MemoryStream(@in.GetBytes(Encoding.UTF8))));
            int ch = 0;
            int off = 0;
            while ((ch = reader.Read()) > 0)
            {
                int correction = reader.CorrectOffset(off);
                assertTrue("invalid offset correction: " + off + "->" + correction + " for doc of length: " + length, 
                    correction <= length);
                off++;
            }
        }

        [Test]
        public virtual void TestLegalOffsets()
        {
            AssertLegalOffsets("hello world");
            AssertLegalOffsets("hello &#x world");
        }

        [Test]
        public virtual void TestRandom()
        {
            int numRounds = RandomMultiplier * 1000;
            CheckRandomData(Random, NewTestAnalyzer(), numRounds);
        }

        [Test]
        public virtual void TestRandomHugeStrings()
        {
            int numRounds = RandomMultiplier * 100;
            CheckRandomData(Random, NewTestAnalyzer(), numRounds, 8192);
        }

        [Test]
        public virtual void TestCloseBR()
        {
            CheckAnalysisConsistency(Random, NewTestAnalyzer(), Random.nextBoolean(), " Secretary)</br> [[M");
        }

        [Test]
        public virtual void TestServerSideIncludes()
        {
            string test = "one<img src=\"image.png\"\n" + 
                " alt =  \"Alt: <!--#echo var='${IMAGE_CAPTION:<!--comment-->\\'Comment\\'}'  -->\"\n\n" + 
                " title=\"Title: <!--#echo var=\"IMAGE_CAPTION\"-->\">two";
            string gold = "onetwo";
            AssertHTMLStripsTo(test, gold, null);

            test = "one<script><!-- <!--#config comment=\"<!-- \\\"comment\\\"-->\"--> --></script>two";
            gold = "one\ntwo";
            AssertHTMLStripsTo(test, gold, null);
        }

        [Test]
        public virtual void TestScriptQuotes()
        {
            string test = "one<script attr= bare><!-- action('<!-- comment -->', \"\\\"-->\\\"\"); --></script>two";
            string gold = "one\ntwo";
            AssertHTMLStripsTo(test, gold, null);

            test = "hello<script><!-- f('<!--internal--></script>'); --></script>";
            gold = "hello\n";
            AssertHTMLStripsTo(test, gold, null);
        }

        [Test]
        public virtual void TestEscapeScript()
        {
            string test = "one<script no-value-attr>callSomeMethod();</script>two";
            string gold = "one<script no-value-attr></script>two";
            ISet<string> escapedTags = new JCG.HashSet<string> { "SCRIPT" };
            AssertHTMLStripsTo(test, gold, escapedTags);
        }

        [Test]
        public virtual void TestStyle()
        {
            string test = "one<style type=\"text/css\">\n" + 
                "<!--\n" + 
                "@import url('http://www.lasletrasdecanciones.com/css.css');\n" + 
                "-->\n" + 
                "</style>two";
            string gold = "one\ntwo";
            AssertHTMLStripsTo(test, gold, null);
        }

        [Test]
        public virtual void TestEscapeStyle()
        {
            string test = "one<style type=\"text/css\"> body,font,a { font-family:arial; } </style>two";
            string gold = "one<style type=\"text/css\"></style>two";
            ISet<string> escapedTags = new JCG.HashSet<string> { "STYLE" };
            AssertHTMLStripsTo(test, gold, escapedTags);
        }

        [Test]
        public virtual void TestBR()
        {
            string[] testGold = {
                "one<BR />two<br>three",
                "one\ntwo\nthree",

                "one<BR some stuff here too>two</BR>",
                "one\ntwo\n"
            };
            for (int i = 0; i < testGold.Length; i += 2)
            {
                AssertHTMLStripsTo(testGold[i], testGold[i + 1], null);
            }
        }
        [Test]
        public virtual void TestEscapeBR()
        {
            string test = "one<BR class='whatever'>two</\nBR\n>";
            string gold = "one<BR class='whatever'>two</\nBR\n>";
            ISet<string> escapedTags = new JCG.HashSet<string> { "BR" };
            AssertHTMLStripsTo(test, gold, escapedTags);
        }

        [Test]
        public virtual void TestInlineTagsNoSpace()
        {
            string test = "one<sPAn class=\"invisible\">two<sup>2<sup>e</sup></sup>.</SpaN>three";
            string gold = "onetwo2e.three";
            AssertHTMLStripsTo(test, gold, null);
        }

        [Test]
        public virtual void TestCDATA()
        {
            int maxNumElems = 100;
            string randomHtmlishString1 // Don't create a comment (disallow "<!--") and don't include a closing ">"
                = TestUtil.RandomHtmlishString(Random, maxNumElems).Replace(">", " ").replaceFirst("^--", "__");
            string closedAngleBangNonCDATA = "<!" + randomHtmlishString1 + "-[CDATA[&]]>";

            string randomHtmlishString2 // Don't create a comment (disallow "<!--") and don't include a closing ">"
                = TestUtil.RandomHtmlishString(Random, maxNumElems).Replace(">", " ").replaceFirst("^--", "__");
            string unclosedAngleBangNonCDATA = "<!" + randomHtmlishString1 + "-[CDATA[";

            string[] testGold = {
                "one<![CDATA[<one><two>three<four></four></two></one>]]>two",
                "one<one><two>three<four></four></two></one>two",

                "one<![CDATA[two<![CDATA[three]]]]><![CDATA[>four]]>five",
                "onetwo<![CDATA[three]]>fourfive",

                "<! [CDATA[&]]>", "",
                "<! [CDATA[&] ] >", "",
                "<! [CDATA[&]]", "<! [CDATA[&]]", // unclosed angle bang - all input is output
                "<!\u2009[CDATA[&]]>", "",
                "<!\u2009[CDATA[&]\u2009]\u2009>", "",
                "<!\u2009[CDATA[&]\u2009]\u2009", "<!\u2009[CDATA[&]\u2009]\u2009", // unclosed angle bang - all input is output
                closedAngleBangNonCDATA, "",
                "<![CDATA[", "",
                "<![CDATA[<br>", "<br>",
                "<![CDATA[<br>]]", "<br>]]",
                "<![CDATA[<br>]]>", "<br>",
                "<![CDATA[<br>] ] >", "<br>] ] >",
                "<![CDATA[<br>]\u2009]\u2009>", "<br>]\u2009]\u2009>",
                "<!\u2009[CDATA[", "<!\u2009[CDATA[",
                unclosedAngleBangNonCDATA, unclosedAngleBangNonCDATA
            };
            for (int i = 0; i < testGold.Length; i += 2)
            {
                AssertHTMLStripsTo(testGold[i], testGold[i + 1], null);
            }
        }

        [Test]
        public virtual void TestUnclosedAngleBang()
        {
            AssertHTMLStripsTo("<![endif]", "<![endif]", null);
        }

        [Test]
        public virtual void TestUppercaseCharacterEntityVariants()
        {
            string test = " &QUOT;-&COPY;&GT;>&LT;<&REG;&AMP;";
            string gold = " \"-\u00A9>><<\u00AE&";
            AssertHTMLStripsTo(test, gold, null);
        }

        [Test]
        public virtual void TestMSWordMalformedProcessingInstruction()
        {
            string test = "one<?xml:namespace prefix = o ns = \"urn:schemas-microsoft-com:office:office\" />two";
            string gold = "onetwo";
            AssertHTMLStripsTo(test, gold, null);
        }

        [Test]
        public virtual void TestSupplementaryCharsInTags()
        {
            string test = "one<𩬅艱鍟䇹愯瀛>two<瀛愯𩬅>three 瀛愯𩬅</瀛愯𩬅>four</𩬅艱鍟䇹愯瀛>five<𠀀𠀀>six<𠀀𠀀/>seven";
            string gold = "one\ntwo\nthree 瀛愯𩬅\nfour\nfive\nsix\nseven";
            AssertHTMLStripsTo(test, gold, null);
        }

        [Test]
        public virtual void TestRandomBrokenHTML()
        {
            int maxNumElements = 10000;
            string text = TestUtil.RandomHtmlishString(Random, maxNumElements);
            CheckAnalysisConsistency(Random, NewTestAnalyzer(), Random.nextBoolean(), text);
        }

        [Test]
        public virtual void TestRandomText()
        {
            StringBuilder text = new StringBuilder();
            int minNumWords = 10;
            int maxNumWords = 10000;
            int minWordLength = 3;
            int maxWordLength = 20;
            int numWords = TestUtil.NextInt32(Random, minNumWords, maxNumWords);
            switch (TestUtil.NextInt32(Random, 0, 4))
            {
                case 0:
                    {
                        for (int wordNum = 0; wordNum < numWords; ++wordNum)
                        {
                            text.Append(TestUtil.RandomUnicodeString(Random, maxWordLength));
                            text.Append(' ');
                        }
                        break;
                    }
                case 1:
                    {
                        for (int wordNum = 0; wordNum < numWords; ++wordNum)
                        {
                            text.Append(TestUtil.RandomRealisticUnicodeString(Random, minWordLength, maxWordLength));
                            text.Append(' ');
                        }
                        break;
                    }
                default:
                    { // ASCII 50% of the time
                        for (int wordNum = 0; wordNum < numWords; ++wordNum)
                        {
                            text.Append(TestUtil.RandomSimpleString(Random));
                            text.Append(' ');
                        }
                    }
                    break;
            }
            TextReader reader = new HTMLStripCharFilter(new StringReader(text.ToString()));
            while (reader.Read() > 0) ;
        }

        [Test]
        public virtual void TestUTF16Surrogates()
        {
            Analyzer analyzer = NewTestAnalyzer();
            // Paired surrogates
            AssertAnalyzesTo(analyzer, " one two &#xD86C;&#XdC01;three", new string[] { "one", "two", "\uD86C\uDC01three" });
            AssertAnalyzesTo(analyzer, " &#55404;&#XdC01;", new string[] { "\uD86C\uDC01" });
            AssertAnalyzesTo(analyzer, " &#xD86C;&#56321;", new string[] { "\uD86C\uDC01" });
            AssertAnalyzesTo(analyzer, " &#55404;&#56321;", new string[] { "\uD86C\uDC01" });

            // Improperly paired surrogates
            AssertAnalyzesTo(analyzer, " &#55404;&#57999;", new string[] { "\uFFFD\uE28F" });
            AssertAnalyzesTo(analyzer, " &#xD86C;&#57999;", new string[] { "\uFFFD\uE28F" });
            AssertAnalyzesTo(analyzer, " &#55002;&#XdC01;", new string[] { "\uD6DA\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#55002;&#56321;", new string[] { "\uD6DA\uFFFD" });

            // Unpaired high surrogates
            AssertAnalyzesTo(analyzer, " &#Xd921;", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#Xd921", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#Xd921<br>", new string[] { "&#Xd921" });
            AssertAnalyzesTo(analyzer, " &#55528;", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#55528", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#55528<br>", new string[] { "&#55528" });

            // Unpaired low surrogates
            AssertAnalyzesTo(analyzer, " &#xdfdb;", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#xdfdb", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#xdfdb<br>", new string[] { "&#xdfdb" });
            AssertAnalyzesTo(analyzer, " &#57209;", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#57209", new string[] { "\uFFFD" });
            AssertAnalyzesTo(analyzer, " &#57209<br>", new string[] { "&#57209" });
        }


        public static void AssertHTMLStripsTo(string input, string gold, ISet<string> escapedTags)
        {
            AssertHTMLStripsTo(new StringReader(input), gold, escapedTags);
        }

        public static void AssertHTMLStripsTo(TextReader input, string gold, ISet<string> escapedTags)
        {
            HTMLStripCharFilter reader;
            if (null == escapedTags)
            {
                reader = new HTMLStripCharFilter(input);
            }
            else
            {
                reader = new HTMLStripCharFilter(input, escapedTags);
            }
            int ch = 0;
            StringBuilder builder = new StringBuilder();
            try
            {
                while ((ch = reader.Read()) > 0)
                {
                    builder.Append((char)ch);
                }
            }
            catch (Exception e) when (e.IsException())
            {
                if (gold.Equals(builder.ToString(), StringComparison.Ordinal))
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
                throw new Exception("('" + builder.ToString() + "' is not equal to '" + gold + "').  " + e.Message, e);
            }
            assertEquals("'" + builder.ToString() + "' is not equal to '" + gold + "'", gold, builder.ToString());
        }
    }
}