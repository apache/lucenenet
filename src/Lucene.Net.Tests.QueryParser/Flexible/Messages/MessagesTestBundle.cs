using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    public class MessagesTestBundle : NLS
    {
        private static readonly string BUNDLE_NAME = typeof(MessagesTestBundle).Name;

        private MessagesTestBundle()
        {
            // should never be instantiated
        }

        static MessagesTestBundle()
        {
            // register all string ids with NLS class and initialize static string
            // values
            NLS.InitializeMessages(BUNDLE_NAME, typeof(MessagesTestBundle));
        }

        // static string must match the strings in the property files.
        public static String Q0001E_INVALID_SYNTAX;
        public static String Q0004E_INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

        // this message is missing from the properties file
        public static String Q0005E_MESSAGE_NOT_IN_BUNDLE;
    }
}
