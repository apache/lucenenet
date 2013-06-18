/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Threading;

namespace Lucene.Net.Store
{
    /// <summary> Simple standalone tool that forever acquires &amp; releases a
    /// lock using a specific LockFactory.  Run without any args
    /// to see usage.
    /// 
    /// </summary>
    /// <seealso cref="VerifyingLockFactory">
    /// </seealso>
    /// <seealso cref="LockVerifyServer">
    /// </seealso>
    public class LockStressTest
    {
        [STAThread]
        public static void Main(String[] args)
        {

            if (args.Length != 6)
            {
                Console.WriteLine("\nUsage: java Lucene.Net.Store.LockStressTest myID verifierHostOrIP verifierPort lockFactoryClassName lockDirName sleepTime\n" + "\n" + "  myID = int from 0 .. 255 (should be unique for test process)\n" + "  verifierHostOrIP = host name or IP address where LockVerifyServer is running\n" + "  verifierPort = port that LockVerifyServer is listening on\n" + "  lockFactoryClassName = primary LockFactory class that we will use\n" + "  lockDirName = path to the lock directory (only set for Simple/NativeFSLockFactory\n" + "  sleepTimeMS = milliseconds to pause betweeen each lock obtain/release\n" + "\n" + "You should run multiple instances of this process, each with its own\n" + "unique ID, and each pointing to the same lock directory, to verify\n" + "that locking is working correctly.\n" + "\n" + "Make sure you are first running LockVerifyServer.\n" + "\n");
                Environment.Exit(1);
            }

            int myID = Int32.Parse(args[0]);

            if (myID < 0 || myID > 255)
            {
                Console.WriteLine("myID must be a unique int 0..255");
                Environment.Exit(1);
            }

            String verifierHost = args[1];
            int verifierPort = Int32.Parse(args[2]);
            String lockFactoryClassName = args[3];
            String lockDirName = args[4];
            int sleepTimeMS = Int32.Parse(args[5]);

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
                throw new IOException("IllegalAccessException when instantiating LockClass " + lockFactoryClassName);
            }
            catch (InvalidCastException)
            {
                throw new IOException("unable to cast LockClass " + lockFactoryClassName + " instance to a LockFactory");
            }
            catch (Exception)
            {
                throw new IOException("InstantiationException when instantiating LockClass " + lockFactoryClassName);
            }

            DirectoryInfo lockDir = new DirectoryInfo(lockDirName);

            if (lockFactory is NativeFSLockFactory)
            {
                ((NativeFSLockFactory)lockFactory).LockDir = lockDir;
            }
            else if (lockFactory is SimpleFSLockFactory)
            {
                ((SimpleFSLockFactory)lockFactory).LockDir = lockDir;
            }

            lockFactory.LockPrefix = "test";

            LockFactory verifyLF = new VerifyingLockFactory((sbyte)myID, lockFactory, verifierHost, verifierPort);

            Lock l = verifyLF.MakeLock("test.lock");

            while (true)
            {

                bool obtained = false;

                try
                {
                    obtained = l.Obtain(10);
                }
                catch (LockObtainFailedException)
                {
                    Console.Out.Write("x");
                }

                if (obtained)
                {
                    Console.Out.Write("l");
                    l.Release();
                }
                Thread.Sleep(new TimeSpan((Int64)10000 * sleepTimeMS));
            }
        }
    }
}