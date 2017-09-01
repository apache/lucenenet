namespace Lucene.Net.Search.Payloads
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
    /// An abstract class that defines a way for Payload*Query instances to transform
    /// the cumulative effects of payload scores for a document.
    /// <para/>
    /// @lucene.experimental this class and its derivations are experimental and subject to
    /// change
    /// </summary>
    /// <seealso cref="Lucene.Net.Search.Payloads.PayloadTermQuery"/>
    public abstract class PayloadFunction
    {
        /// <summary>
        /// Calculate the score up to this point for this doc and field </summary>
        /// <param name="docId"> The current doc </param>
        /// <param name="field"> The field </param>
        /// <param name="start"> The start position of the matching Span </param>
        /// <param name="end"> The end position of the matching Span </param>
        /// <param name="numPayloadsSeen"> The number of payloads seen so far </param>
        /// <param name="currentScore"> The current score so far </param>
        /// <param name="currentPayloadScore"> The score for the current payload </param>
        /// <returns> The new current Score
        /// </returns>
        /// <seealso cref="Lucene.Net.Search.Spans.Spans"/>
        public abstract float CurrentScore(int docId, string field, int start, int end, int numPayloadsSeen, float currentScore, float currentPayloadScore);

        /// <summary>
        /// Calculate the final score for all the payloads seen so far for this doc/field </summary>
        /// <param name="docId"> The current doc </param>
        /// <param name="field"> The current field </param>
        /// <param name="numPayloadsSeen"> The total number of payloads seen on this document </param>
        /// <param name="payloadScore"> The raw score for those payloads </param>
        /// <returns> The final score for the payloads </returns>
        public abstract float DocScore(int docId, string field, int numPayloadsSeen, float payloadScore);

        public virtual Explanation Explain(int docId, string field, int numPayloadsSeen, float payloadScore)
        {
            Explanation result = new Explanation();
            result.Description = this.GetType().Name + ".docScore()";
            result.Value = DocScore(docId, field, numPayloadsSeen, payloadScore);
            return result;
        }

        public override abstract int GetHashCode();

        public override abstract bool Equals(object o);
    }
}