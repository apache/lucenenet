// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Support.IO
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

    [TestFixture]
    public class TestStreamTokenizer : LuceneTestCase
    {
        StringReader r;

        StreamTokenizer st;

        String testString;

        /**
         * @tests java.io.StreamTokenizer#StreamTokenizer(java.io.InputStream)
         */
        [Test]
        public void Test_ConstructorLSystem_IO_InputStream()
        {
#pragma warning disable 612, 618
            st = new StreamTokenizer(new MemoryStream(
#pragma warning restore 612, 618
                Encoding.UTF8.GetBytes("/comments\n d 8 'h'")));


            assertEquals("the next token returned should be the letter d",
                     StreamTokenizer.TT_WORD, st.NextToken());

            assertEquals("the next token returned should be the letter d",
                     "d", st.StringValue);


            assertEquals("the next token returned should be the digit 8",
                     StreamTokenizer.TT_NUMBER, st.NextToken());

            assertEquals("the next token returned should be the digit 8",
                     8.0, st.NumberValue);


            assertEquals("the next token returned should be the quote character",
                     39, st.NextToken());

            assertEquals("the next token returned should be the quote character",
                     "h", st.StringValue);
        }

        /**
         * @tests java.io.StreamTokenizer#StreamTokenizer(java.io.Reader)
         */
        [Test]
        public void Test_ConstructorLSystem_IO_TextReader()
        {
            setTest("/testing\n d 8 'h' ");
            assertEquals("the next token returned should be the letter d skipping the comments",
                     StreamTokenizer.TT_WORD, st.NextToken());
            assertEquals("the next token returned should be the letter d",
                     "d", st.StringValue);

            assertEquals("the next token returned should be the digit 8",
                     StreamTokenizer.TT_NUMBER, st.NextToken());
            assertEquals("the next token returned should be the digit 8",
                     8.0, st.NumberValue);

            assertEquals("the next token returned should be the quote character",
                     39, st.NextToken());
            assertEquals("the next token returned should be the quote character",
                     "h", st.StringValue);
        }

        /**
         * @tests java.io.StreamTokenizer#commentChar(int)
         */
        [Test]
        public void Test_commentCharI()
        {
            setTest("*comment \n / 8 'h' ");
            st.OrdinaryChar('/');
            st.CommentChar('*');
            assertEquals("nextToken() did not return the character / skiping the comments starting with *",
                     47, st.NextToken());
            assertTrue("the next token returned should be the digit 8", st
                   .NextToken() == StreamTokenizer.TT_NUMBER
                   && st.NumberValue == 8.0);
            assertTrue("the next token returned should be the quote character",
                   st.NextToken() == 39 && st.StringValue.equals("h"));
        }

        /**
         * @tests java.io.StreamTokenizer#eolIsSignificant(boolean)
         */
        [Test]
        public void Test_eolIsSignificantZ()
        {
            setTest("d 8\n");
            // by default end of line characters are not significant
            assertTrue("nextToken did not return d",
                   st.NextToken() == StreamTokenizer.TT_WORD
                   && st.StringValue.equals("d"));
            assertTrue("nextToken did not return 8",
                   st.NextToken() == StreamTokenizer.TT_NUMBER
                   && st.NumberValue == 8.0);
            assertTrue("nextToken should be the end of file",
                   st.NextToken() == StreamTokenizer.TT_EOF);
            setTest("d\n");
            st.IsEOLSignificant = (true);
            // end of line characters are significant
            assertTrue("nextToken did not return d",
                   st.NextToken() == StreamTokenizer.TT_WORD
                   && st.StringValue.equals("d"));
            assertTrue("nextToken is the end of line",
                   st.NextToken() == StreamTokenizer.TT_EOL);
        }

        /**
         * @tests java.io.StreamTokenizer#lineno()
         */
        [Test]
        public void Test_lineno()
        {
            setTest("d\n 8\n");
            assertEquals("the lineno should be 1", 1, st.LineNumber);
            st.NextToken();
            st.NextToken();
            assertEquals("the lineno should be 2", 2, st.LineNumber);
            st.NextToken();
            assertEquals("the next line no should be 3", 3, st.LineNumber);
        }

        /**
         * @tests java.io.StreamTokenizer#lowerCaseMode(boolean)
         */
        [Test]
        public void Test_lowerCaseModeZ()
        {
            // SM.
            setTest("HELLOWORLD");
            st.LowerCaseMode = (true);

            st.NextToken();
            assertEquals("sval not converted to lowercase.", "helloworld", st.StringValue
                     );
        }

        /**
         * @tests java.io.StreamTokenizer#nextToken()
         */
        [Test]
        public void Test_nextToken()
        {
            // SM.
            // LUCENENET NOTE: The original test had \257 (which is octal)
            // that is not supported in a .NET string, so we convert to decimal 175 here.
            // This also changes the semantics of the test, because for whatever
            // reason in Java it was expecting the octal number to register as a TT_WORD.
            // So, we changed to expect a TT_NUMBER as a result of the above change.
            // Also, we don't need to escape single quotes in .NET.
            setTest("\r\n/* fje fje 43.4 f \r\n f g */  456.459 \r\n"
                    + "Hello  / 	\r\n \r\n \n \r 175 Hi 'Hello World'");
            st.OrdinaryChar('/');
            st.SlashStarComments = true;
            st.NextToken();
            assertTrue("Wrong Token type1: " + (char)st.TokenType,
                   st.TokenType == StreamTokenizer.TT_NUMBER);
            st.NextToken();
            assertTrue("Wrong Token type2: " + st.TokenType,
                   st.TokenType == StreamTokenizer.TT_WORD);
            st.NextToken();
            assertTrue("Wrong Token type3: " + st.TokenType, st.TokenType == '/');
            st.NextToken();
            assertTrue("Wrong Token type4: " + st.TokenType,
                   st.TokenType == StreamTokenizer.TT_NUMBER);
            st.NextToken();
            assertTrue("Wrong Token type5: " + st.TokenType,
                   st.TokenType == StreamTokenizer.TT_WORD);
            st.NextToken();
            assertTrue("Wrong Token type6: " + st.TokenType, st.TokenType == '\'');
            assertTrue("Wrong Token type7: " + st.TokenType, st.StringValue
                   .equals("Hello World"));
            st.NextToken();
            assertTrue("Wrong Token type8: " + st.TokenType, st.TokenType == -1);

            using (var pin = new MemoryStream(Encoding.UTF8.GetBytes("hello\n\r\r")))
            {
#pragma warning disable 612, 618
                StreamTokenizer s = new StreamTokenizer(pin);
#pragma warning restore 612, 618
                s.IsEOLSignificant = (true);

                assertTrue("Wrong token 1,1",
                       s.NextToken() == StreamTokenizer.TT_WORD
                       && s.StringValue.equals("hello"));

                assertTrue("Wrong token 1,2", s.NextToken() == '\n');

                assertTrue("Wrong token 1,3", s.NextToken() == '\n');

                assertTrue("Wrong token 1,4", s.NextToken() == '\n');


                assertTrue("Wrong token 1,5",
                       s.NextToken() == StreamTokenizer.TT_EOF);
            }
            StreamTokenizer tokenizer = new StreamTokenizer(
                                    new StringReader("\n \r\n#"));
            tokenizer.OrdinaryChar('\n'); // make \n ordinary
            tokenizer.IsEOLSignificant = (true);

            assertTrue("Wrong token 2,1", tokenizer.NextToken() == '\n');

            assertTrue("Wrong token 2,2", tokenizer.NextToken() == '\n');

            assertEquals("Wrong token 2,3", '#', tokenizer.NextToken());
        }

        /**
         * @tests java.io.StreamTokenizer#ordinaryChar(int)
         */
        [Test]
        public void Test_ordinaryCharI()
        {
            // SM.
            setTest("Ffjein 893");
            st.OrdinaryChar('F');
            st.NextToken();
            assertTrue("OrdinaryChar failed." + (char)st.TokenType,
                       st.TokenType == 'F');
        }

        /**
         * @tests java.io.StreamTokenizer#ordinaryChars(int, int)
         */
        [Test]
        public void Test_ordinaryCharsII()
        {
            // SM.
            setTest("azbc iof z 893");
            st.OrdinaryChars('a', 'z');
            assertEquals("OrdinaryChars failed.", 'a', st.NextToken());
            assertEquals("OrdinaryChars failed.", 'z', st.NextToken());
        }

        /**
         * @tests java.io.StreamTokenizer#parseNumbers()
         */
        [Test]
        public void Test_parseNumbers()
        {
            // SM
            setTest("9.9 678");
            assertTrue("Base behavior failed.",
                       st.NextToken() == StreamTokenizer.TT_NUMBER);
            st.OrdinaryChars('0', '9');
            assertEquals("setOrdinary failed.", '6', st.NextToken());
            st.ParseNumbers();
            assertTrue("parseNumbers failed.",
                       st.NextToken() == StreamTokenizer.TT_NUMBER);
        }

        /**
         * @tests java.io.StreamTokenizer#pushBack()
         */
        [Test]
        public void Test_pushBack()
        {
            // SM.
            setTest("Hello 897");
            st.NextToken();
            st.PushBack();
            assertTrue("PushBack failed.",
                       st.NextToken() == StreamTokenizer.TT_WORD);
        }

        /**
         * @tests java.io.StreamTokenizer#quoteChar(int)
         */
        [Test]
        public void Test_quoteCharI()
        {
            // SM
            setTest("<Hello World<    HelloWorldH");
            st.QuoteChar('<');
            assertEquals("QuoteChar failed.", '<', st.NextToken());
            assertEquals("QuoteChar failed.", "Hello World", st.StringValue);
            st.QuoteChar('H');
            st.NextToken();
            assertEquals("QuoteChar failed for word.", "elloWorld", st.StringValue
                         );
        }

        /**
         * @tests java.io.StreamTokenizer#resetSyntax()
         */
        [Test]
        public void Test_resetSyntax()
        {
            // SM
            setTest("H 9\' ello World");
            st.ResetSyntax();
            assertTrue("resetSyntax failed1." + (char)st.TokenType,
                       st.NextToken() == 'H');
            assertTrue("resetSyntax failed1." + (char)st.TokenType,
                       st.NextToken() == ' ');
            assertTrue("resetSyntax failed2." + (char)st.TokenType,
                       st.NextToken() == '9');
            assertTrue("resetSyntax failed3." + (char)st.TokenType,
                       st.NextToken() == '\'');
        }

        /**
         * @tests java.io.StreamTokenizer#slashSlashComments(boolean)
         */
        [Test]
        public void Test_slashSlashCommentsZ()
        {
            // SM.
            setTest("// foo \r\n /fiji \r\n -456");
            st.OrdinaryChar('/');
            st.SlashSlashComments = (true);
            assertEquals("Test failed.", '/', st.NextToken());
            assertTrue("Test failed.",
                       st.NextToken() == StreamTokenizer.TT_WORD);
        }

        /**
         * @tests java.io.StreamTokenizer#slashSlashComments(boolean)
         */
        [Test]
        public void Test_slashSlashComments_withSSOpen()
        {
            TextReader reader = new StringReader("t // t t t");

            StreamTokenizer st = new StreamTokenizer(reader);
            st.SlashSlashComments = (true);

            assertEquals(StreamTokenizer.TT_WORD, st.NextToken());
            assertEquals(StreamTokenizer.TT_EOF, st.NextToken());
        }

        /**
         * @tests java.io.StreamTokenizer#slashSlashComments(boolean)
         */
        [Test]
        public void Test_slashSlashComments_withSSOpen_NoComment()
        {
            TextReader reader = new StringReader("// t");

            StreamTokenizer st = new StreamTokenizer(reader);
            st.SlashSlashComments = (true);
            st.OrdinaryChar('/');

            assertEquals(StreamTokenizer.TT_EOF, st.NextToken());
        }

        /**
         * @tests java.io.StreamTokenizer#slashSlashComments(boolean)
         */
        [Test]
        public void Test_slashSlashComments_withSSClosed()
        {
            TextReader reader = new StringReader("// t");

            StreamTokenizer st = new StreamTokenizer(reader);
            st.SlashSlashComments = (false);
            st.OrdinaryChar('/');

            assertEquals('/', st.NextToken());
            assertEquals('/', st.NextToken());
            assertEquals(StreamTokenizer.TT_WORD, st.NextToken());
        }

        /**
         * @tests java.io.StreamTokenizer#slashStarComments(boolean)
         */
        [Test]
        public void Test_slashStarCommentsZ()
        {
            setTest("/* foo \r\n /fiji \r\n*/ -456");
            st.OrdinaryChar('/');
            st.SlashStarComments = (true);
            assertTrue("Test failed.",
                       st.NextToken() == StreamTokenizer.TT_NUMBER);
        }

        /**
         * @tests java.io.StreamTokenizer#slashStarComments(boolean)
         */
        [Test]
        public void Test_slashStarComments_withSTOpen()
        {
            TextReader reader = new StringReader("t /* t */ t");

            StreamTokenizer st = new StreamTokenizer(reader);
            st.SlashStarComments = (true);

            assertEquals(StreamTokenizer.TT_WORD, st.NextToken());
            assertEquals(StreamTokenizer.TT_WORD, st.NextToken());
            assertEquals(StreamTokenizer.TT_EOF, st.NextToken());
        }

        /**
         * @tests java.io.StreamTokenizer#slashStarComments(boolean)
         */
        [Test]
        public void Test_slashStarComments_withSTClosed()
        {
            TextReader reader = new StringReader("t /* t */ t");

            StreamTokenizer st = new StreamTokenizer(reader);
            st.SlashStarComments = (false);

            assertEquals(StreamTokenizer.TT_WORD, st.NextToken());
            assertEquals(StreamTokenizer.TT_EOF, st.NextToken());
        }

        /**
         * @tests java.io.StreamTokenizer#toString()
         */
        [Test]
        public void Test_toString()
        {
            setTest("ABC Hello World");
            st.NextToken();
            assertTrue("toString failed." + st.toString(),
                       st.toString().equals(
                                "Token[ABC], line 1"));

            // Regression test for HARMONY-4070
            byte[] data = new byte[] { (byte)'-' };
#pragma warning disable 612, 618
            StreamTokenizer tokenizer = new StreamTokenizer(
                    new MemoryStream(data));
#pragma warning restore 612, 618
            tokenizer.NextToken();
            String result = tokenizer.toString();
            assertEquals("Token['-'], line 1", result);
        }

        /**
         * @tests java.io.StreamTokenizer#whitespaceChars(int, int)
         */
        [Test]
        public void Test_whitespaceCharsII()
        {
            setTest("azbc iof z 893");
            st.WhitespaceChars('a', 'z');
            assertTrue("OrdinaryChar failed.",
                       st.NextToken() == StreamTokenizer.TT_NUMBER);
        }

        /**
         * @tests java.io.StreamTokenizer#wordChars(int, int)
         */
        [Test]
        public void Test_wordCharsII()
        {
            setTest("A893 -9B87");
            st.WordChars('0', '9');
            assertTrue("WordChar failed1.",
                       st.NextToken() == StreamTokenizer.TT_WORD);
            assertEquals("WordChar failed2.", "A893", st.StringValue);
            assertTrue("WordChar failed3.",
                       st.NextToken() == StreamTokenizer.TT_NUMBER);
            st.NextToken();
            assertEquals("WordChar failed4.", "B87", st.StringValue);

            setTest("    Hello World");
            st.WordChars(' ', ' ');
            st.NextToken();
            assertEquals("WordChars failed for whitespace.", "Hello World", st.StringValue
                         );

            setTest("    Hello World\r\n  \'Hello World\' Hello\' World");
            st.WordChars(' ', ' ');
            st.WordChars('\'', '\'');
            st.NextToken();
            assertTrue("WordChars failed for whitespace: " + st.StringValue, st.StringValue
                       .equals("Hello World"));
            st.NextToken();
            assertTrue("WordChars failed for quote1: " + st.StringValue, st.StringValue
                       .equals("\'Hello World\' Hello\' World"));
        }

        private void setTest(string s)
        {
            testString = s;
            r = new StringReader(testString);
            st = new StreamTokenizer(r);
        }
    }
}
