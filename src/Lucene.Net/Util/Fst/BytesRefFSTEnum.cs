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
    /// Enumerates all input (<see cref="BytesRef"/>) + output pairs in an
    /// FST.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class BytesRefFSTEnum<T> : FSTEnum<T> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
    {
        private readonly BytesRef current = new BytesRef(10);
        private readonly BytesRefFSTEnum.InputOutput<T> result = new BytesRefFSTEnum.InputOutput<T>();
        private BytesRef target;

        // LUCENENET NOTE: InputOutput<T> was moved to the BytesRefFSTEnum class

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        /// doFloor is true, advance positions to the biggest
        /// term before target.
        /// </summary>
        public BytesRefFSTEnum(FST<T> fst)
            : base(fst)
        {
            result.Input = current;
            current.Offset = 1;
        }

        public BytesRefFSTEnum.InputOutput<T> Current => result;

        public bool MoveNext()
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
        public BytesRefFSTEnum.InputOutput<T> Next()
        {
            //System.out.println("  enum.next");
            if (MoveNext())
                return result;
            return null;
        }

        /// <summary>
        /// Seeks to smallest term that's &gt;= target. </summary>
        public BytesRefFSTEnum.InputOutput<T> SeekCeil(BytesRef target)
        {
            this.target = target;
            m_targetLength = target.Length;
            base.DoSeekCeil();
            return SetResult();
        }

        /// <summary>
        /// Seeks to biggest term that's &lt;= target. </summary>
        public BytesRefFSTEnum.InputOutput<T> SeekFloor(BytesRef target)
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
        public BytesRefFSTEnum.InputOutput<T> SeekExact(BytesRef target)
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
                    return target.Bytes[target.Offset + m_upto - 1] & 0xFF;
                }
            }
        }

        protected override int CurrentLabel
        {   // current.offset fixed at 1
            get => current.Bytes[m_upto] & 0xFF;
            set => current.Bytes[m_upto] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Grow()
        {
            current.Bytes = ArrayUtil.Grow(current.Bytes, m_upto + 1);
        }

        private BytesRefFSTEnum.InputOutput<T> SetResult()
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
    /// (i.e. BytesRefFSTEnum.InputOutput{T} rather than BytesRefFSTEnum{T}.InputOutput{T})
    /// </summary>
    public sealed class BytesRefFSTEnum
    {
        private BytesRefFSTEnum()
        { }

        /// <summary>
        /// Holds a single input (<see cref="BytesRef"/>) + output pair. </summary>
        public class InputOutput<T>
        {
            public BytesRef Input { get; set; }
            public T Output { get; set; }
        }
    }
}