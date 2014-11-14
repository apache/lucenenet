using System.Collections;

namespace org.apache.lucene.analysis.@in
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

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static Character.UnicodeBlock.*;
	using org.apache.lucene.analysis.util;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.util.StemmerUtil.*;

	/// <summary>
	/// Normalizes the Unicode representation of text in Indian languages.
	/// <para>
	/// Follows guidelines from Unicode 5.2, chapter 6, South Asian Scripts I
	/// and graphical decompositions from http://ldc.upenn.edu/myl/IndianScriptsUnicode.html
	/// </para>
	/// </summary>
	public class IndicNormalizer
	{

	  private class ScriptData
	  {
		internal readonly int flag;
		internal readonly int @base;
		internal BitArray decompMask;

		internal ScriptData(int flag, int @base)
		{
		  this.flag = flag;
		  this.@base = @base;
		}
	  }

	  private static readonly IdentityHashMap<char.UnicodeBlock, ScriptData> scripts = new IdentityHashMap<char.UnicodeBlock, ScriptData>(9);

	  private static int flag(char.UnicodeBlock ub)
	  {
		return scripts.get(ub).flag;
	  }

	  static IndicNormalizer()
	  {
		scripts.put(DEVANAGARI, new ScriptData(1, 0x0900));
		scripts.put(BENGALI, new ScriptData(2, 0x0980));
		scripts.put(GURMUKHI, new ScriptData(4, 0x0A00));
		scripts.put(GUJARATI, new ScriptData(8, 0x0A80));
		scripts.put(ORIYA, new ScriptData(16, 0x0B00));
		scripts.put(TAMIL, new ScriptData(32, 0x0B80));
		scripts.put(TELUGU, new ScriptData(64, 0x0C00));
		scripts.put(KANNADA, new ScriptData(128, 0x0C80));
		scripts.put(MALAYALAM, new ScriptData(256, 0x0D00));
		foreach (ScriptData sd in scripts.values())
		{
		  sd.decompMask = new BitArray(0x7F);
		  for (int i = 0; i < decompositions.Length; i++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ch = decompositions[i][0];
			int ch = decompositions[i][0];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int flags = decompositions[i][4];
			int flags = decompositions[i][4];
			if ((flags & sd.flag) != 0)
			{
			  sd.decompMask.Set(ch, true);
			}
		  }
		}
	  }

	  /// <summary>
	  /// Decompositions according to Unicode 5.2, 
	  /// and http://ldc.upenn.edu/myl/IndianScriptsUnicode.html
	  /// 
	  /// Most of these are not handled by unicode normalization anyway.
	  /// 
	  /// The numbers here represent offsets into the respective codepages,
	  /// with -1 representing null and 0xFF representing zero-width joiner.
	  /// 
	  /// the columns are: ch1, ch2, ch3, res, flags
	  /// ch1, ch2, and ch3 are the decomposition
	  /// res is the composition, and flags are the scripts to which it applies.
	  /// </summary>
	  private static readonly int[][] decompositions = {};


	  /// <summary>
	  /// Normalizes input text, and returns the new length.
	  /// The length will always be less than or equal to the existing length.
	  /// </summary>
	  /// <param name="text"> input text </param>
	  /// <param name="len"> valid length </param>
	  /// <returns> normalized length </returns>
	  public virtual int normalize(char[] text, int len)
	  {
		for (int i = 0; i < len; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Character.UnicodeBlock block = Character.UnicodeBlock.of(text[i]);
		  char.UnicodeBlock block = char.UnicodeBlock.of(text[i]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ScriptData sd = scripts.get(block);
		  ScriptData sd = scripts.get(block);
		  if (sd != null)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ch = text[i] - sd.base;
			int ch = text[i] - sd.@base;
			if (sd.decompMask.Get(ch))
			{
			  len = compose(ch, block, sd, text, i, len);
			}
		  }
		}
		return len;
	  }

	  /// <summary>
	  /// Compose into standard form any compositions in the decompositions table.
	  /// </summary>
	  private int compose(int ch0, char.UnicodeBlock block0, ScriptData sd, char[] text, int pos, int len)
	  {
		if (pos + 1 >= len) // need at least 2 chars!
		{
		  return len;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ch1 = text[pos + 1] - sd.base;
		int ch1 = text[pos + 1] - sd.@base;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Character.UnicodeBlock block1 = Character.UnicodeBlock.of(text[pos + 1]);
		char.UnicodeBlock block1 = char.UnicodeBlock.of(text[pos + 1]);
		if (block1 != block0) // needs to be the same writing system
		{
		  return len;
		}

		int ch2 = -1;

		if (pos + 2 < len)
		{
		  ch2 = text[pos + 2] - sd.@base;
		  char.UnicodeBlock block2 = char.UnicodeBlock.of(text[pos + 2]);
		  if (text[pos + 2] == '\u200D') // ZWJ
		  {
			ch2 = 0xFF;
		  }
		  else if (block2 != block1) // still allow a 2-char match
		  {
			ch2 = -1;
		  }
		}

		for (int i = 0; i < decompositions.Length; i++)
		{
		  if (decompositions[i][0] == ch0 && (decompositions[i][4] & sd.flag) != 0)
		  {
			if (decompositions[i][1] == ch1 && (decompositions[i][2] < 0 || decompositions[i][2] == ch2))
			{
			  text[pos] = (char)(sd.@base + decompositions[i][3]);
			  len = StemmerUtil.delete(text, pos + 1, len);
			  if (decompositions[i][2] >= 0)
			  {
				len = StemmerUtil.delete(text, pos + 1, len);
			  }
			  return len;
			}
		  }
		}

		return len;
	  }
	}

}