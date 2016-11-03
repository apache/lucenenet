using System;
using System.IO;
using System.Threading;

namespace Lucene.Net.Util
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
    /// InfoStream implementation over a <seealso cref="PrintStream"/>
    /// such as <code>System.out</code>.
    ///
    /// @lucene.internal
    /// </summary>
    public class PrintStreamInfoStream : InfoStream
    {
        // Used for printing messages
        private static int MESSAGE_ID = 0;

        protected internal readonly int MessageID;

        protected internal readonly TextWriter Stream;

        public PrintStreamInfoStream(TextWriter stream)
            : this(stream, Interlocked.Increment(ref MESSAGE_ID))
        {
        }

        public PrintStreamInfoStream(TextWriter stream, int messageID)
        {
            this.Stream = stream;
            this.MessageID = messageID;
        }

        public override void Message(string component, string message)
        {
            Stream.Write(component + " " + MessageID + " [" + DateTime.Now + "; " + Thread.CurrentThread.Name + "]: " + message);
        }

        public override bool IsEnabled(string component)
        {
            return true;
        }

        public override void Dispose()
        {
            if (!SystemStream)
            {
                Stream.Dispose();
            }
        }

        public virtual bool SystemStream
        {
            get
            {
                return Stream == Console.Out || Stream == Console.Error;
            }
        }
    }
}