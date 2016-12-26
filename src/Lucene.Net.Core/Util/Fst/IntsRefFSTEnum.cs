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

    /// <summary>
    /// Enumerates all input (IntsRef) + output pairs in an
    ///  FST.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class IntsRefFSTEnum<T> : FSTEnum<T>
    {
        private readonly IntsRef current = new IntsRef(10);
        private readonly IntsRefFSTEnum.InputOutput<T> result = new IntsRefFSTEnum.InputOutput<T>();
        private IntsRef target;

        // LUCENENET NOTE: The InputOutput<T> class was moved into the IntsRefFSTEnum class

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        ///  doFloor is true, advance positions to the biggest
        ///  term before target.
        /// </summary>
        public IntsRefFSTEnum(FST<T> fst)
            : base(fst)
        {
            result.Input = current;
            current.Offset = 1;
        }

        public IntsRefFSTEnum.InputOutput<T> Current() // LUCENENET TODO: make property
        {
            return result;
        }

        public IntsRefFSTEnum.InputOutput<T> Next()
        {
            //System.out.println("  enum.next");
            DoNext();
            return SetResult();
        }

        /// <summary>
        /// Seeks to smallest term that's >= target. </summary>
        public IntsRefFSTEnum.InputOutput<T> SeekCeil(IntsRef target)
        {
            this.target = target;
            targetLength = target.Length;
            base.DoSeekCeil();
            return SetResult();
        }

        /// <summary>
        /// Seeks to biggest term that's <= target. </summary>
        public IntsRefFSTEnum.InputOutput<T> SeekFloor(IntsRef target)
        {
            this.target = target;
            targetLength = target.Length;
            base.DoSeekFloor();
            return SetResult();
        }

        /// <summary>
        /// Seeks to exactly this term, returning null if the term
        ///  doesn't exist.  this is faster than using {@link
        ///  #seekFloor} or <seealso cref="#seekCeil"/> because it
        ///  short-circuits as soon the match is not found.
        /// </summary>
        public IntsRefFSTEnum.InputOutput<T> SeekExact(IntsRef target)
        {
            this.target = target;
            targetLength = target.Length;
            if (base.DoSeekExact())
            {
                Debug.Assert(upto == 1 + target.Length);
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
                if (upto - 1 == target.Length)
                {
                    return FST.END_LABEL;
                }
                else
                {
                    return target.Ints[target.Offset + upto - 1];
                }
            }
        }

        protected override int CurrentLabel
        {
            get
            {
                // current.offset fixed at 1
                return current.Ints[upto];
            }
            set
            {
                current.Ints[upto] = value;
            }
        }

        protected override void Grow()
        {
            current.Ints = ArrayUtil.Grow(current.Ints, upto + 1);
        }

        private IntsRefFSTEnum.InputOutput<T> SetResult()
        {
            if (upto == 0)
            {
                return null;
            }
            else
            {
                current.Length = upto - 1;
                result.Output = output[upto];
                return result;
            }
        }
    }

    /// <summary>
    /// LUCENENET specific. This class is to mimic Java's ability to specify
    /// nested classes of Generics without having to specify the generic type
    /// (i.e. IntsRefFSTEnum.InputOutput{T} rather than IntsRefFSTEnum{T}.InputOutput{T})
    /// </summary>
    public sealed class IntsRefFSTEnum
    {
        private IntsRefFSTEnum()
        { }

        /// <summary>
        /// Holds a single input (IntsRef) + output pair. </summary>
        public class InputOutput<T>
        {
            public IntsRef Input; // LUCENENET TODO: make property
            public T Output; // LUCENENET TODO: make property
        }
    }
}