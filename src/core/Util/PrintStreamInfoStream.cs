using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Util
{
    public class PrintStreamInfoStream : InfoStream
    {
        private static int MESSAGE_ID = 0;

        protected readonly int messageID;
        protected readonly TextWriter stream;

        public PrintStreamInfoStream(TextWriter stream)
            : this(stream, Interlocked.Increment(ref MESSAGE_ID))
        {
        }

        public PrintStreamInfoStream(TextWriter stream, int messageID)
        {
            this.stream = stream;
            this.messageID = messageID;
        }

        public override void Message(string component, string message)
        {
            stream.WriteLine(component + " " + messageID + " [" + DateTime.UtcNow + "; " + Thread.CurrentThread.Name + "]: " + message);
        }

        public override bool IsEnabled(string component)
        {
            return true;
        }

        public override void Dispose()
        {
            if (!IsSystemStream())
            {
                stream.Dispose();
            }
        }

        public bool IsSystemStream()
        {
            return stream == Console.Out || stream == Console.Error;
        }
    }
}
