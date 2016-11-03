using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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

    /// <summary>
    /// Simple standalone tool that forever acquires & releases a
    /// lock using a specific LockFactory.  Run without any args
    /// to see usage.
    /// </summary>
    /// <seealso cref= VerifyingLockFactory </seealso>
    /// <seealso cref= LockVerifyServer </seealso>

    public class LockStressTest
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length != 7)
            {
                Console.WriteLine("Usage: java Lucene.Net.Store.LockStressTest myID verifierHost verifierPort lockFactoryClassName lockDirName sleepTimeMS count\n" + "\n" + "  myID = int from 0 .. 255 (should be unique for test process)\n" + "  verifierHost = hostname that LockVerifyServer is listening on\n" + "  verifierPort = port that LockVerifyServer is listening on\n" + "  lockFactoryClassName = primary LockFactory class that we will use\n" + "  lockDirName = path to the lock directory (only set for Simple/NativeFSLockFactory\n" + "  sleepTimeMS = milliseconds to pause betweeen each lock obtain/release\n" + "  count = number of locking tries\n" + "\n" + "You should run multiple instances of this process, each with its own\n" + "unique ID, and each pointing to the same lock directory, to verify\n" + "that locking is working correctly.\n" + "\n" + "Make sure you are first running LockVerifyServer.");
                Environment.FailFast("1");
            }

            int arg = 0;
            int myID = Convert.ToInt32(args[arg++]);

            if (myID < 0 || myID > 255)
            {
                Console.WriteLine("myID must be a unique int 0..255");
                Environment.FailFast("1");
            }

            IPHostEntry verifierHost = Dns.GetHostEntryAsync(args[arg++]).Result;
            int verifierPort = Convert.ToInt32(args[arg++]);
            IPAddress verifierIp = verifierHost.AddressList[0];
            IPEndPoint addr = new IPEndPoint(verifierIp, verifierPort);

            string lockFactoryClassName = args[arg++];
            string lockDirName = args[arg++];
            int sleepTimeMS = Convert.ToInt32(args[arg++]);
            int count = Convert.ToInt32(args[arg++]);

            Type c;
            try
            {
                c = Type.GetType(lockFactoryClassName);
            }
            catch (Exception)
            {
                throw new IOException("unable to find LockClass " + lockFactoryClassName);
            }

            LockFactory lockFactory;
            try
            {
                lockFactory = (LockFactory)Activator.CreateInstance(c);
            }
            catch (UnauthorizedAccessException)
            {
                throw new System.IO.IOException("Cannot instantiate lock factory " + lockFactoryClassName);
            }
            catch (InvalidCastException)
            {
                throw new System.IO.IOException("unable to cast LockClass " + lockFactoryClassName + " instance to a LockFactory");
            }
            catch (Exception)
            {
                throw new System.IO.IOException("InstantiationException when instantiating LockClass " + lockFactoryClassName);
            }

            DirectoryInfo lockDir = new DirectoryInfo(lockDirName);

            if (lockFactory is FSLockFactory)
            {
                ((FSLockFactory)lockFactory).LockDir = lockDir;
            }

            Console.WriteLine("Connecting to server " + addr + " and registering as client " + myID + "...");
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                using (Stream @out = new NetworkStream(socket), @in = new NetworkStream(socket))
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                    socket.Connect(verifierIp, 500);

                    BinaryReader intReader = new BinaryReader(@in);
                    BinaryWriter intWriter = new BinaryWriter(@out);

                    intWriter.Write(myID);
                    @out.Flush();

                    lockFactory.LockPrefix = "test";
                    LockFactory verifyLF = new VerifyingLockFactory(lockFactory, @in, @out);
                    Lock l = verifyLF.MakeLock("test.lock");
                    Random rnd = new Random();

                    // wait for starting gun
                    if (intReader.ReadInt32() != 43)
                    {
                        throw new System.IO.IOException("Protocol violation");
                    }

                    for (int i = 0; i < count; i++)
                    {
                        bool obtained = false;

                        try
                        {
                            obtained = l.Obtain(rnd.Next(100) + 10);
                        }
                        catch (LockObtainFailedException e)
                        {
                        }

                        if (obtained)
                        {
                            Thread.Sleep(sleepTimeMS);
                            l.Release();
                        }

                        if (i % 500 == 0)
                        {
                            Console.WriteLine((i * 100.0 / count) + "% done.");
                        }

                        Thread.Sleep(sleepTimeMS);
                    }
                }
            }

            Console.WriteLine("Finished " + count + " tries.");
        }

        private static int ToInt32(byte[] tempBuf, int p)
        {
            throw new NotImplementedException();
        }
    }
}