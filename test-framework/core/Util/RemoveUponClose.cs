using System.Text;
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
	 * http://www.apache.org/licenses/LICENSE-2.0
	 * 
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	/// <summary>
	/// A <seealso cref="IDisposable"/> that attempts to remove a given file/folder.
	/// </summary>
	internal sealed class RemoveUponClose : IDisposable
	{
	  private readonly File File;
	  private readonly TestRuleMarkFailure FailureMarker;
	  private readonly string CreationStack;

	  public RemoveUponClose(File file, TestRuleMarkFailure failureMarker)
	  {
		this.File = file;
		this.FailureMarker = failureMarker;

		StringBuilder b = new StringBuilder();
		foreach (StackTraceElement e in Thread.CurrentThread.StackTrace)
		{
		  b.Append('\t').Append(e.ToString()).Append('\n');
		}
		CreationStack = b.ToString();
	  }

	  public override void Close()
	  {
		// only if there were no other test failures.
		if (FailureMarker.WasSuccessful())
		{
		  if (File.exists())
		  {
			try
			{
			  TestUtil.Rm(File);
			}
			catch (IOException e)
			{
			  throw new IOException("Could not remove temporary location '" + File.AbsolutePath + "', created at stack trace:\n" + CreationStack, e);
			}
		  }
		}
	  }
	}
}