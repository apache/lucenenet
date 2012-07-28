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

using System;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;

namespace Lucene.Net.Spatial.Util
{
	public class ValueSourceFilter : Filter
	{
		public class ValueSourceFilteredDocIdSet : FilteredDocIdSet
		{
			private readonly ValueSourceFilter enclosingFilter;
			private readonly DocValues values;

			public ValueSourceFilteredDocIdSet(DocIdSet innerSet, DocValues values, ValueSourceFilter caller) : base(innerSet)
			{
				this.enclosingFilter = caller;
				this.values = values;
			}

			public override bool Match(int docid)
			{
				double val = values.DoubleVal(docid);
				return val > enclosingFilter.min && val < enclosingFilter.max;
			}
		}

		readonly Filter startingFilter;
		readonly ValueSource source;

		public readonly double min;
		public readonly double max;

		public ValueSourceFilter(Filter startingFilter, ValueSource source, double min, double max)
		{
			if (startingFilter == null)
			{
				throw new ArgumentException("please provide a non-null startingFilter; you can use QueryWrapperFilter(MatchAllDocsQuery) as a no-op filter", "startingFilter");
			}
			this.startingFilter = startingFilter;
			this.source = source;
			this.min = min;
			this.max = max;
		}

		public override DocIdSet GetDocIdSet(Index.IndexReader reader)
		{
			var values = source.GetValues(reader);
			return new ValueSourceFilteredDocIdSet(startingFilter.GetDocIdSet(reader), values, this);
		}
	}
}
