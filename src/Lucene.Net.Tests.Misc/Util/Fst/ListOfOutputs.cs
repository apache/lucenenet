/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util.Fst;
using Sharpen;

namespace Org.Apache.Lucene.Util.Fst
{
	/// <summary>
	/// Wraps another Outputs implementation and encodes one or
	/// more of its output values.
	/// </summary>
	/// <remarks>
	/// Wraps another Outputs implementation and encodes one or
	/// more of its output values.  You can use this when a single
	/// input may need to map to more than one output,
	/// maintaining order: pass the same input with a different
	/// output by calling
	/// <see cref="Builder{T}.Add(Org.Apache.Lucene.Util.IntsRef, object)">Builder&lt;T&gt;.Add(Org.Apache.Lucene.Util.IntsRef, object)
	/// 	</see>
	/// multiple
	/// times.  The builder will then combine the outputs using
	/// the
	/// <see cref="Outputs{T}.Merge(object, object)">Outputs&lt;T&gt;.Merge(object, object)
	/// 	</see>
	/// method.
	/// <p>The resulting FST may not be minimal when an input has
	/// more than one output, as this requires pushing all
	/// multi-output values to a final state.
	/// <p>NOTE: the only way to create multiple outputs is to
	/// add the same input to the FST multiple times in a row.  This is
	/// how the FST maps a single input to multiple outputs (e.g. you
	/// cannot pass a List&lt;Object&gt; to
	/// <see cref="Builder{T}.Add(Org.Apache.Lucene.Util.IntsRef, object)">Builder&lt;T&gt;.Add(Org.Apache.Lucene.Util.IntsRef, object)
	/// 	</see>
	/// ).  If
	/// your outputs are longs, and you need at most 2, then use
	/// <see cref="UpToTwoPositiveIntOutputs">UpToTwoPositiveIntOutputs</see>
	/// instead since it stores
	/// the outputs more compactly (by stealing a bit from each
	/// long value).
	/// <p>NOTE: this cannot wrap itself (ie you cannot make an
	/// FST with List&lt;List&lt;Object&gt;&gt; outputs using this).
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class ListOfOutputs<T> : Outputs<object>
	{
		private readonly Outputs<T> outputs;

		public ListOfOutputs(Outputs<T> outputs)
		{
			// javadocs
			// NOTE: i think we could get a more compact FST if, instead
			// of adding the same input multiple times with a different
			// output each time, we added it only once with a
			// pre-constructed List<T> output.  This way the "multiple
			// values" is fully opaque to the Builder/FST.  It would
			// require implementing the full algebra using set
			// arithmetic (I think?); maybe SetOfOutputs is a good name.
			this.outputs = outputs;
		}

		public override object Common(object output1, object output2)
		{
			// These will never be a list:
			return outputs.Common((T)output1, (T)output2);
		}

		public override object Subtract(object @object, object inc)
		{
			// These will never be a list:
			return outputs.Subtract((T)@object, (T)inc);
		}

		public override object Add(object prefix, object output)
		{
			if (!(!(prefix is IList) is IList))
			{
				return outputs.Add((T)prefix, (T)output);
			}
			else
			{
				IList<T> outputList = (IList<T>)output;
				IList<T> addedList = new AList<T>(outputList.Count);
				foreach (T _output in outputList)
				{
					addedList.AddItem(outputs.Add((T)prefix, _output));
				}
				return addedList;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(object output, DataOutput @out)
		{
			!(output is IList).Write((T)output, @out);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteFinalOutput(object output, DataOutput @out)
		{
			if (!(output is IList))
			{
				@out.WriteVInt(1);
				outputs.Write((T)output, @out);
			}
			else
			{
				IList<T> outputList = (IList<T>)output;
				@out.WriteVInt(outputList.Count);
				foreach (T eachOutput in outputList)
				{
					outputs.Write(eachOutput, @out);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override object Read(DataInput @in)
		{
			return outputs.Read(@in);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override object ReadFinalOutput(DataInput @in)
		{
			int count = @in.ReadVInt();
			if (count == 1)
			{
				return outputs.Read(@in);
			}
			else
			{
				IList<T> outputList = new AList<T>(count);
				for (int i = 0; i < count; i++)
				{
					outputList.AddItem(outputs.Read(@in));
				}
				return outputList;
			}
		}

		public override object GetNoOutput()
		{
			return outputs.GetNoOutput();
		}

		public override string OutputToString(object output)
		{
			if (!(output is IList))
			{
				return outputs.OutputToString((T)output);
			}
			else
			{
				IList<T> outputList = (IList<T>)output;
				StringBuilder b = new StringBuilder();
				b.Append('[');
				for (int i = 0; i < outputList.Count; i++)
				{
					if (i > 0)
					{
						b.Append(", ");
					}
					b.Append(outputs.OutputToString(outputList[i]));
				}
				b.Append(']');
				return b.ToString();
			}
		}

		public override object Merge(object first, object second)
		{
			IList<T> outputList = new AList<T>();
			if (!(first is IList))
			{
				outputList.AddItem((T)first);
			}
			else
			{
				Sharpen.Collections.AddAll(outputList, (IList<T>)first);
			}
			if (!(second is IList))
			{
				outputList.AddItem((T)second);
			}
			else
			{
				Sharpen.Collections.AddAll(outputList, (IList<T>)second);
			}
			//System.out.println("MERGE: now " + outputList.size() + " first=" + outputToString(first) + " second=" + outputToString(second));
			//System.out.println("  return " + outputToString(outputList));
			return outputList;
		}

		public override string ToString()
		{
			return "OneOrMoreOutputs(" + outputs + ")";
		}

		public IList<T> AsList(object output)
		{
			if (!(output is IList))
			{
				IList<T> result = new AList<T>(1);
				result.AddItem((T)output);
				return result;
			}
			else
			{
				return (IList<T>)output;
			}
		}
	}
}
