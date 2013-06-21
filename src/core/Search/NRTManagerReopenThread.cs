using System;
using System.IO;
using System.Threading;

namespace Lucene.Net.Search
{
    public class NRTManagerReopenThread : NRTManager.IWaitingListener, IDisposable
    {
        private readonly Thread _thread;

        private readonly NRTManager manager;
        private readonly long targetMaxStaleNS;
        private readonly long targetMinStatleNS;
        private bool finish;
        private long waitingGen;

        public NRTManagerReopenThread(Thread thread, NRTManager manager, double targetMaxStaleSec,
                                      double targetMinStaleSec)
        {
            if (thread == null) throw new ArgumentNullException("thread");
            if (targetMaxStaleSec < targetMinStaleSec)
                throw new ArgumentException("targetMaxStaleSec (=" + targetMaxStaleSec + ") < targetMinStaleSec (=" +
                                            targetMinStaleSec + ")");

            _thread = new Thread(ThreadTask);
            this.manager = manager;
            this.targetMaxStaleNS = (long)(1000000000 * targetMaxStaleSec);
            this.targetMinStatleNS = (long)(1000000000 * targetMinStaleSec);
            manager.AddWaitingListener(this);
        }

        public virtual void Run()
        {
            _thread.Start();
        }

        #region NRTManger.IWaitingListener
        public void Waiting(long targetGen)
        {
            lock (this)
            {
                waitingGen = Math.Max(waitingGen, targetGen);
                Monitor.Pulse(this);
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            lock (this)
            {
                manager.RemoveWaitingListener(this);
                this.finish = true;
                Monitor.Pulse(this);
                _thread.Join();
            }
        }
        #endregion


        private void ThreadTask()
        {
            var lastReopenStartNS = DateTime.Now.Ticks*100; // convert to nanoseconds

            try
            {
                while (true)
                {
                    var hasWaiting = false;

                    lock (this)
                    {
                        while (!finish)
                        {
                            hasWaiting = waitingGen > manager.CurrentSearchingGen;
                            var nextReopenStartNS = lastReopenStartNS +
                                                    (hasWaiting ? targetMinStatleNS : targetMaxStaleNS);

                            var sleepNS = nextReopenStartNS - DateTime.Now.Ticks*100; // convert to nanoseconds

                            if (sleepNS > 0)
                            {
                                try
                                {
                                    Monitor.Wait(sleepNS/1000000);
                                }
                                catch (ThreadInterruptedException ex)
                                {
                                    Thread.CurrentThread.Interrupt();
                                    finish = true;
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (finish)
                        {
                            return;
                        }
                    }

                    lastReopenStartNS = DateTime.Now.Ticks*100; // convert to nanoseconds
                    try
                    {
                        manager.MaybeRefresh();
                    }
                    catch (IOException ex)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
