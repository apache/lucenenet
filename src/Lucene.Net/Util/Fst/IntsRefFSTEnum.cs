using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;

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
    /// Enumerates all input (<see cref="Int32sRef"/>) + output pairs in an
    /// FST.
    /// <para/>
    /// NOTE: This was IntsRefFSTEnum{T} in Lucene
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class Int32sRefFSTEnum<T> : FSTEnum<T> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
    {
        private readonly Int32sRef current = new Int32sRef(10);
        private readonly Int32sRefFSTEnum.InputOutput<T> result = new Int32sRefFSTEnum.InputOutput<T>();
        private Int32sRef target;

        // LUCENENET NOTE: The InputOutput<T> class was moved into the IntsRefFSTEnum class

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        /// doFloor is true, advance positions to the biggest
        /// term before target.
        /// </summary>
        public Int32sRefFSTEnum(FST<T> fst)
            : base(fst)
        {
            result.Input = current;
            current.Offset = 1;
        }

        public Int32sRefFSTEnum.InputOutput<T> Current => result;

        public bool MoveNext() // LUCENENET specific - replaced Next() with MoveNext()
        {
            //System.out.println("  enum.next");
            DoNext();

            if (m_upto == 0)
            {
                return false;
            }
            else
            {
                current.Length = m_upto - 1;
                result.Output = m_output[m_upto];
                return true;
            }
        }

        [Obsolete("Use MoveNext() and Current instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Int32sRefFSTEnum.InputOutput<T> Next()
        {
            //System.out.println("  enum.next");
            DoNext();
            return SetResult();
        }

        /// <summary>
        /// Seeks to smallest term that's &gt;= target. </summary>
        public Int32sRefFSTEnum.InputOutput<T> SeekCeil(Int32sRef target)
        {
            this.target = target;
            m_targetLength = target.Length;
            base.DoSeekCeil();
            return SetResult();
        }

        /// <summary>
        /// Seeks to biggest term that's &lt;= target. </summary>
        public Int32sRefFSTEnum.InputOutput<T> SeekFloor(Int32sRef target)
        {
            this.target = target;
            m_targetLength = target.Length;
            base.DoSeekFloor();
            return SetResult();
        }

        /// <summary>
        /// Seeks to exactly this term, returning <c>null</c> if the term
        /// doesn't exist.  This is faster than using 
        /// <see cref="SeekFloor"/> or <see cref="SeekCeil"/> because it
        /// short-circuits as soon the match is not found.
        /// </summary>
        public Int32sRefFSTEnum.InputOutput<T> SeekExact(Int32sRef target)
        {
            this.target = target;
            m_targetLength = target.Length;
            if (base.DoSeekExact())
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(m_upto == 1 + target.Length);
                return SetResult();
            }
            else
            {
                return null;
            }
        }

        protected override int TargetLabel
        {
            get
            {
                if (m_upto - 1 == target.Length)
                {
                    return FST.END_LABEL;
                }
                else
                {
                    return target.Int32s[target.Offset + m_upto - 1];
                }
            }
        }

        protected override int CurrentLabel
        {   // current.offset fixed at 1
            get => current.Int32s[m_upto];
            set => current.Int32s[m_upto] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Grow()
        {
            current.Int32s = ArrayUtil.Grow(current.Int32s, m_upto + 1);
        }

        private Int32sRefFSTEnum.InputOutput<T> SetResult()
        {
            if (m_upto == 0)
            {
                return null;
            }
            else
            {
                current.Length = m_upto - 1;
                result.Output = m_output[m_upto];
                return result;
            }
        }
    }

    /// <summary>
    /// LUCENENET specific. This class is to mimic Java's ability to specify
    /// nested classes of Generics without having to specify the generic type
    /// (i.e. <c>Int32sRefFSTEnum.InputOutput{T}</c> rather than <c>Int32sRefFSTEnum{T}.InputOutput{T}</c>)
    /// <para/>
    /// NOTE: This was Int32sRefFSTEnum{T} in Lucene
    /// </summary>
    public sealed class Int32sRefFSTEnum
    {
        private Int32sRefFSTEnum()
        { }

        /// <summary>
        /// Holds a single input (<see cref="Int32sRef"/>) + output pair. </summary>
        public class InputOutput<T> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            public Int32sRef Input { get; set; }
            public T Output { get; set; }
        }
    }
}