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

using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

namespace Lucene.Net.Index
{
    internal sealed class TermVectorsTermsWriterPerThread : TermsHashConsumerPerThread
    {

        internal readonly TermVectorsTermsWriter termsWriter;
        internal readonly TermsHashPerThread termsHashPerThread;
        internal readonly DocumentsWriter.DocState docState;

        internal TermVectorsTermsWriter.PerDoc doc;

        public TermVectorsTermsWriterPerThread(TermsHashPerThread termsHashPerThread, TermVectorsTermsWriter termsWriter)
        {
            this.termsWriter = termsWriter;
            this.termsHashPerThread = termsHashPerThread;
            docState = termsHashPerThread.docState;
        }

        // Used by perField when serializing the term vectors
        internal readonly ByteSliceReader vectorSliceReader = new ByteSliceReader();

        internal readonly UnicodeUtil.UTF8Result[] utf8Results = { new UnicodeUtil.UTF8Result(), new UnicodeUtil.UTF8Result() };

        internal override void startDocument()
        {
            System.Diagnostics.Debug.Assert(clearLastVectorFieldName());
            if (doc != null)
            {
                doc.reset();
                doc.docID = docState.docID;
            }
        }

        internal override DocumentsWriter.DocWriter finishDocument()
        {
            try
            {
                return doc;
            }
            finally
            {
                doc = null;
            }
        }

        public override TermsHashConsumerPerField addField(TermsHashPerField termsHashPerField, FieldInfo fieldInfo)
        {
            return new TermVectorsTermsWriterPerField(termsHashPerField, this, fieldInfo);
        }

        public override void abort()
        {
            if (doc != null)
            {
                doc.Abort();
                doc = null;
            }
        }

        // Called only by assert
        internal bool clearLastVectorFieldName()
        {
            lastVectorFieldName = null;
            return true;
        }

        // Called only by assert
        internal string lastVectorFieldName;
        internal bool vectorFieldsInOrder(FieldInfo fi)
        {
            try
            {
                if (lastVectorFieldName != null)
                    return string.CompareOrdinal(lastVectorFieldName, fi.name) < 0;
                else
                    return true;
            }
            finally
            {
                lastVectorFieldName = fi.name;
            }
        }
    }
}
