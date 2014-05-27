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

	  public static void Main(string[] args)
	  {

		if (args.Length != 2)
		{
		  Console.WriteLine("Usage: java Lucene.Net.Store.LockVerifyServer bindToIp clients\n");
		  Environment.Exit(1);
		}

		int arg = 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String hostname = args[arg++];
		string hostname = args[arg++];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxClients = Integer.parseInt(args[arg++]);
		int maxClients = Convert.ToInt32(args[arg++]);

		using (final ServerSocket s = new ServerSocket())
		{
		  s.ReuseAddress = true;
		  s.SoTimeout = 30000; // initially 30 secs to give clients enough time to startup
		  s.bind(new InetSocketAddress(hostname, 0));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.net.InetSocketAddress localAddr = (java.net.InetSocketAddress) s.getLocalSocketAddress();
		  InetSocketAddress localAddr = (InetSocketAddress) s.LocalSocketAddress;
		  Console.WriteLine("Listening on " + localAddr + "...");

		  // we set the port as a sysprop, so the ANT task can read it. For that to work, this server must run in-process:
		  System.setProperty("lockverifyserver.port", Convert.ToString(localAddr.Port));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object localLock = new Object();
		  object localLock = new object();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] lockedID = new int[1];
		  int[] lockedID = new int[1];
		  lockedID[0] = -1;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.CountDownLatch startingGun = new java.util.concurrent.CountDownLatch(1);
		  CountDownLatch startingGun = new CountDownLatch(1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Thread[] threads = new Thread[maxClients];
		  Thread[] threads = new Thread[maxClients];

		  for (int count = 0; count < maxClients; count++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.net.Socket cs = s.accept();
			Socket cs = s.accept();
			threads[count] = new ThreadAnonymousInnerClassHelper(localLock, lockedID, startingGun, cs);
			threads[count].Start();
		  }

		  // start
		  Console.WriteLine("All clients started, fire gun...");
		  startingGun.countDown();

		  // wait for all threads to finish
		  foreach (Thread t in threads)
		  {
			t.Join();
		  }

		  // cleanup sysprop
		  System.clearProperty("lockverifyserver.port");

		  Console.WriteLine("Server terminated.");
		}
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private object LocalLock;
		  private int[] LockedID;
		  private CountDownLatch StartingGun;
		  private Socket Cs;

		  public ThreadAnonymousInnerClassHelper(object localLock, int[] lockedID, CountDownLatch startingGun, Socket cs)
		  {
			  this.LocalLock = localLock;
			  this.LockedID = lockedID;
			  this.StartingGun = startingGun;
			  this.Cs = cs;
		  }

		  public override void Run()
		  {
			using (InputStream @in = Cs.InputStream, OutputStream os = Cs.OutputStream)
			{
					try
					{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int id = in.read();
				  int id = @in.read();
				  if (id < 0)
				  {
					throw new System.IO.IOException("Client closed connection before communication started.");
				  }

				  StartingGun.@await();
				  os.write(43);
				  os.flush();

				  while (true)
				  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int command = in.read();
					int command = @in.read();
					if (command < 0)
					{
					  return; // closed
					}

					lock (LocalLock)
					{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int currentLock = lockedID[0];
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
					  os.write(command);
					  os.flush();
					}
				  }
					}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
				catch (Exception | Exception e)
				{
				  throw e;
				}
				catch (Exception ioe)
				{
				  throw new Exception(ioe);
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