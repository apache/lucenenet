/**
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

using ArrayUtil = Lucene.Net.Util.ArrayUtil;
using Similarity = Lucene.Net.Search.Similarity;

namespace Lucene.Net.Index
{
    internal sealed class NormsWriterPerField : InvertedDocEndConsumerPerField, System.IComparable
    {
        internal readonly NormsWriterPerThread perThread;
        internal readonly FieldInfo fieldInfo;
        internal readonly DocumentsWriter.DocState docState;

        // Holds all docID/norm pairs we've seen
        internal int[] docIDs = new int[1];
        internal byte[] norms = new byte[1];
        internal int upto;

        internal readonly DocInverter.FieldInvertState fieldState;

        public void reset()
        {
            // Shrink back if we are overallocated now:
            docIDs = ArrayUtil.Shrink(docIDs, upto);
            norms = ArrayUtil.Shrink(norms, upto);
            upto = 0;
        }

        public NormsWriterPerField(DocInverterPerField docInverterPerField, NormsWriterPerThread perThread, FieldInfo fieldInfo)
        {
            this.perThread = perThread;
            this.fieldInfo = fieldInfo;
            docState = perThread.docState;
            fieldState = docInverterPerField.fieldState;
        }

        internal override void abort()
        {
            upto = 0;
        }

        public int CompareTo(object other)
        {
            return string.CompareOrdinal(fieldInfo.name, ((NormsWriterPerField)other).fieldInfo.name);
        }

        internal override void finish()
        {
            System.Diagnostics.Debug.Assert(docIDs.Length == norms.Length);
            if (fieldInfo.isIndexed && !fieldInfo.omitNorms)
            {
                if (docIDs.Length <= upto)
                {
                    System.Diagnostics.Debug.Assert(docIDs.Length == upto);
                    docIDs = ArrayUtil.Grow(docIDs, 1 + upto);
                    norms = ArrayUtil.Grow(norms, 1 + upto);
                }
                float norm = fieldState.boost * docState.similarity.LengthNorm(fieldInfo.name, fieldState.length);
                norms[upto] = Similarity.EncodeNorm(norm);
                docIDs[upto] = docState.docID;
                upto++;
            }
        }
    }
}
