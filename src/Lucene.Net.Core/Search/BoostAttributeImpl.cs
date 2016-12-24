namespace Lucene.Net.Search
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

    using Attribute = Lucene.Net.Util.Attribute;

    /// <summary>
    /// Implementation class for <seealso cref="IBoostAttribute"/>.
    /// @lucene.internal
    /// </summary>
    public sealed class BoostAttribute : Attribute, IBoostAttribute
    {
        /// <summary>
        /// Sets the boost in this attribute </summary>
        private float boost = 1.0f;

        public float Boost
        {
            get { return boost; }
            set { boost = value; }
        }

        public override void Clear()
        {
            boost = 1.0f;
        }

        public override void CopyTo(Attribute target)
        {
            ((BoostAttribute)target).Boost = boost;
        }
    }
}