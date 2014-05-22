using System;

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
	/// Some useful constants.
	/// 
	/// </summary>

	public sealed class Constants
	{
	  private Constants() // can't construct
	  {
	  }

	  /// <summary>
	  /// JVM vendor info. </summary>
	  public static readonly string JVM_VENDOR = System.getProperty("java.vm.vendor");
	  public static readonly string JVM_VERSION = System.getProperty("java.vm.version");
	  public static readonly string JVM_NAME = System.getProperty("java.vm.name");

	  /// <summary>
	  /// The value of <tt>System.getProperty("java.version")</tt>. * </summary>
	  public static readonly string JAVA_VERSION = System.getProperty("java.version");

	  /// <summary>
	  /// The value of <tt>System.getProperty("os.name")</tt>. * </summary>
	  public static readonly string OS_NAME = System.getProperty("os.name");
	  /// <summary>
	  /// True iff running on Linux. </summary>
	  public static readonly bool LINUX = OS_NAME.StartsWith("Linux");
	  /// <summary>
	  /// True iff running on Windows. </summary>
	  public static readonly bool WINDOWS = OS_NAME.StartsWith("Windows");
	  /// <summary>
	  /// True iff running on SunOS. </summary>
	  public static readonly bool SUN_OS = OS_NAME.StartsWith("SunOS");
	  /// <summary>
	  /// True iff running on Mac OS X </summary>
	  public static readonly bool MAC_OS_X = OS_NAME.StartsWith("Mac OS X");
	  /// <summary>
	  /// True iff running on FreeBSD </summary>
	  public static readonly bool FREE_BSD = OS_NAME.StartsWith("FreeBSD");

	  public static readonly string OS_ARCH = System.getProperty("os.arch");
	  public static readonly string OS_VERSION = System.getProperty("os.version");
	  public static readonly string JAVA_VENDOR = System.getProperty("java.vendor");

	  /// @deprecated With Lucene 4.0, we are always on Java 6 
	  [Obsolete("With Lucene 4.0, we are always on Java 6")]
	  public static readonly bool JRE_IS_MINIMUM_JAVA6 = (bool)new bool?(true); // prevent inlining in foreign class files

	  /// @deprecated With Lucene 4.8, we are always on Java 7 
	  [Obsolete("With Lucene 4.8, we are always on Java 7")]
	  public static readonly bool JRE_IS_MINIMUM_JAVA7 = (bool)new bool?(true); // prevent inlining in foreign class files

	  public static readonly bool JRE_IS_MINIMUM_JAVA8;

	  /// <summary>
	  /// True iff running on a 64bit JVM </summary>
	  public static readonly bool JRE_IS_64BIT;

	  static Constants()
	  {
		bool is64Bit = false;
		try
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Class unsafeClass = Class.forName("sun.misc.Unsafe");
		  Type unsafeClass = Type.GetType("sun.misc.Unsafe");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Field unsafeField = unsafeClass.getDeclaredField("theUnsafe");
		  Field unsafeField = unsafeClass.getDeclaredField("theUnsafe");
		  unsafeField.Accessible = true;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object unsafe = unsafeField.get(null);
		  object @unsafe = unsafeField.get(null);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int addressSize = ((Number) unsafeClass.getMethod("addressSize").invoke(unsafe)).intValue();
		  int addressSize = (int)((Number) unsafeClass.GetMethod("addressSize").invoke(@unsafe));
		  //System.out.println("Address size: " + addressSize);
		  is64Bit = addressSize >= 8;
		}
		catch (Exception e)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String x = System.getProperty("sun.arch.data.model");
		  string x = System.getProperty("sun.arch.data.model");
		  if (x != null)
		  {
			is64Bit = x.IndexOf("64") != -1;
		  }
		  else
		  {
			if (OS_ARCH != null && OS_ARCH.IndexOf("64") != -1)
			{
			  is64Bit = true;
			}
			else
			{
			  is64Bit = false;
			}
		  }
		}
		JRE_IS_64BIT = is64Bit;

		// this method only exists in Java 8:
		bool v8 = true;
		try
		{
		  typeof(Collections).getMethod("emptySortedSet");
		}
		catch (NoSuchMethodException nsme)
		{
		  v8 = false;
		}
		JRE_IS_MINIMUM_JAVA8 = v8;
		Package pkg = LucenePackage.Get();
		string v = (pkg == null) ? null : pkg.ImplementationVersion;
		if (v == null)
		{
		  v = MainVersionWithoutAlphaBeta() + "-SNAPSHOT";
		}
		LUCENE_VERSION = Ident(v);
	  }

	  // this method prevents inlining the final version constant in compiled classes,
	  // see: http://www.javaworld.com/community/node/3400
	  private static string Ident(string s)
	  {
		return s.ToString();
	  }

	  // We should never change index format with minor versions, so it should always be x.y or x.y.0.z for alpha/beta versions!
	  /// <summary>
	  /// this is the internal Lucene version, recorded into each segment.
	  /// NOTE: we track per-segment version as a String with the {@code "X.Y"} format
	  /// (no minor version), e.g. {@code "4.0", "3.1", "3.0"}.
	  /// <p>Alpha and Beta versions will have numbers like {@code "X.Y.0.Z"},
	  /// anything else is not allowed. this is done to prevent people from
	  /// using indexes created with ALPHA/BETA versions with the released version.
	  /// </summary>
	  public static readonly string LUCENE_MAIN_VERSION = Ident("4.8");

	  /// <summary>
	  /// this is the Lucene version for display purposes.
	  /// </summary>
	  public static readonly string LUCENE_VERSION;

	  /// <summary>
	  /// Returns a LUCENE_MAIN_VERSION without any ALPHA/BETA qualifier
	  /// Used by test only!
	  /// </summary>
	  internal static string MainVersionWithoutAlphaBeta()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String parts[] = LUCENE_MAIN_VERSION.split("\\.");
		string[] parts = LUCENE_MAIN_VERSION.Split("\\.", true);
		if (parts.Length == 4 && "0".Equals(parts[2]))
		{
		  return parts[0] + "." + parts[1];
		}
		return LUCENE_MAIN_VERSION;
	  }
	}

}