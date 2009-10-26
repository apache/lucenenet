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

using LucenePackage = Lucene.Net.LucenePackage;

namespace Lucene.Net.Util
{
	
	/// <summary> Some useful constants.
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: Constants.java 780225 2009-05-30 10:00:45Z mikemccand $
	/// 
	/// </version>
	
	public sealed class Constants
	{
		private Constants()
		{
		} // can't construct
		
		/// <summary>The value of <tt>System.getProperty("java.version")<tt>. *</summary>
		public static readonly System.String JAVA_VERSION = SupportClass.AppSettings.Get("java.version", "");
		/// <summary>True iff this is Java version 1.1. </summary>
		public static readonly bool JAVA_1_1 = JAVA_VERSION.StartsWith("1.1.");
		/// <summary>True iff this is Java version 1.2. </summary>
		public static readonly bool JAVA_1_2 = JAVA_VERSION.StartsWith("1.2.");
		/// <summary>True iff this is Java version 1.3. </summary>
		public static readonly bool JAVA_1_3 = JAVA_VERSION.StartsWith("1.3.");
		
		/// <summary>The value of <tt>System.getProperty("os.name")<tt>. *</summary>
		public static readonly System.String OS_NAME = System.Environment.GetEnvironmentVariable("OS");
		/// <summary>True iff running on Linux. </summary>
		public static readonly bool LINUX = OS_NAME.StartsWith("Linux");
		/// <summary>True iff running on Windows. </summary>
		public static readonly bool WINDOWS = OS_NAME.StartsWith("Windows");
		/// <summary>True iff running on SunOS. </summary>
		public static readonly bool SUN_OS = OS_NAME.StartsWith("SunOS");
		
		public static readonly System.String OS_ARCH = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
		public static readonly System.String OS_VERSION = System.Environment.OSVersion.ToString();
		public static readonly System.String JAVA_VENDOR = SupportClass.AppSettings.Get("java.vendor", "");
		
		// NOTE: this logic may not be correct; if you know of a
		// more reliable approach please raise it on java-dev!
		public static bool JRE_IS_64BIT;
		
		public const System.String LUCENE_MAIN_VERSION = "2.9";
		
		public static System.String LUCENE_VERSION;
		static Constants()
		{
			{
				System.String x = SupportClass.AppSettings.Get("sun.arch.data.model", "");
				if (x != null)
				{
					JRE_IS_64BIT = x.IndexOf("64") != - 1;
				}
				else
				{
					if (OS_ARCH != null && OS_ARCH.IndexOf("64") != - 1)
					{
						JRE_IS_64BIT = true;
					}
					else
					{
						JRE_IS_64BIT = false;
					}
				}
			}
			{
                // {{Aroush-2.9}}
                /*
				Package pkg = LucenePackage.Get();
				System.String v = (pkg == null)?null:pkg.getImplementationVersion();
				if (v == null)
				{
					v = LUCENE_MAIN_VERSION + "-dev";
				}
				else if (v.IndexOf(LUCENE_MAIN_VERSION) == - 1)
				{
					v = v + " [" + LUCENE_MAIN_VERSION + "]";
				}
				LUCENE_VERSION = v;
                */
                LUCENE_VERSION = " [" + LUCENE_MAIN_VERSION + "]";
                // {{Aroush-2.9}}
			}
		}
	}
}