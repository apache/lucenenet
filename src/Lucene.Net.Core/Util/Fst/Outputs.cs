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
    /// Represents the outputs for an FST, providing the basic
    /// algebra required for building and traversing the FST.
    ///
    /// <p>Note that any operation that returns NO_OUTPUT must
    /// return the same singleton object from {@link
    /// #getNoOutput}.</p>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class Outputs<T>
    {
        // TODO: maybe change this API to allow for re-use of the
        // output instances -- this is an insane amount of garbage
        // (new object per byte/char/int) if eg used during
        // analysis

        /// <summary>
        /// Eg common("foobar", "food") -> "foo" </summary>
        public abstract T Common(T output1, T output2);

        /// <summary>
        /// Eg subtract("foobar", "foo") -> "bar" </summary>
        public abstract T Subtract(T output, T inc);

        /// <summary>
        /// Eg add("foo", "bar") -> "foobar" </summary>
        public abstract T Add(T prefix, T output);

        /// <summary>
        /// Encode an output value into a <seealso cref="DataOutput"/>. </summary>
        public abstract void Write(T output, DataOutput @out);

        /// <summary>
        /// Encode an final node output value into a {@link
        ///  DataOutput}.  By default this just calls {@link #write(Object,
        ///  DataOutput)}.
        /// </summary>
        public virtual void WriteFinalOutput(T output, DataOutput @out)
        {
            Write(output, @out);
        }

        /// <summary>
        /// Decode an output value previously written with {@link
        ///  #write(Object, DataOutput)}.
        /// </summary>
        public abstract T Read(DataInput @in);

        /// <summary>
        /// Decode an output value previously written with {@link
        ///  #writeFinalOutput(Object, DataOutput)}.  By default this
        ///  just calls <seealso cref="#read(DataInput)"/>.
        /// </summary>
        public virtual T ReadFinalOutput(DataInput @in)
        {
            return Read(@in);
        }

        /// <summary>
        /// NOTE: this output is compared with == so you must
        ///  ensure that all methods return the single object if
        ///  it's really no output
        /// </summary>
        public abstract T NoOutput { get; }

        public abstract string OutputToString(T output);

        // TODO: maybe make valid(T output) public...?  for asserts

        public virtual T Merge(T first, T second)
        {
            throw new System.NotSupportedException();
        }
    }
}