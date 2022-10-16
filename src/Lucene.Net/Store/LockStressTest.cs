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

    /// <summary>
    /// Simple standalone tool that forever acquires &amp; releases a
    /// lock using a specific <see cref="LockFactory"/>.  Run without any args
    /// to see usage.
    /// </summary>
    /// <seealso cref="VerifyingLockFactory"/>
    /// <seealso cref="LockVerifyServer"/>
    public static class LockStressTest // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length != 7)
            {
                // LUCENENET specific - our lucene-cli wrapper console shows the correct usage
                throw new ArgumentException();
                //Console.WriteLine("Usage: java Lucene.Net.Store.LockStressTest myID verifierHost verifierPort lockFactoryClassName lockDirName sleepTimeMS count\n" + 
                //    "\n" + 
                //    "  myID = int from 0 .. 255 (should be unique for test process)\n" + 
                //    "  verifierHost = hostname that LockVerifyServer is listening on\n" + 
                //    "  verifierPort = port that LockVerifyServer is listening on\n" + 
                //    "  lockFactoryClassName = primary LockFactory class that we will use\n" + 
                //    "  lockDirName = path to the lock directory (only set for Simple/NativeFSLockFactory\n" + 
                //    "  sleepTimeMS = milliseconds to pause betweeen each lock obtain/release\n" + 
                //    "  count = number of locking tries\n" + 
                //    "\n" + 
                //    "You should run multiple instances of this process, each with its own\n" + 
                //    "unique ID, and each pointing to the same lock directory, to verify\n" + 
                //    "that locking is working correctly.\n" + 
                //    "\n" + 
                //    "Make sure you are first running LockVerifyServer.");
                //Environment.FailFast("1");
            }

            int arg = 0;
            int myID = Convert.ToInt32(args[arg++], CultureInfo.InvariantCulture);

            if (myID < 0 || myID > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(args), "ID must be a unique int 0..255"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                //Console.WriteLine("myID must be a unique int 0..255");
                //Environment.Exit(1);
            }

            string verifierHost = args[arg++];
            int verifierPort = Convert.ToInt32(args[arg++], CultureInfo.InvariantCulture);
            string lockFactoryClassName = args[arg++];
            string lockDirName = args[arg++];
            int sleepTimeMS = Convert.ToInt32(args[arg++], CultureInfo.InvariantCulture);
            int count = Convert.ToInt32(args[arg++], CultureInfo.InvariantCulture);

            IPAddress[] addresses = Dns.GetHostAddressesAsync(verifierHost).Result;
            IPAddress addr = addresses.Length > 0 ? addresses[0] : null;

            Type c;
            try
            {
                c = Type.GetType(lockFactoryClassName);
                if (c is null)
                {
                    // LUCENENET: try again, this time with the Store namespace
                    c = Type.GetType("Lucene.Net.Store." + lockFactoryClassName);
                }
            }
            catch (Exception e)
            {
                throw new IOException("unable to find LockClass " + lockFactoryClassName, e);
            }

            LockFactory lockFactory;
            try
            {
                lockFactory = (LockFactory)Activator.CreateInstance(c);
            }
            catch (Exception e) when (e.IsIllegalAccessException() || e.IsInstantiationException() || e.IsClassNotFoundException())
            {
                throw new IOException("Cannot instantiate lock factory " + lockFactoryClassName, e);
            }
            // LUCENENET specific - added more explicit exception message in this case
            catch (Exception e) when (e.IsClassCastException())
            {
                throw new IOException("unable to cast LockClass " + lockFactoryClassName + " instance to a LockFactory", e);
            }

            DirectoryInfo lockDir = new DirectoryInfo(lockDirName);

            if (lockFactory is FSLockFactory fsLockFactory)
            {
                fsLockFactory.SetLockDir(lockDir);
            }

            Console.WriteLine("Connecting to server " + addr + " and registering as client " + myID + "...");
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            socket.Connect(verifierHost, verifierPort);

            using Stream stream = new NetworkStream(socket);
            BinaryReader intReader = new BinaryReader(stream);
            BinaryWriter intWriter = new BinaryWriter(stream);

            intWriter.Write(myID);
            stream.Flush();

            lockFactory.LockPrefix = "test";
            LockFactory verifyLF = new VerifyingLockFactory(lockFactory, stream);
            Lock l = verifyLF.MakeLock("test.lock");
            Random rnd = new Random();

            // wait for starting gun
            if (intReader.ReadInt32() != 43)
            {
                throw new IOException("Protocol violation");
            }

            for (int i = 0; i < count; i++)
            {
                bool obtained = false;

                try
                {
                    obtained = l.Obtain(rnd.Next(100) + 10);
                }
#pragma warning disable 168
                catch (LockObtainFailedException e)
#pragma warning restore 168
                {
                }

                if (obtained)
                {
                    Thread.Sleep(sleepTimeMS);
                    l.Dispose();
                }

                if (i % 500 == 0)
                {
                    Console.WriteLine((i * 100.0 / count) + "% done.");
                }

                Thread.Sleep(sleepTimeMS);
            }
            Console.WriteLine("Finished " + count + " tries.");
        }
    }
}