using System.Diagnostics;

namespace Lucene.Net.Codecs
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

    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using MathUtil = Lucene.Net.Util.MathUtil;
    using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;

    /// <summary>
    /// this abstract class writes skip lists with multiple levels.
    ///
    /// <pre>
    ///
    /// Example for skipInterval = 3:
    ///                                                     c            (skip level 2)
    ///                 c                 c                 c            (skip level 1)
    ///     x     x     x     x     x     x     x     x     x     x      (skip level 0)
    /// d d d d d d d d d d d d d d d d d d d d d d d d d d d d d d d d  (posting list)
    ///     3     6     9     12    15    18    21    24    27    30     (df)
    ///
    /// d - document
    /// x - skip data
    /// c - skip data with child pointer
    ///
    /// Skip level i contains every skipInterval-th entry from skip level i-1.
    /// Therefore the number of entries on level i is: floor(df / ((skipInterval ^ (i + 1))).
    ///
    /// Each skip entry on a level i>0 contains a pointer to the corresponding skip entry in list i-1.
    /// this guarantees a logarithmic amount of skips to find the target document.
    ///
    /// While this class takes care of writing the different skip levels,
    /// subclasses must define the actual format of the skip data.
    /// </pre>
    /// @lucene.experimental
    /// </summary>

    public abstract class MultiLevelSkipListWriter
    {
        /// <summary>
        /// number of levels in this skip list </summary>
        protected internal int NumberOfSkipLevels;

        /// <summary>
        /// the skip interval in the list with level = 0 </summary>
        private int SkipInterval;

        /// <summary>
        /// skipInterval used for level &gt; 0 </summary>
        private int SkipMultiplier;

        /// <summary>
        /// for every skip level a different buffer is used </summary>
        private RAMOutputStream[] SkipBuffer;

        /// <summary>
        /// Creates a {@code MultiLevelSkipListWriter}. </summary>
        protected MultiLevelSkipListWriter(int skipInterval, int skipMultiplier, int maxSkipLevels, int df)
        {
            this.SkipInterval = skipInterval;
            this.SkipMultiplier = skipMultiplier;

            // calculate the maximum number of skip levels for this document frequency
            if (df <= skipInterval)
            {
                NumberOfSkipLevels = 1;
            }
            else
            {
                NumberOfSkipLevels = 1 + MathUtil.Log(df / skipInterval, skipMultiplier);
            }

            // make sure it does not exceed maxSkipLevels
            if (NumberOfSkipLevels > maxSkipLevels)
            {
                NumberOfSkipLevels = maxSkipLevels;
            }
        }

        /// <summary>
        /// Creates a {@code MultiLevelSkipListWriter}, where
        ///  {@code skipInterval} and {@code skipMultiplier} are
        ///  the same.
        /// </summary>
        protected MultiLevelSkipListWriter(int skipInterval, int maxSkipLevels, int df)
            : this(skipInterval, skipInterval, maxSkipLevels, df)
        {
        }

        /// <summary>
        /// Allocates internal skip buffers. </summary>
        protected virtual void Init()
        {
            SkipBuffer = new RAMOutputStream[NumberOfSkipLevels];
            for (int i = 0; i < NumberOfSkipLevels; i++)
            {
                SkipBuffer[i] = new RAMOutputStream();
            }
        }

        /// <summary>
        /// Creates new buffers or empties the existing ones </summary>
        public virtual void ResetSkip()
        {
            if (SkipBuffer == null)
            {
                Init();
            }
            else
            {
                for (int i = 0; i < SkipBuffer.Length; i++)
                {
                    SkipBuffer[i].Reset();
                }
            }
        }

        /// <summary>
        /// Subclasses must implement the actual skip data encoding in this method.
        /// </summary>
        /// <param name="level"> the level skip data shall be writing for </param>
        /// <param name="skipBuffer"> the skip buffer to write to </param>
        protected abstract void WriteSkipData(int level, IndexOutput skipBuffer);

        /// <summary>
        /// Writes the current skip data to the buffers. The current document frequency determines
        /// the max level is skip data is to be written to.
        /// </summary>
        /// <param name="df"> the current document frequency </param>
        /// <exception cref="IOException"> If an I/O error occurs </exception>
        public virtual void BufferSkip(int df)
        {
            Debug.Assert(df % SkipInterval == 0);
            int numLevels = 1;
            df /= SkipInterval;

            // determine max level
            while ((df % SkipMultiplier) == 0 && numLevels < NumberOfSkipLevels)
            {
                numLevels++;
                df /= SkipMultiplier;
            }

            long childPointer = 0;

            for (int level = 0; level < numLevels; level++)
            {
                WriteSkipData(level, SkipBuffer[level]);

                long newChildPointer = SkipBuffer[level].FilePointer;

                if (level != 0)
                {
                    // store child pointers for all levels except the lowest
                    SkipBuffer[level].WriteVLong(childPointer);
                }

                //remember the childPointer for the next level
                childPointer = newChildPointer;
            }
        }

        /// <summary>
        /// Writes the buffered skip lists to the given output.
        /// </summary>
        /// <param name="output"> the IndexOutput the skip lists shall be written to </param>
        /// <returns> the pointer the skip list starts </returns>
        public virtual long WriteSkip(IndexOutput output)
        {
            long skipPointer = output.FilePointer;
            //System.out.println("skipper.writeSkip fp=" + skipPointer);
            if (SkipBuffer == null || SkipBuffer.Length == 0)
            {
                return skipPointer;
            }

            for (int level = NumberOfSkipLevels - 1; level > 0; level--)
            {
                long length = SkipBuffer[level].FilePointer;
                if (length > 0)
                {
                    output.WriteVLong(length);
                    SkipBuffer[level].WriteTo(output);
                }
            }
            SkipBuffer[0].WriteTo(output);

            return skipPointer;
        }
    }
}