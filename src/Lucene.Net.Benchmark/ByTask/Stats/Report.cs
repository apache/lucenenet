namespace Lucene.Net.Benchmarks.ByTask.Stats
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
    /// Textual report of current statistics.
    /// </summary>
    public class Report
    {
        private readonly string text; // LUCENENET: marked readonly
        private readonly int size; // LUCENENET: marked readonly
        private readonly int outOf; // LUCENENET: marked readonly
        private readonly int reported; // LUCENENET: marked readonly

        public Report(string text, int size, int reported, int outOf)
        {
            this.text = text;
            this.size = size;
            this.reported = reported;
            this.outOf = outOf;
        }

        /// <summary>
        /// Gets total number of stats points when this report was created.
        /// </summary>
        public virtual int OutOf => outOf;

        /// <summary>
        /// Gets number of lines in the report.
        /// </summary>
        public virtual int Count => size;

        /// <summary>
        /// Gets the report text.
        /// </summary>
        public virtual string Text => text;

        /// <summary>
        /// Gets number of stats points represented in this report.
        /// </summary>
        public virtual int Reported => reported;
    }
}
