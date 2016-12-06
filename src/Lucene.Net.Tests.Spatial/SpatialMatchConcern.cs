namespace Lucene.Net.Spatial
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

    public class SpatialMatchConcern
    {
        public readonly bool orderIsImportant;
        public readonly bool resultsAreSuperset; // if the strategy can not give exact answers, but used to limit results

        private SpatialMatchConcern(bool order, bool superset)
        {
            this.orderIsImportant = order;
            this.resultsAreSuperset = superset;
        }

        public static readonly SpatialMatchConcern EXACT = new SpatialMatchConcern(true, false);
        public static readonly SpatialMatchConcern FILTER = new SpatialMatchConcern(false, false);
        public static readonly SpatialMatchConcern SUPERSET = new SpatialMatchConcern(false, true);
    }
}
