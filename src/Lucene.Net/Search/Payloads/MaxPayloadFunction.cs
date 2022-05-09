using System;

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
    /// Returns the maximum payload score seen, else 1 if there are no payloads on the doc.
    /// <para/>
    /// Is thread safe and completely reusable.
    /// </summary>
    public class MaxPayloadFunction : PayloadFunction
    {
        public override float CurrentScore(int docId, string field, int start, int end, int numPayloadsSeen, float currentScore, float currentPayloadScore)
        {
            if (numPayloadsSeen == 0)
            {
                return currentPayloadScore;
            }
            else
            {
                return Math.Max(currentPayloadScore, currentScore);
            }
        }

        public override float DocScore(int docId, string field, int numPayloadsSeen, float payloadScore)
        {
            return numPayloadsSeen > 0 ? payloadScore : 1;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + this.GetType().GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            return true;
        }
    }
}