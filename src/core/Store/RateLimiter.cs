using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Store
{
    public abstract class RateLimiter
    {
        public abstract double MbPerSec { get; set; }

        public abstract long Pause(long bytes);

        public class SimpleRateLimiter : RateLimiter
        {
            private double mbPerSec;
            private double nsPerByte;
            private long lastNS;

            public SimpleRateLimiter(double mbPerSec)
            {
                this.MbPerSec = mbPerSec;
            }

            public override double MbPerSec
            {
                get
                {
                    return this.mbPerSec;
                }
                set
                {
                    this.mbPerSec = value;
                    nsPerByte = (double)1000000000 / (1024 * 1024 * value);
                }
            }

            public override long Pause(long bytes)
            {
                if (bytes == 1)
                {
                    return 0;
                }

                // TODO: this is purely instantaneous rate; maybe we
                // should also offer decayed recent history one?
                long targetNS = lastNS = lastNS + ((long)(bytes * nsPerByte));
                long startNS;
                long curNS = startNS = DateTime.UtcNow.Ticks * 100 /* ns */;
                if (lastNS < curNS)
                {
                    lastNS = curNS;
                }

                // While loop because Thread.sleep doesn't always sleep
                // enough:
                while (true)
                {
                    long pauseNS = targetNS - curNS;
                    if (pauseNS > 0)
                    {
                        try
                        {
                            Thread.Sleep((int)(pauseNS / 1000000));
                        }
                        catch (ThreadInterruptedException)
                        {
                            throw;
                        }
                        curNS = DateTime.UtcNow.Ticks * 100;
                        continue;
                    }
                    break;
                }
                return curNS - startNS;
            }
        }
    }
}
