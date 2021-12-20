using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// Wraps another Outputs implementation and encodes one or
    /// more of its output values.  You can use this when a single
    /// input may need to map to more than one output,
    /// maintaining order: pass the same input with a different
    /// output by calling <see cref="Builder{T}.Add(Int32sRef,T)"/> multiple
    /// times.  The builder will then combine the outputs using
    /// the <see cref="Outputs{T}.Merge(T,T)"/> method.
    /// 
    /// <para>The resulting FST may not be minimal when an input has
    /// more than one output, as this requires pushing all
    /// multi-output values to a final state.
    /// 
    /// </para>
    /// <para>NOTE: the only way to create multiple outputs is to
    /// add the same input to the FST multiple times in a row.  This is
    /// how the FST maps a single input to multiple outputs (e.g. you
    /// cannot pass a List&lt;Object&gt; to <see cref="Builder{T}.Add(Int32sRef, T)"/>).  If
    /// your outputs are longs, and you need at most 2, then use
    /// <see cref="UpToTwoPositiveInt64Outputs"/> instead since it stores
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
    public sealed class ListOfOutputs<T> : Outputs<object>
    {

        private readonly Outputs<T> outputs;

        public ListOfOutputs(Outputs<T> outputs)
        {
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
            if (Debugging.AssertsEnabled) Debugging.Assert(!(prefix is IList));
            if (!(output is IList outputList))
            {
                return outputs.Add((T)prefix, (T)output);
            }
            else
            {
                IList<T> addedList = new JCG.List<T>(outputList.Count);
                foreach (object _output in outputList)
                {
                    addedList.Add(outputs.Add((T)prefix, (T)_output));
                }
                return addedList;
            }
        }

        public override void Write(object output, DataOutput @out)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!(output is IList));
            outputs.Write((T)output, @out);
        }

        public override void WriteFinalOutput(object output, DataOutput @out)
        {
            if (!(output is IList outputList))
            {
                @out.WriteVInt32(1);
                outputs.Write((T)output, @out);
            }
            else
            {
                @out.WriteVInt32(outputList.Count);
                foreach (var eachOutput in outputList)
                {
                    outputs.Write((T)eachOutput, @out);
                }
            }
        }

        public override object Read(DataInput @in)
        {
            return outputs.Read(@in);
        }

        public override object ReadFinalOutput(DataInput @in)
        {
            int count = @in.ReadVInt32();
            if (count == 1)
            {
                return outputs.Read(@in);
            }
            else
            {
                IList<T> outputList = new JCG.List<T>(count);
                for (int i = 0; i < count; i++)
                {
                    outputList.Add(outputs.Read(@in));
                }
                return outputList;
            }
        }

        public override object NoOutput => outputs.NoOutput;

        public override string OutputToString(object output)
        {
            if (!(output is IList outputList))
            {
                return outputs.OutputToString((T)output);
            }
            else
            {
                StringBuilder b = new StringBuilder();
                b.Append('[');

                for (int i = 0; i < outputList.Count; i++)
                {
                    if (i > 0)
                    {
                        b.Append(", ");
                    }
                    b.Append(outputs.OutputToString((T)outputList[i]));
                }
                b.Append(']');
                return b.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object Merge(object first, object second)
        {
            IList<T> outputList = new JCG.List<T>();
            if (!(first is IList firstList))
            {
                outputList.Add((T)first);
            }
            else
            {
                foreach (object value in firstList)
                {
                    outputList.Add((T)value);
                }
            }
            if (!(second is IList secondList))
            {
                outputList.Add((T)second);
            }
            else
            {
                foreach (object value in secondList)
                {
                    outputList.Add((T)value);
                }
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
            if (!(output is IList<T> outputList))
            {
                return new JCG.List<T>(1) { (T)output };
            }
            else
            {
                return outputList;
            }
        }
    }
}