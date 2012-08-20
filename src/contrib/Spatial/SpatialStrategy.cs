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

using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial
{
	/* must be thread safe */
	public abstract class SpatialStrategy<T> where T : SpatialFieldInfo
	{
		protected bool ignoreIncompatibleGeometry = false;
		protected readonly SpatialContext ctx;

		protected SpatialStrategy(SpatialContext ctx)
		{
			this.ctx = ctx;
		}

		public SpatialContext GetSpatialContext()
		{
			return ctx;
		}

		/** Corresponds with Solr's  FieldType.isPolyField(). */
		public virtual bool IsPolyField()
		{
			return false;
		}

		/**
		 * Corresponds with Solr's FieldType.createField().
		 *
		 * This may return a null field if it does not want to make anything.
		 * This is reasonable behavior if 'ignoreIncompatibleGeometry=true' and the
		 * geometry is incompatible
		 */
		public abstract Field CreateField(T fieldInfo, Shape shape, bool index, bool store);

		/** Corresponds with Solr's FieldType.createFields(). */
		public virtual AbstractField[] CreateFields(T fieldInfo, Shape shape, bool index, bool store)
		{
			return new AbstractField[] { CreateField(fieldInfo, shape, index, store) };
		}

		/// <summary>
		/// The value source yields a number that is proportional to the distance between the query shape and indexed data.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="fieldInfo"></param>
		/// <returns></returns>
		public abstract ValueSource MakeValueSource(SpatialArgs args, T fieldInfo);

		/// <summary>
		/// Make a query which has a score based on the distance from the data to the query shape.
		/// The default implementation constructs a {@link FilteredQuery} based on
		/// {@link #makeFilter(com.spatial4j.core.query.SpatialArgs, SpatialFieldInfo)} and
		/// {@link #makeValueSource(com.spatial4j.core.query.SpatialArgs, SpatialFieldInfo)}.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="fieldInfo"></param>
		/// <returns></returns>
		public virtual Query MakeQuery(SpatialArgs args, T fieldInfo)
		{
			Filter filter = MakeFilter(args, fieldInfo);
			ValueSource vs = MakeValueSource(args, fieldInfo);
			return new FilteredQuery(new FunctionQuery(vs), filter);
		}

		/// <summary>
		/// Make a Filter
		/// </summary>
		/// <param name="args"></param>
		/// <param name="fieldInfo"></param>
		/// <returns></returns>
		public abstract Filter MakeFilter(SpatialArgs args, T fieldInfo);

		public bool IsIgnoreIncompatibleGeometry()
		{
			return ignoreIncompatibleGeometry;
		}

		public void SetIgnoreIncompatibleGeometry(bool ignoreIncompatibleGeometry)
		{
			this.ignoreIncompatibleGeometry = ignoreIncompatibleGeometry;
		}

	}
}
