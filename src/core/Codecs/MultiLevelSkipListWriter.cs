using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class MultiLevelSkipListWriter
    {
        protected int numberOfSkipLevels;

        private int skipInterval;

        private int skipMultiplier;

        private RAMOutputStream[] skipBuffer;

        protected MultiLevelSkipListWriter(int skipInterval, int skipMultiplier, int maxSkipLevels, int df)
        {
            this.skipInterval = skipInterval;
            this.skipMultiplier = skipMultiplier;

            // calculate the maximum number of skip levels for this document frequency
            if (df <= skipInterval)
            {
                numberOfSkipLevels = 1;
            }
            else
            {
                numberOfSkipLevels = 1 + MathUtil.Log(df / skipInterval, skipMultiplier);
            }

            // make sure it does not exceed maxSkipLevels
            if (numberOfSkipLevels > maxSkipLevels)
            {
                numberOfSkipLevels = maxSkipLevels;
            }
        }

        protected MultiLevelSkipListWriter(int skipInterval, int maxSkipLevels, int df)
            : this(skipInterval, skipInterval, maxSkipLevels, df)
        {
        }

        protected virtual void Init()
        {
            skipBuffer = new RAMOutputStream[numberOfSkipLevels];
            for (int i = 0; i < numberOfSkipLevels; i++)
            {
                skipBuffer[i] = new RAMOutputStream();
            }
        }

        protected virtual void ResetSkip()
        {
            if (skipBuffer == null)
            {
                Init();
            }
            else
            {
                for (int i = 0; i < skipBuffer.Length; i++)
                {
                    skipBuffer[i].Reset();
                }
            }
        }

        protected abstract void WriteSkipData(int level, IndexOutput skipBuffer);

        public virtual void BufferSkip(int df)
        {

            //assert df % skipInterval == 0;
            int numLevels = 1;
            df /= skipInterval;

            // determine max level
            while ((df % skipMultiplier) == 0 && numLevels < numberOfSkipLevels)
            {
                numLevels++;
                df /= skipMultiplier;
            }

            long childPointer = 0;

            for (int level = 0; level < numLevels; level++)
            {
                WriteSkipData(level, skipBuffer[level]);

                long newChildPointer = skipBuffer[level].FilePointer;

                if (level != 0)
                {
                    // store child pointers for all levels except the lowest
                    skipBuffer[level].WriteVLong(childPointer);
                }

                //remember the childPointer for the next level
                childPointer = newChildPointer;
            }
        }

        public virtual long WriteSkip(IndexOutput output)
        {
            long skipPointer = output.FilePointer;
            //System.out.println("skipper.writeSkip fp=" + skipPointer);
            if (skipBuffer == null || skipBuffer.Length == 0) return skipPointer;

            for (int level = numberOfSkipLevels - 1; level > 0; level--)
            {
                long length = skipBuffer[level].FilePointer;
                if (length > 0)
                {
                    output.WriteVLong(length);
                    skipBuffer[level].WriteTo(output);
                }
            }
            skipBuffer[0].WriteTo(output);

            return skipPointer;
        }
    }
}
