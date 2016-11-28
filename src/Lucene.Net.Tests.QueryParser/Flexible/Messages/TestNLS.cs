using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    public class TestNLS : LuceneTestCase
    {
        [Test]
        public void testMessageLoading()
        {
            string message = MessagesTestBundle.ResourceManager.GetString("Q0001E_INVALID_SYNTAX");

            fail("not implemented");

            //IMessage invalidSyntax = new MessageImpl(
            //    MessagesTestBundle.Q0001E_INVALID_SYNTAX, "XXX");
            ///* 
            // * if the default locale is ja, you get ja as a fallback:
            // * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
            // */
            ////if (!Locale.getDefault().getLanguage().equals("ja"))
            ////    assertEquals("Syntax Error: XXX", invalidSyntax.getLocalizedMessage(Locale.ENGLISH));
            //if (!CultureInfo.CurrentUICulture.Equals(new CultureInfo("ja-JP")))
            //    assertEquals("Syntax Error: XXX", invalidSyntax.GetLocalizedMessage(new CultureInfo("en")));
        }

        //[Test]
        //public void testMessageLoading_ja()
        //{
        //    IMessage invalidSyntax = new MessageImpl(
        //        MessagesTestBundle.Q0001E_INVALID_SYNTAX, "XXX");
        //    assertEquals("構文エラー: XXX", invalidSyntax
        //        .GetLocalizedMessage(new CultureInfo("ja-JP")));
        //}

        //[Test]
        //public void testNLSLoading()
        //{
        //    String message = NLS
        //        .GetLocalizedMessage(MessagesTestBundle.Q0004E_INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION, new CultureInfo("en"));
        //    /* 
        //     * if the default locale is ja, you get ja as a fallback:
        //     * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
        //     */
        //    //if (!Locale.getDefault().getLanguage().equals("ja"))
        //    if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
        //        assertEquals("Truncated unicode escape sequence.", message);

        //    message = NLS.GetLocalizedMessage(MessagesTestBundle.Q0001E_INVALID_SYNTAX, new CultureInfo("en"),
        //        "XXX");
        //    /* 
        //     * if the default locale is ja, you get ja as a fallback:
        //     * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
        //     */
        //    //if (!Locale.getDefault().getLanguage().equals("ja"))
        //    //if (!CultureInfo.CurrentUICulture.Equals(new CultureInfo("ja-JP")))
        //    if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
        //        assertEquals("Syntax Error: XXX", message);
        //}

        //[Test]
        //public void testNLSLoading_ja()
        //{
        //    String message = NLS.GetLocalizedMessage(
        //        MessagesTestBundle.Q0004E_INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION,
        //        new CultureInfo("ja-JP"));
        //    assertEquals("切り捨てられたユニコード・エスケープ・シーケンス。", message);

        //    message = NLS.GetLocalizedMessage(MessagesTestBundle.Q0001E_INVALID_SYNTAX,
        //        new CultureInfo("ja-JP"), "XXX");
        //    assertEquals("構文エラー: XXX", message);
        //}

        //[Test]
        //public void testNLSLoading_xx_XX()
        //{
        //    //Locale locale = new Locale("xx", "XX", "");
        //    CultureInfo locale = new CultureInfo("xx-XX");
        //    String message = NLS.GetLocalizedMessage(
        //        MessagesTestBundle.Q0004E_INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION,
        //        locale);
        //    /* 
        //     * if the default locale is ja, you get ja as a fallback:
        //     * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
        //     */
        //    //if (!Locale.getDefault().getLanguage().equals("ja"))
        //    if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
        //        assertEquals("Truncated unicode escape sequence.", message);

        //    message = NLS.GetLocalizedMessage(MessagesTestBundle.Q0001E_INVALID_SYNTAX,
        //        locale, "XXX");
        //    /* 
        //     * if the default locale is ja, you get ja as a fallback:
        //     * see ResourceBundle.html#getBundle(java.lang.String, java.util.Locale, java.lang.ClassLoader)
        //     */
        //    //if (!Locale.getDefault().getLanguage().equals("ja"))
        //    if (!CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
        //        assertEquals("Syntax Error: XXX", message);
        //}

        // LUCENENET TODO: This doesn't compile because the resources don't contain this message

        //[Test]
        //public void testMissingMessage()
        //{
        //    //Locale locale = Locale.ENGLISH;
        //    CultureInfo locale = new CultureInfo("en");
        //    String message = NLS.GetLocalizedMessage(
        //        MessagesTestBundle.Q0005E_MESSAGE_NOT_IN_BUNDLE, locale);

        //    assertEquals("Message with key:Q0005E_MESSAGE_NOT_IN_BUNDLE and locale: "
        //        + locale.toString() + " not found.", message);
        //}
    }
}
