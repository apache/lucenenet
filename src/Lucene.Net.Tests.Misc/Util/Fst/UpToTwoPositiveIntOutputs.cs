/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util.Fst;
using Sharpen;

namespace Org.Apache.Lucene.Util.Fst
{
	/// <summary>
	/// An FST
	/// <see cref="Outputs{T}">Outputs&lt;T&gt;</see>
	/// implementation where each output
	/// is one or two non-negative long values.  If it's a
	/// single output, Long is returned; else, TwoLongs.  Order
	/// is preserved in the TwoLongs case, ie .first is the first
	/// input/output added to Builder, and .second is the
	/// second.  You cannot store 0 output with this (that's
	/// reserved to mean "no output")!
	/// <p>NOTE: the only way to create a TwoLongs output is to
	/// add the same input to the FST twice in a row.  This is
	/// how the FST maps a single input to two outputs (e.g. you
	/// cannot pass a TwoLongs to
	/// <see cref="Builder{T}.Add(Org.Apache.Lucene.Util.IntsRef, object)">Builder&lt;T&gt;.Add(Org.Apache.Lucene.Util.IntsRef, object)
	/// 	</see>
	/// .  If you
	/// need more than two then use
	/// <see cref="ListOfOutputs{T}">ListOfOutputs&lt;T&gt;</see>
	/// , but if
	/// you only have at most 2 then this implementation will
	/// require fewer bytes as it steals one bit from each long
	/// value.
	/// <p>NOTE: the resulting FST is not guaranteed to be minimal!
	/// See
	/// <see cref="Builder{T}">Builder&lt;T&gt;</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class UpToTwoPositiveIntOutputs : Outputs<object>
	{
		/// <summary>Holds two long outputs.</summary>
		/// <remarks>Holds two long outputs.</remarks>
		public sealed class TwoLongs
		{
			public readonly long first;

			public readonly long second;

			public TwoLongs(long first, long second)
			{
				this.first = first;
				this.second = second;
			}

			//HM:revisit
			public override string ToString()
			{
				return "TwoLongs:" + first + "," + second;
			}

			public override bool Equals(object _other)
			{
				if (_other is UpToTwoPositiveIntOutputs.TwoLongs)
				{
					UpToTwoPositiveIntOutputs.TwoLongs other = (UpToTwoPositiveIntOutputs.TwoLongs)_other;
					return first == other.first && second == other.second;
				}
				else
				{
					return false;
				}
			}

			public override int GetHashCode()
			{
				return (int)((first ^ ((long)(((ulong)first) >> 32))) ^ (second ^ (second >> 32))
					);
			}
		}

		private static readonly long NO_OUTPUT = System.Convert.ToInt64(0);

		private readonly bool doShare;

		private static readonly UpToTwoPositiveIntOutputs singletonShare = new UpToTwoPositiveIntOutputs
			(true);

		private static readonly UpToTwoPositiveIntOutputs singletonNoShare = new UpToTwoPositiveIntOutputs
			(false);

		private UpToTwoPositiveIntOutputs(bool doShare)
		{
			this.doShare = doShare;
		}

		public static UpToTwoPositiveIntOutputs GetSingleton(bool doShare)
		{
			return doShare ? singletonShare : singletonNoShare;
		}

		public long Get(long v)
		{
			if (v == 0)
			{
				return NO_OUTPUT;
			}
			else
			{
				return Sharpen.Extensions.ValueOf(v);
			}
		}

		public UpToTwoPositiveIntOutputs.TwoLongs Get(long first, long second)
		{
			return new UpToTwoPositiveIntOutputs.TwoLongs(first, second);
		}

		public override object Common(object _output1, object _output2)
		{
			//HM:revisit
			long output1 = (long)_output1;
			long output2 = (long)_output2;
			if (output1 == NO_OUTPUT || output2 == NO_OUTPUT)
			{
				return NO_OUTPUT;
			}
			else
			{
				if (doShare)
				{
					return Math.Min(output1, output2);
				}
				else
				{
					if (output1.Equals(output2))
					{
						return output1;
					}
					else
					{
						return NO_OUTPUT;
					}
				}
			}
		}

		public override object Subtract(object _output, object _inc)
		{
			long output = (long)Valid(Valid(_output, false), false);
			long inc = (long)_inc;
			if (output >= inc == NO_OUTPUT)
			{
				return output;
			}
			else
			{
				if (output.Equals(inc))
				{
					return NO_OUTPUT;
				}
				else
				{
					return output - inc;
				}
			}
		}

		public override object Add(object _prefix, object _output)
		{
			long prefix = (long)Valid(Valid(_prefix, false), true);
			if (_output is long)
			{
				long output = (long)_output;
				if (prefix == NO_OUTPUT)
				{
					return output;
				}
				else
				{
					if (output == NO_OUTPUT)
					{
						return prefix;
					}
					else
					{
						return prefix + output;
					}
				}
			}
			else
			{
				UpToTwoPositiveIntOutputs.TwoLongs output = (UpToTwoPositiveIntOutputs.TwoLongs)_output;
				long v = prefix;
				return new UpToTwoPositiveIntOutputs.TwoLongs(output.first + v, output.second + v
					);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(object _output, DataOutput @out)
		{
			if (Valid(_output, true) is long)
			{
				long output = (long)_output;
				@out.WriteVLong(output << 1);
			}
			else
			{
				UpToTwoPositiveIntOutputs.TwoLongs output = (UpToTwoPositiveIntOutputs.TwoLongs)_output;
				@out.WriteVLong((output.first << 1) | 1);
				@out.WriteVLong(output.second);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override object Read(DataInput @in)
		{
			long code = @in.ReadVLong();
			if ((code & 1) == 0)
			{
				// single long
				long v = (long)(((ulong)code) >> 1);
				if (v == 0)
				{
					return NO_OUTPUT;
				}
				else
				{
					return Sharpen.Extensions.ValueOf(v);
				}
			}
			else
			{
				// two longs
				long first = (long)(((ulong)code) >> 1);
				long second = @in.ReadVLong();
				return new UpToTwoPositiveIntOutputs.TwoLongs(first, second);
			}
		}

		private bool Valid(long o)
		{
			//HM:revisit
			return true;
		}

		// Used only by assert
		private bool Valid(object _o, bool allowDouble)
		{
			if (!allowDouble)
			{
				return Valid((long)_o is long);
			}
			else
			{
				if (_o is UpToTwoPositiveIntOutputs.TwoLongs)
				{
					return true;
				}
				else
				{
					return Valid((long)_o);
				}
			}
		}

		public override object GetNoOutput()
		{
			return NO_OUTPUT;
		}

		public override string OutputToString(object output)
		{
			return output.ToString();
		}

		public override object Merge(object first, object second)
		{
			return new UpToTwoPositiveIntOutputs.TwoLongs((long)Valid(Valid(first, false), false
				), (long)second);
		}
	}
}
