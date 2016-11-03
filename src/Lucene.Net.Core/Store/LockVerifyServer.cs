using System;
using System.Threading;

namespace Lucene.Net.Store
{
    using Lucene.Net.Support;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;

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

    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Simple standalone server that must be running when you
    /// use <seealso cref="VerifyingLockFactory"/>.  this server simply
    /// verifies at most one process holds the lock at a time.
    /// Run without any args to see usage.
    /// </summary>
    /// <seealso cref= VerifyingLockFactory </seealso>
    /// <seealso cref= LockStressTest </seealso>

    public class LockVerifyServer
    {
        private static String GetTime(long startTime)
        {
            return "[" + (((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - startTime) / 1000) + "s] ";
        }

        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: java Lucene.Net.Store.LockVerifyServer bindToIp clients\n");
                Environment.FailFast("1");
            }

            int arg = 0;
            IPHostEntry ipHostInfo = Dns.GetHostEntryAsync(args[arg++]).Result;
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 0);
            int maxClients = Convert.ToInt32(args[arg++]);

            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 30000);// SoTimeout = 30000; // initially 30 secs to give clients enough time to startup

                s.Bind(localEndPoint);
                Console.WriteLine("Listening on " + ((IPEndPoint)s.LocalEndPoint).Port.ToString() + "...");

                // we set the port as a sysprop, so the ANT task can read it. For that to work, this server must run in-process:
                System.Environment.SetEnvironmentVariable("lockverifyserver.port", ((IPEndPoint)s.LocalEndPoint).Port.ToString());

                object localLock = new object();
                int[] lockedID = new int[1];
                lockedID[0] = -1;
                CountdownEvent startingGun = new CountdownEvent(1);
                ThreadClass[] threads = new ThreadClass[maxClients];

                for (int count = 0; count < maxClients; count++)
                {
                    Socket cs = s.Accept();
                    threads[count] = new ThreadAnonymousInnerClassHelper(localLock, lockedID, startingGun, cs);
                    threads[count].Start();
                }

                // start
                Console.WriteLine("All clients started, fire gun...");
                startingGun.Signal();

                // wait for all threads to finish
                foreach (ThreadClass t in threads)
                {
                    t.Join();
                }

                //LUCENE TO-DO Not sure if equivalent?
                // cleanup sysprop
                //System.clearProperty("lockverifyserver.port");

                Console.WriteLine("Server terminated.");
            }
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private object LocalLock;
            private int[] LockedID;
            private CountdownEvent StartingGun;
            private Socket Cs;

            public ThreadAnonymousInnerClassHelper(object localLock, int[] lockedID, CountdownEvent startingGun, Socket cs)
            {
                this.LocalLock = localLock;
                this.LockedID = lockedID;
                this.StartingGun = startingGun;
                this.Cs = cs;
            }

            public override void Run()
            {
                using (Stream @in = new NetworkStream(Cs), os = new NetworkStream(Cs))
                {
                    BinaryReader intReader = new BinaryReader(@in);
                    BinaryWriter intWriter = new BinaryWriter(os);
                    try
                    {
                        int id = intReader.ReadInt32();
                        if (id < 0)
                        {
                            throw new System.IO.IOException("Client closed connection before communication started.");
                        }

                        //LUCENE TO-DO NOt sure about this
                        StartingGun.Wait();
                        intWriter.Write(43);
                        os.Flush();

                        while (true)
                        {
                            int command = intReader.ReadInt32();
                            if (command < 0)
                            {
                                return; // closed
                            }

                            lock (LocalLock)
                            {
                                int currentLock = LockedID[0];
                                if (currentLock == -2)
                                {
                                    return; // another thread got error, so we exit, too!
                                }
                                switch (command)
                                {
                                    case 1:
                                        // Locked
                                        if (currentLock != -1)
                                        {
                                            LockedID[0] = -2;
                                            throw new InvalidOperationException("id " + id + " got lock, but " + currentLock + " already holds the lock");
                                        }
                                        LockedID[0] = id;
                                        break;

                                    case 0:
                                        // Unlocked
                                        if (currentLock != id)
                                        {
                                            LockedID[0] = -2;
                                            throw new InvalidOperationException("id " + id + " released the lock, but " + currentLock + " is the one holding the lock");
                                        }
                                        LockedID[0] = -1;
                                        break;

                                    default:
                                        throw new Exception("Unrecognized command: " + command);
                                }
                                intWriter.Write(command);
                                os.Flush();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        IOUtils.CloseWhileHandlingException(Cs);
                    }
                }
            }
        }
    }
}