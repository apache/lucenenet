// LUCENENET specific - Factored out NLS so end users can elect to use .NET localization or not
// rather than forcing them to use it.

//using Lucene.Net.Util;
//using NUnit.Framework;
//using System;
//using System.Globalization;

//namespace Lucene.Net.QueryParsers.Flexible.Messages
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    public class TestNLS : LuceneTestCase
//    {
//        [Test]
//        public void TestMessageLoading()
//        {
//            IMessage invalidSyntax = new Message(
//                MessagesTestBundle.Q0001E_INVALID_SYNTAX, "XXX");
//            /* 
//             * if the default locale is ja, you get ja as a fallback:
//             * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
//             */
//            if (!CultureInfo.CurrentUICulture.Equals(new CultureInfo("ja")))
//                assertEquals("Syntax Error: XXX", invalidSyntax.GetLocalizedMessage(new CultureInfo("en")));
//        }

//        [Test]
//        public void TestMessageLoading_ja()
//        {
//            IMessage invalidSyntax = new Message(
//                MessagesTestBundle.Q0001E_INVALID_SYNTAX, "XXX");
//            assertEquals("構文エラー: XXX", invalidSyntax
//                .GetLocalizedMessage(new CultureInfo("ja")));
//        }

//        [Test]
//        public void TestNLSLoading()
//        {
//            String message = NLS
//                .GetLocalizedMessage(MessagesTestBundle.Q0004E_INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION, new CultureInfo("en"));
//            /* 
//             * if the default locale is ja, you get ja as a fallback:
//             * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
//             */
//            if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
//                assertEquals("Truncated unicode escape sequence.", message);

//            message = NLS.GetLocalizedMessage(MessagesTestBundle.Q0001E_INVALID_SYNTAX, new CultureInfo("en"),
//                "XXX");
//            /* 
//             * if the default locale is ja, you get ja as a fallback:
//             * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
//             */
//            if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
//                assertEquals("Syntax Error: XXX", message);
//        }

//        [Test]
//        public void TestNLSLoading_ja()
//        {
//            String message = NLS.GetLocalizedMessage(
//                MessagesTestBundle.Q0004E_INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION,
//                new CultureInfo("ja-JP"));
//            assertEquals("切り捨てられたユニコード・エスケープ・シーケンス。", message);

//            message = NLS.GetLocalizedMessage(MessagesTestBundle.Q0001E_INVALID_SYNTAX,
//                new CultureInfo("ja-JP"), "XXX");
//            assertEquals("構文エラー: XXX", message);
//        }

//        [Test]
//        public void TestNLSLoading_xx_XX()
//        {
//            try
//            {
//                CultureInfo locale = new CultureInfo("xx-XX");
//                String message = NLS.GetLocalizedMessage(
//                    MessagesTestBundle.Q0004E_INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION,
//                    locale);
//                /* 
//                 * if the default locale is ja, you get ja as a fallback:
//                 * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
//                 */
//                if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
//                    assertEquals("Truncated unicode escape sequence.", message);

//                message = NLS.GetLocalizedMessage(MessagesTestBundle.Q0001E_INVALID_SYNTAX,
//                    locale, "XXX");
//                /* 
//                 * if the default locale is ja, you get ja as a fallback:
//                 * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
//                 */
//                if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
//                    assertEquals("Syntax Error: XXX", message);
//            }
//            catch (CultureNotFoundException)
//            {
//                // ignore

//                // LUCENENET NOTE: .NET Core throws a CultureNotFoundException in this case.
//                // There doesn't seem to be a reasonable way to test this as a result.
//            }
//        }

//        [Test]
//        public void TestMissingMessage()
//        {
//            CultureInfo locale = new CultureInfo("en");
//            String message = NLS.GetLocalizedMessage(
//                MessagesTestBundle.Q0005E_MESSAGE_NOT_IN_BUNDLE, locale);

//            assertEquals("Message with key:Q0005E_MESSAGE_NOT_IN_BUNDLE and locale: "
//                + locale.toString() + " not found.", message);
//        }
//    }
//}
