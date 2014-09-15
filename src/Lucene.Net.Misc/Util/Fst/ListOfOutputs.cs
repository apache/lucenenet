using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
	/// Wraps another Outputs implementation and encodes one or
	/// more of its output values.  You can use this when a single
	/// input may need to map to more than one output,
	/// maintaining order: pass the same input with a different
	/// output by calling <seealso cref="Builder#add(IntsRef,Object)"/> multiple
	/// times.  The builder will then combine the outputs using
	/// the <seealso cref="Outputs#merge(Object,Object)"/> method.
	/// 
	/// <para>The resulting FST may not be minimal when an input has
	/// more than one output, as this requires pushing all
	/// multi-output values to a final state.
	/// 
	/// </para>
	/// <para>NOTE: the only way to create multiple outputs is to
	/// add the same input to the FST multiple times in a row.  This is
	/// how the FST maps a single input to multiple outputs (e.g. you
	/// cannot pass a List&lt;Object&gt; to <seealso cref="Builder#add"/>).  If
	/// your outputs are longs, and you need at most 2, then use
	/// <seealso cref="UpToTwoPositiveIntOutputs"/> instead since it stores
	/// the outputs more compactly (by stealing a bit from each
	/// long value).
	/// 
	/// </para>
	/// <para>NOTE: this cannot wrap itself (ie you cannot make an
	/// FST with List&lt;List&lt;Object&gt;&gt; outputs using this).
	/// 
	/// @lucene.experimental
	/// </para>
	/// </summary>


	// NOTE: i think we could get a more compact FST if, instead
	// of adding the same input multiple times with a different
	// output each time, we added it only once with a
	// pre-constructed List<T> output.  This way the "multiple
	// values" is fully opaque to the Builder/FST.  It would
	// require implementing the full algebra using set
	// arithmetic (I think?); maybe SetOfOutputs is a good name.

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") public final class ListOfOutputs<T> extends Outputs<Object>
	public sealed class ListOfOutputs<T> : Outputs<object>
	{

	  private readonly Outputs<T> outputs;

	  public ListOfOutputs(Outputs<T> outputs)
	  {
		this.outputs = outputs;
	  }

	  public override object common(object output1, object output2)
	  {
		// These will never be a list:
		return outputs.common((T) output1, (T) output2);
	  }

	  public override object subtract(object @object, object inc)
	  {
		// These will never be a list:
		return outputs.subtract((T) @object, (T) inc);
	  }

	  public override object add(object prefix, object output)
	  {
		Debug.Assert(!(prefix is IList));
		if (!(output is IList))
		{
		  return outputs.add((T) prefix, (T) output);
		}
		else
		{
		  IList<T> outputList = (IList<T>) output;
		  IList<T> addedList = new List<T>(outputList.Count);
		  foreach (T _output in outputList)
		  {
			addedList.Add(outputs.add((T) prefix, _output));
		  }
		  return addedList;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void write(Object output, org.apache.lucene.store.DataOutput out) throws java.io.IOException
	  public override void write(object output, DataOutput @out)
	  {
		Debug.Assert(!(output is IList));
		outputs.write((T) output, @out);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void writeFinalOutput(Object output, org.apache.lucene.store.DataOutput out) throws java.io.IOException
	  public override void writeFinalOutput(object output, DataOutput @out)
	  {
		if (!(output is IList))
		{
		  @out.writeVInt(1);
		  outputs.write((T) output, @out);
		}
		else
		{
		  IList<T> outputList = (IList<T>) output;
		  @out.writeVInt(outputList.Count);
		  foreach (T eachOutput in outputList)
		  {
			outputs.write(eachOutput, @out);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Object read(org.apache.lucene.store.DataInput in) throws java.io.IOException
	  public override object read(DataInput @in)
	  {
		return outputs.read(@in);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Object readFinalOutput(org.apache.lucene.store.DataInput in) throws java.io.IOException
	  public override object readFinalOutput(DataInput @in)
	  {
		int count = @in.readVInt();
		if (count == 1)
		{
		  return outputs.read(@in);
		}
		else
		{
		  IList<T> outputList = new List<T>(count);
		  for (int i = 0;i < count;i++)
		  {
			outputList.Add(outputs.read(@in));
		  }
		  return outputList;
		}
	  }

	  public override object NoOutput
	  {
		  get
		  {
			return outputs.NoOutput;
		  }
	  }

	  public override string outputToString(object output)
	  {
		if (!(output is IList))
		{
		  return outputs.outputToString((T) output);
		}
		else
		{
		  IList<T> outputList = (IList<T>) output;

		  StringBuilder b = new StringBuilder();
		  b.Append('[');

		  for (int i = 0;i < outputList.Count;i++)
		  {
			if (i > 0)
			{
			  b.Append(", ");
			}
			b.Append(outputs.outputToString(outputList[i]));
		  }
		  b.Append(']');
		  return b.ToString();
		}
	  }

	  public override object merge(object first, object second)
	  {
		IList<T> outputList = new List<T>();
		if (!(first is IList))
		{
		  outputList.Add((T) first);
		}
		else
		{
		  outputList.AddRange((IList<T>) first);
		}
		if (!(second is IList))
		{
		  outputList.Add((T) second);
		}
		else
		{
		  outputList.AddRange((IList<T>) second);
		}
		//System.out.println("MERGE: now " + outputList.size() + " first=" + outputToString(first) + " second=" + outputToString(second));
		//System.out.println("  return " + outputToString(outputList));
		return outputList;
	  }

	  public override string ToString()
	  {
		return "OneOrMoreOutputs(" + outputs + ")";
	  }

	  public IList<T> asList(object output)
	  {
		if (!(output is IList))
		{
		  IList<T> result = new List<T>(1);
		  result.Add((T) output);
		  return result;
		}
		else
		{
		  return (IList<T>) output;
		}
	  }
	}

}