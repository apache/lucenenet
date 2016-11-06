namespace Lucene.Net.Search.Grouping
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
    /// Expert: representation of a group in <see cref="AbstractFirstPassGroupingCollector{TGroupValue}"/>,
    /// tracking the top doc and <see cref="FieldComparator"/> slot.
    /// @lucene.internal
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public class CollectedSearchGroup<TGroupValue> : SearchGroup<TGroupValue>, ICollectedSearchGroup
    {
        public int TopDoc { get; internal set; }
        public int ComparatorSlot { get; internal set; }
    }


    /// <summary>
    /// LUCENENET specific interface for passing/comparing the CollectedSearchGroup
    /// without referencing its generic closing type
    /// </summary>
    public interface ICollectedSearchGroup
    {
        int TopDoc { get; }
        int ComparatorSlot { get; }
    }
}
