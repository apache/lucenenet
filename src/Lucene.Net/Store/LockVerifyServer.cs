using J2N.Threading;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Store
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

    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Simple standalone server that must be running when you
    /// use <see cref="VerifyingLockFactory"/>.  This server simply
    /// verifies at most one process holds the lock at a time.
    /// Run without any args to see usage.
    /// </summary>
    /// <seealso cref="VerifyingLockFactory"/>
    /// <seealso cref="LockStressTest"/>
    public static class LockVerifyServer // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                // LUCENENET specific - our wrapper console shows the correct usage
                throw new ArgumentException();
                //Console.WriteLine("Usage: java Lucene.Net.Store.LockVerifyServer bindToIp clients\n");
                //Environment.FailFast("1");
            }

            int arg = 0;
            string hostname = args[arg++];
            int maxClients = Convert.ToInt32(args[arg++], CultureInfo.InvariantCulture);

            IPAddress ipAddress = IPAddress.Parse(hostname);

            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 30000);// SoTimeout = 30000; // initially 30 secs to give clients enough time to startup

            s.Bind(new IPEndPoint(ipAddress, 0));
            s.Listen(maxClients);
            Console.WriteLine("Listening on " + ((IPEndPoint)s.LocalEndPoint).Port.ToString() + "...");

            // we set the port as a sysprop, so the ANT task can read it. For that to work, this server must run in-process:
            Environment.SetEnvironmentVariable("lockverifyserver.port", ((IPEndPoint)s.LocalEndPoint).Port.ToString(CultureInfo.InvariantCulture));

            object localLock = new object();
            int[] lockedID = new int[1];
            lockedID[0] = -1;
            CountdownEvent startingGun = new CountdownEvent(1);
            ThreadJob[] threads = new ThreadJob[maxClients];

            for (int count = 0; count < maxClients; count++)
            {
                Socket cs = s.Accept();
                threads[count] = new ThreadAnonymousClass(localLock, lockedID, startingGun, cs);
                threads[count].Start();
            }

            // start
            Console.WriteLine("All clients started, fire gun...");
            startingGun.Signal();

            // wait for all threads to finish
            foreach (ThreadJob t in threads)
            {
                t.Join();
            }

            // cleanup sysprop
            Environment.SetEnvironmentVariable("lockverifyserver.port", null);

            Console.WriteLine("Server terminated.");
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly object localLock;
            private readonly int[] lockedID;
            private readonly CountdownEvent startingGun;
            private readonly Socket cs;

            public ThreadAnonymousClass(object localLock, int[] lockedID, CountdownEvent startingGun, Socket cs)
            {
                this.localLock = localLock;
                this.lockedID = lockedID;
                this.startingGun = startingGun;
                this.cs = cs;
            }

            public override void Run()
            {
                using Stream stream = new NetworkStream(cs);
                BinaryReader intReader = new BinaryReader(stream);
                BinaryWriter intWriter = new BinaryWriter(stream);
                try
                {
                    int id = intReader.ReadInt32();
                    if (id < 0)
                    {
                        throw new IOException("Client closed connection before communication started.");
                    }

                    startingGun.Wait();
                    intWriter.Write(43);
                    stream.Flush();

                    while (true)
                    {
                        int command = stream.ReadByte();
                        if (command < 0)
                        {
                            return; // closed
                        }

                        UninterruptableMonitor.Enter(localLock);
                        try
                        {
                            int currentLock = lockedID[0];
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
                                        lockedID[0] = -2;
                                        throw IllegalStateException.Create("id " + id + " got lock, but " + currentLock + " already holds the lock");
                                    }
                                    lockedID[0] = id;
                                    break;

                                case 0:
                                    // Unlocked
                                    if (currentLock != id)
                                    {
                                        lockedID[0] = -2;
                                        throw IllegalStateException.Create("id " + id + " released the lock, but " + currentLock + " is the one holding the lock");
                                    }
                                    lockedID[0] = -1;
                                    break;

                                default:
                                    throw RuntimeException.Create("Unrecognized command: " + command);
                            }
                            intWriter.Write((byte)command);
                            stream.Flush();
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(localLock);
                        }
                    }
                }
                catch (Exception e) when (e.IsRuntimeException() || e.IsError())
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
                catch (Exception ioe) when (ioe.IsException())
                {
                    throw RuntimeException.Create(ioe);
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(cs);
                }
            }
        }
    }
}