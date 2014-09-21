using System;
using System.Diagnostics;

namespace org.apache.lucene.util.fst
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

	using DataInput = org.apache.lucene.store.DataInput;
	using DataOutput = org.apache.lucene.store.DataOutput;

	/// <summary>
	/// An FST <seealso cref="Outputs"/> implementation where each output
	/// is one or two non-negative long values.  If it's a
	/// single output, Long is returned; else, TwoLongs.  Order
	/// is preserved in the TwoLongs case, ie .first is the first
	/// input/output added to Builder, and .second is the
	/// second.  You cannot store 0 output with this (that's
	/// reserved to mean "no output")!
	/// 
	/// <para>NOTE: the only way to create a TwoLongs output is to
	/// add the same input to the FST twice in a row.  This is
	/// how the FST maps a single input to two outputs (e.g. you
	/// cannot pass a TwoLongs to <seealso cref="Builder#add"/>.  If you
	/// need more than two then use <seealso cref="ListOfOutputs"/>, but if
	/// you only have at most 2 then this implementation will
	/// require fewer bytes as it steals one bit from each long
	/// value.
	/// 
	/// </para>
	/// <para>NOTE: the resulting FST is not guaranteed to be minimal!
	/// See <seealso cref="Builder"/>.
	/// 
	/// @lucene.experimental
	/// </para>
	/// </summary>

	public sealed class UpToTwoPositiveIntOutputs : Outputs<object>
	{

	  /// <summary>
	  /// Holds two long outputs. </summary>
	  public sealed class TwoLongs
	  {
		public readonly long first;
		public readonly long second;

		public TwoLongs(long first, long second)
		{
		  this.first = first;
		  this.second = second;
		  Debug.Assert(first >= 0);
		  Debug.Assert(second >= 0);
		}

		public override string ToString()
		{
		  return "TwoLongs:" + first + "," + second;
		}

		public override bool Equals(object _other)
		{
		  if (_other is TwoLongs)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TwoLongs other = (TwoLongs) _other;
			TwoLongs other = (TwoLongs) _other;
			return first == other.first && second == other.second;
		  }
		  else
		  {
			return false;
		  }
		}

		public override int GetHashCode()
		{
		  return (int)((first ^ ((long)((ulong)first >> 32))) ^ (second ^ (second >> 32)));
		}
	  }

	  private static readonly long? NO_OUTPUT = new long?(0);

	  private readonly bool doShare;

	  private static readonly UpToTwoPositiveIntOutputs singletonShare = new UpToTwoPositiveIntOutputs(true);
	  private static readonly UpToTwoPositiveIntOutputs singletonNoShare = new UpToTwoPositiveIntOutputs(false);

	  private UpToTwoPositiveIntOutputs(bool doShare)
	  {
		this.doShare = doShare;
	  }

	  public static UpToTwoPositiveIntOutputs getSingleton(bool doShare)
	  {
		return doShare ? singletonShare : singletonNoShare;
	  }

	  public long? get(long v)
	  {
		if (v == 0)
		{
		  return NO_OUTPUT;
		}
		else
		{
		  return Convert.ToInt64(v);
		}
	  }

	  public TwoLongs get(long first, long second)
	  {
		return new TwoLongs(first, second);
	  }

	  public override long? common(object _output1, object _output2)
	  {
		Debug.Assert(valid(_output1, false));
		Debug.Assert(valid(_output2, false));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long output1 = (Long) _output1;
		long? output1 = (long?) _output1;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long output2 = (Long) _output2;
		long? output2 = (long?) _output2;
		if (output1 == NO_OUTPUT || output2 == NO_OUTPUT)
		{
		  return NO_OUTPUT;
		}
		else if (doShare)
		{
		  Debug.Assert(output1 > 0);
		  Debug.Assert(output2 > 0);
		  return Math.Min(output1, output2);
		}
		else if (output1.Equals(output2))
		{
		  return output1;
		}
		else
		{
		  return NO_OUTPUT;
		}
	  }

	  public override long? subtract(object _output, object _inc)
	  {
		Debug.Assert(valid(_output, false));
		Debug.Assert(valid(_inc, false));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long output = (Long) _output;
		long? output = (long?) _output;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long inc = (Long) _inc;
		long? inc = (long?) _inc;
		Debug.Assert(output >= inc);

		if (inc == NO_OUTPUT)
		{
		  return output;
		}
		else if (output.Equals(inc))
		{
		  return NO_OUTPUT;
		}
		else
		{
		  return output - inc;
		}
	  }

	  public override object add(object _prefix, object _output)
	  {
		Debug.Assert(valid(_prefix, false));
		Debug.Assert(valid(_output, true));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long prefix = (Long) _prefix;
		long? prefix = (long?) _prefix;
		if (_output is long?)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long output = (Long) _output;
		  long? output = (long?) _output;
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
			return prefix + output;
		  }
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TwoLongs output = (TwoLongs) _output;
		  TwoLongs output = (TwoLongs) _output;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long v = prefix;
		  long v = prefix.Value;
		  return new TwoLongs(output.first + v, output.second + v);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void write(Object _output, org.apache.lucene.store.DataOutput out) throws java.io.IOException
	  public override void write(object _output, DataOutput @out)
	  {
		Debug.Assert(valid(_output, true));
		if (_output is long?)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long output = (Long) _output;
		  long? output = (long?) _output;
		  @out.writeVLong(output << 1);
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TwoLongs output = (TwoLongs) _output;
		  TwoLongs output = (TwoLongs) _output;
		  @out.writeVLong((output.first << 1) | 1);
		  @out.writeVLong(output.second);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Object read(org.apache.lucene.store.DataInput in) throws java.io.IOException
	  public override object read(DataInput @in)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long code = in.readVLong();
		long code = @in.readVLong();
		if ((code & 1) == 0)
		{
		  // single long
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long v = code >>> 1;
		  long v = (long)((ulong)code >> 1);
		  if (v == 0)
		  {
			return NO_OUTPUT;
		  }
		  else
		  {
			return Convert.ToInt64(v);
		  }
		}
		else
		{
		  // two longs
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long first = code >>> 1;
		  long first = (long)((ulong)code >> 1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long second = in.readVLong();
		  long second = @in.readVLong();
		  return new TwoLongs(first, second);
		}
	  }

	  private bool valid(long? o)
	  {
		Debug.Assert(o != null);
		Debug.Assert(o is long?);
		Debug.Assert(o == NO_OUTPUT || o > 0);
		return true;
	  }

	  // Used only by assert
	  private bool valid(object _o, bool allowDouble)
	  {
		if (!allowDouble)
		{
		  Debug.Assert(_o is long?);
		  return valid((long?) _o);
		}
		else if (_o is TwoLongs)
		{
		  return true;
		}
		else
		{
		  return valid((long?) _o);
		}
	  }

	  public override object NoOutput
	  {
		  get
		  {
			return NO_OUTPUT;
		  }
	  }

	  public override string outputToString(object output)
	  {
		return output.ToString();
	  }

	  public override object merge(object first, object second)
	  {
		Debug.Assert(valid(first, false));
		Debug.Assert(valid(second, false));
		return new TwoLongs((long?) first, (long?) second);
	  }
	}

}