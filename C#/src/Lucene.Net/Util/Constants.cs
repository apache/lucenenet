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

namespace Lucene.Net.Util
{
	
	/// <summary> Some useful constants.
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: Constants.java 564236 2007-08-09 15:21:19Z gsingers $
	/// 
	/// </version>
	
	public sealed class Constants
	{
		private Constants()
		{
		} // can't construct
		
		/// <summary>Lucene.Net Runtime version</summary>
		public static readonly string DOTNET_VERSION = System.Reflection.Assembly.GetAssembly(typeof(Lucene.Net.Index.IndexReader)).ImageRuntimeVersion.Substring(1);
		/// <summary>True iff Lucene.Net Runtime version is 1.0</summary>
		public static readonly bool DOTNET_VERSION_1_0 = DOTNET_VERSION.StartsWith("1.0.");
		/// <summary>True iff Lucene.Net Runtime version is 1.1</summary>
		public static readonly bool DOTNET_VERSION_1_1 = DOTNET_VERSION.StartsWith("1.1.");
		/// <summary>True iff Lucene.Net Runtime version is 2.0</summary>
		public static readonly bool DOTNET_VERSION_2_0 = DOTNET_VERSION.StartsWith("2.0.");
		/// <summary>True iff Lucene.Net Runtime version is 3.0</summary>
		public static readonly bool DOTNET_VERSION_3_0 = DOTNET_VERSION.StartsWith("3.0.");
		
		/// <summary>The value of <tt>System.getProperty("os.name")<tt>. *</summary>
		public static readonly System.String OS_NAME = System.Environment.GetEnvironmentVariable("OS");
		/// <summary>True iff running on Linux. </summary>
		public static readonly bool LINUX = OS_NAME.StartsWith("Linux");
		/// <summary>True iff running on Windows. </summary>
		public static readonly bool WINDOWS = OS_NAME.StartsWith("Windows");
		/// <summary>True iff running on SunOS. </summary>
		public static readonly bool SUN_OS = OS_NAME.StartsWith("SunOS");
	}
}