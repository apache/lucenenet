using J2N;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// Manages the <see cref="DocValuesProducer"/> held by <see cref="SegmentReader"/> and
    /// keeps track of their reference counting.
    /// </summary>
    internal sealed class SegmentDocValues
    {
        private readonly IDictionary<long, RefCount<DocValuesProducer>> genDVProducers = new Dictionary<long, RefCount<DocValuesProducer>>();

        private RefCount<DocValuesProducer> NewDocValuesProducer(SegmentCommitInfo si, IOContext context, Directory dir, DocValuesFormat dvFormat, long gen, IList<FieldInfo> infos, int termsIndexDivisor)
        {
            Directory dvDir = dir;
            string segmentSuffix = "";
            if (gen != -1)
            {
                dvDir = si.Info.Dir; // gen'd files are written outside CFS, so use SegInfo directory
                // LUCENENET specific: We created the segments names wrong in 4.8.0-beta00001 - 4.8.0-beta00015,
                // so we added a switch to be able to read these indexes in later versions. This logic as well as an
                // optimization on the first 100 segment values is implmeneted in SegmentInfos.SegmentNumberToString().
                segmentSuffix = SegmentInfos.SegmentNumberToString(gen);
            }

            // set SegmentReadState to list only the fields that are relevant to that gen
            SegmentReadState srs = new SegmentReadState(dvDir, si.Info, new FieldInfos(infos.ToArray()), context, termsIndexDivisor, segmentSuffix);
            return new RefCountHelper(this, dvFormat.FieldsProducer(srs), gen);
        }

        private class RefCountHelper : RefCount<DocValuesProducer>
        {
            private readonly SegmentDocValues outerInstance;
            private readonly long gen; // LUCENENET: marked readonly

            public RefCountHelper(SegmentDocValues outerInstance, DocValuesProducer fieldsProducer, long gen)
                : base(fieldsProducer)
            {
                this.outerInstance = outerInstance;
                this.gen = gen;
            }

            protected override void Release()
            {
                m_object.Dispose();
                UninterruptableMonitor.Enter(outerInstance);
                try
                {
                    outerInstance.genDVProducers.Remove(gen);
                }
                finally
                {
                    UninterruptableMonitor.Exit(outerInstance);
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="DocValuesProducer"/> for the given generation. </summary>
        internal DocValuesProducer GetDocValuesProducer(long gen, SegmentCommitInfo si, IOContext context, Directory dir, DocValuesFormat dvFormat, IList<FieldInfo> infos, int termsIndexDivisor)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!genDVProducers.TryGetValue(gen, out RefCount<DocValuesProducer> dvp))
                {
                    dvp = NewDocValuesProducer(si, context, dir, dvFormat, gen, infos, termsIndexDivisor);
                    if (Debugging.AssertsEnabled) Debugging.Assert(dvp != null);
                    genDVProducers[gen] = dvp;
                }
                else
                {
                    dvp.IncRef();
                }
                return dvp.Get();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Decrement the reference count of the given <see cref="DocValuesProducer"/>
        /// generations.
        /// </summary>
        internal void DecRef(IList<long> dvProducersGens)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                Exception t = null;
                foreach (long gen in dvProducersGens)
                {
                    genDVProducers.TryGetValue(gen, out RefCount<DocValuesProducer> dvp);
                    if (Debugging.AssertsEnabled) Debugging.Assert(dvp != null,"gen={0}", gen);
                    try
                    {
                        dvp.DecRef();
                    }
                    catch (Exception th) when (th.IsThrowable())
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}