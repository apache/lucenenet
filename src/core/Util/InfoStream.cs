using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public abstract class InfoStream : IDisposable, ICloneable
    {
        public static readonly InfoStream NO_OUTPUT = new NoOutput();

        private sealed class NoOutput : InfoStream
        {
            public override void Message(string component, string message)
            {
                // assert false
            }

            public override bool IsEnabled(string component)
            {
                return false;
            }

            public override void Dispose()
            {
            }
        }

        public abstract void Message(string component, string message);

        public abstract bool IsEnabled(string component);

        private static InfoStream defaultInfoStream = NO_OUTPUT;

        public static InfoStream Default
        {
            get
            {
                lock (typeof(InfoStream))
                {
                    return defaultInfoStream;
                }
            }
            set
            {
                lock (typeof(InfoStream))
                {
                    if (value == null)
                        throw new ArgumentException("Cannot set InfoStream default implementation to null. To disable logging use InfoStream.NO_OUTPUT.");

                    defaultInfoStream = value;
                }
            }
        }

        public virtual void Dispose()
        {
        }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
