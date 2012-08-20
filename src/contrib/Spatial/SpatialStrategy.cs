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
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial
{
	/// <summary>
	/// The SpatialStrategy encapsulates an approach to indexing and searching based on shapes.
	/// <p/>
	/// Note that a SpatialStrategy is not involved with the Lucene stored field values of shapes, which is
	/// immaterial to indexing & search.
	/// <p/>
	/// Thread-safe.
	/// </summary>
	public abstract class SpatialStrategy
	{
		protected bool ignoreIncompatibleGeometry;
		protected readonly SpatialContext ctx;
		protected readonly string fieldName;

		/// <summary>
		/// Constructs the spatial strategy with its mandatory arguments.
		/// </summary>
		/// <param name="ctx"></param>
		protected SpatialStrategy(SpatialContext ctx, string fieldName)
		{
			if (ctx == null)
				throw new ArgumentException("ctx is required", "ctx");
			this.ctx = ctx;
			if (string.IsNullOrEmpty(fieldName))
				throw new ArgumentException("fieldName is required", "fieldName");
			this.fieldName = fieldName;
		}

		public SpatialContext GetSpatialContext()
		{
			return ctx;
		}

		/// <summary>
		/// The name of the field or the prefix of them if there are multiple
		/// fields needed internally.
		/// </summary>
		/// <returns></returns>
		public String GetFieldName()
		{
			return fieldName;
		}

		/// <summary>
		/// Returns the IndexableField(s) from the <code>shape</code> that are to be
		/// added to the {@link org.apache.lucene.document.Document}.  These fields
		/// are expected to be marked as indexed and not stored.
		/// <p/>
		/// Note: If you want to <i>store</i> the shape as a string for retrieval in search
		/// results, you could add it like this:
		/// <pre>document.add(new StoredField(fieldName,ctx.toString(shape)));</pre>
		/// The particular string representation used doesn't matter to the Strategy since it
		/// doesn't use it.
		/// </summary>
		/// <param name="shape"></param>
		/// <returns>Not null nor will it have null elements.</returns>
		public abstract AbstractField[] CreateIndexableFields(Shape shape);

		public AbstractField CreateStoredField(Shape shape)
		{
			return new Field(GetFieldName(), ctx.ToString(shape), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO);
		}

		/// <summary>
		/// The value source yields a number that is proportional to the distance between the query shape and indexed data.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="fieldInfo"></param>
		/// <returns></returns>
		public abstract ValueSource MakeValueSource(SpatialArgs args);

		/// <summary>
		/// Make a query which has a score based on the distance from the data to the query shape.
		/// The default implementation constructs a {@link FilteredQuery} based on
		/// {@link #makeFilter(com.spatial4j.core.query.SpatialArgs, SpatialFieldInfo)} and
		/// {@link #makeValueSource(com.spatial4j.core.query.SpatialArgs, SpatialFieldInfo)}.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="fieldInfo"></param>
		/// <returns></returns>
		public virtual Query MakeQuery(SpatialArgs args)
		{
			Filter filter = MakeFilter(args);
			ValueSource vs = MakeValueSource(args);
			return new FilteredQuery(new FunctionQuery(vs), filter);
		}

		/// <summary>
		/// Make a Filter
		/// </summary>
		/// <param name="args"></param>
		/// <param name="fieldInfo"></param>
		/// <returns></returns>
		public abstract Filter MakeFilter(SpatialArgs args);

		public bool IsIgnoreIncompatibleGeometry()
		{
			return ignoreIncompatibleGeometry;
		}

		public void SetIgnoreIncompatibleGeometry(bool ignoreIncompatibleGeometry)
		{
			this.ignoreIncompatibleGeometry = ignoreIncompatibleGeometry;
		}

		public override string ToString()
		{
			return GetType().Name + " field:" + fieldName + " ctx=" + ctx;
		}
	}
}
