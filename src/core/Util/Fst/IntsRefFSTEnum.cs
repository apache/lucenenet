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
        private readonly IntsRef Current_Renamed = new IntsRef(10);
        private readonly InputOutput<T> Result = new InputOutput<T>();
        private IntsRef Target;

        /// <summary>
        /// Holds a single input (IntsRef) + output pair. </summary>
        public class InputOutput<T>
        {
            public IntsRef Input;
            public T Output;
        }

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        ///  doFloor is true, advance positions to the biggest
        ///  term before target.
        /// </summary>
        public IntsRefFSTEnum(FST<T> fst)
            : base(fst)
        {
            Result.Input = Current_Renamed;
            Current_Renamed.Offset = 1;
        }

        public InputOutput<T> Current()
        {
            return Result;
        }

        public InputOutput<T> Next()
        {
            //System.out.println("  enum.next");
            DoNext();
            return SetResult();
        }

        /// <summary>
        /// Seeks to smallest term that's >= target. </summary>
        public InputOutput<T> SeekCeil(IntsRef target)
        {
            this.Target = target;
            TargetLength = target.Length;
            base.DoSeekCeil();
            return SetResult();
        }

        /// <summary>
        /// Seeks to biggest term that's <= target. </summary>
        public InputOutput<T> SeekFloor(IntsRef target)
        {
            this.Target = target;
            TargetLength = target.Length;
            base.DoSeekFloor();
            return SetResult();
        }

        /// <summary>
        /// Seeks to exactly this term, returning null if the term
        ///  doesn't exist.  this is faster than using {@link
        ///  #seekFloor} or <seealso cref="#seekCeil"/> because it
        ///  short-circuits as soon the match is not found.
        /// </summary>
        public InputOutput<T> SeekExact(IntsRef target)
        {
            this.Target = target;
            TargetLength = target.Length;
            if (base.DoSeekExact())
            {
                Debug.Assert(Upto == 1 + target.Length);
                return SetResult();
            }
            else
            {
                return null;
            }
        }

        protected internal override int TargetLabel
        {
            get
            {
                if (Upto - 1 == Target.Length)
                {
                    return FST<T>.END_LABEL;
                }
                else
                {
                    return Target.Ints[Target.Offset + Upto - 1];
                }
            }
        }

        protected internal override int CurrentLabel
        {
            get
            {
                // current.offset fixed at 1
                return Current_Renamed.Ints[Upto];
            }
            set
            {
                Current_Renamed.Ints[Upto] = value;
            }
        }

        protected internal override void Grow()
        {
            Current_Renamed.Ints = ArrayUtil.Grow(Current_Renamed.Ints, Upto + 1);
        }

        private InputOutput<T> SetResult()
        {
            if (Upto == 0)
            {
                return null;
            }
            else
            {
                Current_Renamed.Length = Upto - 1;
                Result.Output = Output[Upto];
                return Result;
            }
        }
    }
}