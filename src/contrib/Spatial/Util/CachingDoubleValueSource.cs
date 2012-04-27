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

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search.Function;

namespace Lucene.Net.Spatial.Util
{
	public class CachingDoubleValueSource : ValueSource
	{
		readonly ValueSource source;
		readonly Dictionary<int, double> cache;

		public CachingDoubleValueSource(ValueSource source)
		{
			this.source = source;
			cache = new Dictionary<int, double>();
		}

		public override DocValues GetValues(IndexReader reader)
		{
			//int @base = reader.DocBase;
			//FunctionValues vals = source.getValues(context,readerContext);
			//return new FunctionValues() {

			//  @Override
			//  public double doubleVal(int doc) {
			//    Integer key = Integer.valueOf( base+doc );
			//    Double v = cache.get( key );
			//    if( v == null ) {
			//      v = Double.valueOf( vals.doubleVal(doc) );
			//      cache.put( key, v );
			//    }
			//    return v.doubleValue();
			//  }

			//  @Override
			//  public float floatVal(int doc) {
			//    return (float)doubleVal(doc);
			//  }

			//  @Override
			//  public String toString(int doc) {
			//    return doubleVal(doc)+"";
			//  }
			//};

		}

		public override string Description()
		{
			return "Cached[" + source.Description() + "]";
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			var that = o as CachingDoubleValueSource;

			if (that == null) return false;
			if (source != null ? !source.Equals(that.source) : that.source != null) return false;

			return true;
		}

		public override int GetHashCode()
		{
			return source != null ? source.GetHashCode() : 0;
		}
	}
}
