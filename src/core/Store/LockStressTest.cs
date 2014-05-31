using System;
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

	  public static void Main(string[] args)
	  {

		if (args.Length != 7)
		{
		  Console.WriteLine("Usage: java Lucene.Net.Store.LockStressTest myID verifierHost verifierPort lockFactoryClassName lockDirName sleepTimeMS count\n" + "\n" + "  myID = int from 0 .. 255 (should be unique for test process)\n" + "  verifierHost = hostname that LockVerifyServer is listening on\n" + "  verifierPort = port that LockVerifyServer is listening on\n" + "  lockFactoryClassName = primary LockFactory class that we will use\n" + "  lockDirName = path to the lock directory (only set for Simple/NativeFSLockFactory\n" + "  sleepTimeMS = milliseconds to pause betweeen each lock obtain/release\n" + "  count = number of locking tries\n" + "\n" + "You should run multiple instances of this process, each with its own\n" + "unique ID, and each pointing to the same lock directory, to verify\n" + "that locking is working correctly.\n" + "\n" + "Make sure you are first running LockVerifyServer.");
		  Environment.Exit(1);
		}

		int arg = 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int myID = Integer.parseInt(args[arg++]);
		int myID = Convert.ToInt32(args[arg++]);

		if (myID < 0 || myID > 255)
		{
		  Console.WriteLine("myID must be a unique int 0..255");
		  Environment.Exit(1);
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String verifierHost = args[arg++];
		string verifierHost = args[arg++];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int verifierPort = Integer.parseInt(args[arg++]);
		int verifierPort = Convert.ToInt32(args[arg++]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String lockFactoryClassName = args[arg++];
		string lockFactoryClassName = args[arg++];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String lockDirName = args[arg++];
		string lockDirName = args[arg++];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int sleepTimeMS = Integer.parseInt(args[arg++]);
		int sleepTimeMS = Convert.ToInt32(args[arg++]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int count = Integer.parseInt(args[arg++]);
		int count = Convert.ToInt32(args[arg++]);

		LockFactory lockFactory;
		try
		{
		  lockFactory = Type.GetType(lockFactoryClassName).asSubclass(typeof(LockFactory)).newInstance();
		}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		catch (IllegalAccessException)
		{
		  throw new System.IO.IOException("Cannot instantiate lock factory " + lockFactoryClassName);
		}
        catch (InvalidCastException) {
            throw new IOException("unable to cast LockClass " + lockFactoryClassName + " instance to a LockFactory");
        }
        catch (ClassNotFoundException)
        {
            throw new IOException("InstantiationException when instantiating LockClass " + lockFactoryClassName);
        }

		File lockDir = new File(lockDirName);

		if (lockFactory is FSLockFactory)
		{
		  ((FSLockFactory) lockFactory).LockDir = lockDir;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.net.InetSocketAddress addr = new java.net.InetSocketAddress(verifierHost, verifierPort);
		InetSocketAddress addr = new InetSocketAddress(verifierHost, verifierPort);
		Console.WriteLine("Connecting to server " + addr + " and registering as client " + myID + "...");
		using (Socket socket = new Socket())
		{
		  socket.ReuseAddress = true;
		  socket.connect(addr, 500);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.OutputStream out = socket.getOutputStream();
		  OutputStream @out = socket.OutputStream;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.InputStream in = socket.getInputStream();
		  InputStream @in = socket.InputStream;

		  @out.write(myID);
		  @out.flush();

		  lockFactory.LockPrefix = "test";
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final LockFactory verifyLF = new VerifyingLockFactory(lockFactory, in, out);
		  LockFactory verifyLF = new VerifyingLockFactory(lockFactory, @in, @out);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lock l = verifyLF.makeLock("test.lock");
		  Lock l = verifyLF.MakeLock("test.lock");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Random rnd = new java.util.Random();
		  Random rnd = new Random();

		  // wait for starting gun
		  if (@in.read() != 43)
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
			  l.Close();
			}

			if (i % 500 == 0)
			{
			  Console.WriteLine((i * 100.0 / count) + "% done.");
			}

			Thread.Sleep(sleepTimeMS);
		  }
		}

		Console.WriteLine("Finished " + count + " tries.");
	  }
	}

}