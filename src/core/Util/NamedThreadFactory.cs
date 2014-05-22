using System.Threading;

namespace Lucene.Net.Util
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
	/// A default <seealso cref="ThreadFactory"/> implementation that accepts the name prefix
	/// of the created threads as a constructor argument. Otherwise, this factory
	/// yields the same semantics as the thread factory returned by
	/// <seealso cref="Executors#defaultThreadFactory()"/>.
	/// </summary>
	public class NamedThreadFactory : ThreadFactory
	{
	  private static readonly AtomicInteger ThreadPoolNumber = new AtomicInteger(1);
	  private readonly ThreadGroup Group;
	  private readonly AtomicInteger ThreadNumber = new AtomicInteger(1);
	  private const string NAME_PATTERN = "%s-%d-thread";
	  private readonly string ThreadNamePrefix;

	  /// <summary>
	  /// Creates a new <seealso cref="NamedThreadFactory"/> instance
	  /// </summary>
	  /// <param name="threadNamePrefix"> the name prefix assigned to each thread created. </param>
	  public NamedThreadFactory(string threadNamePrefix)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SecurityManager s = System.getSecurityManager();
		SecurityManager s = System.SecurityManager;
		Group = (s != null) ? s.ThreadGroup : Thread.CurrentThread.ThreadGroup;
		this.ThreadNamePrefix = string.format(Locale.ROOT, NAME_PATTERN, CheckPrefix(threadNamePrefix), ThreadPoolNumber.AndIncrement);
	  }

	  private static string CheckPrefix(string prefix)
	  {
		return prefix == null || prefix.Length == 0 ? "Lucene" : prefix;
	  }

	  /// <summary>
	  /// Creates a new <seealso cref="Thread"/>
	  /// </summary>
	  /// <seealso cref= java.util.concurrent.ThreadFactory#newThread(java.lang.Runnable) </seealso>
	  public override Thread NewThread(Runnable r)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Thread t = new Thread(group, r, String.format(java.util.Locale.ROOT, "%s-%d", this.threadNamePrefix, threadNumber.getAndIncrement()), 0);
		Thread t = new Thread(Group, r, string.format(Locale.ROOT, "%s-%d", this.ThreadNamePrefix, ThreadNumber.AndIncrement), 0);
		t.Daemon = false;
		t.Priority = Thread.NORM_PRIORITY;
		return t;
	  }

	}

}