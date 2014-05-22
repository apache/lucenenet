using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
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

	using DataInput = Lucene.Net.Store.DataInput;
	using DataOutput = Lucene.Net.Store.DataOutput;

	/// <summary>
	/// An FST <seealso cref="Outputs"/> implementation where each output
	/// is a sequence of characters.
	/// 
	/// @lucene.experimental
	/// </summary>

	public sealed class CharSequenceOutputs : Outputs<CharsRef>
	{

	  private static readonly CharsRef NO_OUTPUT = new CharsRef();
	  private static readonly CharSequenceOutputs Singleton_Renamed = new CharSequenceOutputs();

	  private CharSequenceOutputs()
	  {
	  }

	  public static CharSequenceOutputs Singleton
	  {
		  get
		  {
			return Singleton_Renamed;
		  }
	  }

	  public override CharsRef Common(CharsRef output1, CharsRef output2)
	  {
		Debug.Assert(output1 != null);
		Debug.Assert(output2 != null);

		int pos1 = output1.Offset;
		int pos2 = output2.Offset;
		int stopAt1 = pos1 + Math.Min(output1.Length_Renamed, output2.Length_Renamed);
		while (pos1 < stopAt1)
		{
		  if (output1.Chars[pos1] != output2.Chars[pos2])
		  {
			break;
		  }
		  pos1++;
		  pos2++;
		}

		if (pos1 == output1.Offset)
		{
		  // no common prefix
		  return NO_OUTPUT;
		}
		else if (pos1 == output1.Offset + output1.Length_Renamed)
		{
		  // output1 is a prefix of output2
		  return output1;
		}
		else if (pos2 == output2.Offset + output2.Length_Renamed)
		{
		  // output2 is a prefix of output1
		  return output2;
		}
		else
		{
		  return new CharsRef(output1.Chars, output1.Offset, pos1 - output1.Offset);
		}
	  }

	  public override CharsRef Subtract(CharsRef output, CharsRef inc)
	  {
		Debug.Assert(output != null);
		Debug.Assert(inc != null);
		if (inc == NO_OUTPUT)
		{
		  // no prefix removed
		  return output;
		}
		else if (inc.Length_Renamed == output.Length_Renamed)
		{
		  // entire output removed
		  return NO_OUTPUT;
		}
		else
		{
		  Debug.Assert(inc.Length_Renamed < output.Length_Renamed, "inc.length=" + inc.Length_Renamed + " vs output.length=" + output.Length_Renamed);
		  Debug.Assert(inc.Length_Renamed > 0);
		  return new CharsRef(output.Chars, output.Offset + inc.Length_Renamed, output.Length_Renamed - inc.Length_Renamed);
		}
	  }

	  public override CharsRef Add(CharsRef prefix, CharsRef output)
	  {
		Debug.Assert(prefix != null);
		Debug.Assert(output != null);
		if (prefix == NO_OUTPUT)
		{
		  return output;
		}
		else if (output == NO_OUTPUT)
		{
		  return prefix;
		}
		else
		{
		  Debug.Assert(prefix.Length_Renamed > 0);
		  Debug.Assert(output.Length_Renamed > 0);
		  CharsRef result = new CharsRef(prefix.Length_Renamed + output.Length_Renamed);
		  Array.Copy(prefix.Chars, prefix.Offset, result.Chars, 0, prefix.Length_Renamed);
		  Array.Copy(output.Chars, output.Offset, result.Chars, prefix.Length_Renamed, output.Length_Renamed);
		  result.Length_Renamed = prefix.Length_Renamed + output.Length_Renamed;
		  return result;
		}
	  }

	  public override void Write(CharsRef prefix, DataOutput @out)
	  {
		Debug.Assert(prefix != null);
		@out.WriteVInt(prefix.Length_Renamed);
		// TODO: maybe UTF8?
		for (int idx = 0;idx < prefix.Length_Renamed;idx++)
		{
		  @out.WriteVInt(prefix.Chars[prefix.Offset + idx]);
		}
	  }

	  public override CharsRef Read(DataInput @in)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int len = in.readVInt();
		int len = @in.ReadVInt();
		if (len == 0)
		{
		  return NO_OUTPUT;
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.CharsRef output = new Lucene.Net.Util.CharsRef(len);
		  CharsRef output = new CharsRef(len);
		  for (int idx = 0;idx < len;idx++)
		  {
			output.Chars[idx] = (char) @in.ReadVInt();
		  }
		  output.Length_Renamed = len;
		  return output;
		}
	  }

	  public override CharsRef NoOutput
	  {
		  get
		  {
			return NO_OUTPUT;
		  }
	  }

	  public override string OutputToString(CharsRef output)
	  {
		return output.ToString();
	  }
	}

}