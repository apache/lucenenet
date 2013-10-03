using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    public class Message : IMessage
    {
        private string key;

        private object[] arguments = new object[0];

        public Message(string key)
        {
            this.key = key;
        }

        public Message(string key, params object[] args)
            : this(key)
        {
            this.arguments = args;
        }
        
        public object[] Arguments
        {
            get { return this.arguments; }
        }

        public string Key
        {
            get { return this.key; }
        }

        public string LocalizedMessage
        {
            get { return GetLocalizedMessage(CultureInfo.CurrentCulture); }
        }

        public string GetLocalizedMessage(CultureInfo locale)
        {
            return NLS.GetLocalizedMessage(Key, locale, Arguments);
        }

        public override string ToString()
        {
            object[] args = Arguments;
            StringBuilder sb = new StringBuilder(Key);
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    sb.Append(i == 0 ? " " : ", ").Append(args[i]);
                }
            }
            return sb.ToString();
        }
    }
}
