using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Globalization;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Creates documents whose content is a <see cref="long"/> number starting from
    /// <c><see cref="long.MinValue"/> + 10</c>.
    /// </summary>
    public class Int64ToEnglishContentSource : ContentSource
    {
        private long counter = 0;

        protected override void Dispose(bool disposing)
        {
        }

        // TODO: we could take param to specify locale...
        //private readonly RuleBasedNumberFormat rnbf = new RuleBasedNumberFormat(Locale.ROOT,
        //                                                                     RuleBasedNumberFormat.SPELLOUT);
        public override DocData GetNextDocData(DocData docData)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                docData.Clear();
                // store the current counter to avoid synchronization later on
                long curCounter;
                UninterruptableMonitor.Enter(this); // LUCENENET TODO: Since the whole method is synchronized, do we need this?
                try
                {
                    curCounter = counter;
                    if (counter == long.MaxValue)
                    {
                        counter = long.MinValue;//loop around
                    }
                    else
                    {
                        ++counter;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                docData.Body = curCounter.ToWords(); //rnbf.format(curCounter);
                docData.Name = "doc_" + curCounter.ToString(CultureInfo.InvariantCulture);
                docData.Title = "title_" + curCounter.ToString(CultureInfo.InvariantCulture);
                docData.SetDate(new DateTime());
                return docData;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override void ResetInputs()
        {
            counter = long.MinValue + 10;
        }
    }
}
