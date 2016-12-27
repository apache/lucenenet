using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Lucene.Net.Index
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

    using Directory = Lucene.Net.Store.Directory;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using DocValuesProducer = Lucene.Net.Codecs.DocValuesProducer;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Manages the <seealso cref="DocValuesProducer"/> held by <seealso cref="SegmentReader"/> and
    /// keeps track of their reference counting.
    /// </summary>
    internal sealed class SegmentDocValues
    {
        private readonly IDictionary<long?, RefCount<DocValuesProducer>> GenDVProducers = new Dictionary<long?, RefCount<DocValuesProducer>>();

        private RefCount<DocValuesProducer> NewDocValuesProducer(SegmentCommitInfo si, IOContext context, Directory dir, DocValuesFormat dvFormat, long? gen, IList<FieldInfo> infos, int termsIndexDivisor)
        {
            Directory dvDir = dir;
            string segmentSuffix = "";
            if ((long)gen != -1)
            {
                dvDir = si.Info.Dir; // gen'd files are written outside CFS, so use SegInfo directory
                segmentSuffix = ((long)gen).ToString(CultureInfo.InvariantCulture);//Convert.ToString((long)gen, Character.MAX_RADIX);
            }

            // set SegmentReadState to list only the fields that are relevant to that gen
            SegmentReadState srs = new SegmentReadState(dvDir, si.Info, new FieldInfos(infos.ToArray()), context, termsIndexDivisor, segmentSuffix);
            return new RefCountHelper(this, dvFormat.FieldsProducer(srs), gen);
        }

        private class RefCountHelper : RefCount<DocValuesProducer>
        {
            private readonly SegmentDocValues OuterInstance;
            private long? Gen;

            public RefCountHelper(SegmentDocValues outerInstance, DocValuesProducer fieldsProducer, long? gen)
                : base(fieldsProducer)
            {
                this.OuterInstance = outerInstance;
                this.Gen = gen;
            }

            protected override void Release()
            {
                m_object.Dispose();
                lock (OuterInstance)
                {
                    OuterInstance.GenDVProducers.Remove(Gen);
                }
            }
        }

        /// <summary>
        /// Returns the <seealso cref="DocValuesProducer"/> for the given generation. </summary>
        internal DocValuesProducer GetDocValuesProducer(long? gen, SegmentCommitInfo si, IOContext context, Directory dir, DocValuesFormat dvFormat, IList<FieldInfo> infos, int termsIndexDivisor)
        {
            lock (this)
            {
                RefCount<DocValuesProducer> dvp;
                if (!(GenDVProducers.TryGetValue(gen, out dvp)))
                {
                    dvp = NewDocValuesProducer(si, context, dir, dvFormat, gen, infos, termsIndexDivisor);
                    Debug.Assert(dvp != null);
                    GenDVProducers[gen] = dvp;
                }
                else
                {
                    dvp.IncRef();
                }
                return dvp.Get();
            }
        }

        /// <summary>
        /// Decrement the reference count of the given <seealso cref="DocValuesProducer"/>
        /// generations.
        /// </summary>
        internal void DecRef(IList<long?> dvProducersGens)
        {
            lock (this)
            {
                Exception t = null;
                foreach (long? gen in dvProducersGens)
                {
                    RefCount<DocValuesProducer> dvp = GenDVProducers[gen];
                    Debug.Assert(dvp != null, "gen=" + gen);
                    try
                    {
                        dvp.DecRef();
                    }
                    catch (Exception th)
                    {
                        if (t != null)
                        {
                            t = th;
                        }
                    }
                }
                if (t != null)
                {
                    IOUtils.ReThrow(t);
                }
            }
        }
    }
}