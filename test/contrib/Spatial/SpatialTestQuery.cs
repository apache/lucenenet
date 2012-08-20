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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Core.Context;
using Spatial4n.Core.Io;

namespace Lucene.Net.Contrib.Spatial.Test
{
	/// <summary>
	/// Helper class to execute queries
	/// </summary>
	public class SpatialTestQuery
	{
		public String testname;
		public String line;
		public int lineNumber = -1;
		public SpatialArgs args;
		public List<String> ids = new List<String>();

		public class SpatialTestQueryLineReader : LineReader<SpatialTestQuery>
		{
			private readonly SpatialArgsParser parser;
			private readonly SpatialContext ctx;

			public SpatialTestQueryLineReader(Stream @in, SpatialArgsParser parser, SpatialContext ctx)
				: base(@in)
			{
				this.parser = parser;
				this.ctx = ctx;
			}

			public SpatialTestQueryLineReader(StreamReader r, SpatialArgsParser parser, SpatialContext ctx)
				: base(r)
			{
				this.parser = parser;
				this.ctx = ctx;
			}

			public override SpatialTestQuery ParseLine(string line)
			{
				var test = new SpatialTestQuery {line = line, lineNumber = GetLineNumber()};

				// skip a comment
				if (line.StartsWith("["))
				{
					int idx0 = line.IndexOf(']');
					if (idx0 > 0)
					{
						line = line.Substring(idx0 + 1);
					}
				}

				int idx = line.IndexOf('@');

				var pos = 0;
				var st = line.Substring(0, idx).Split(new[] {' ', '\t', '\n', '\r', '\f'}, StringSplitOptions.RemoveEmptyEntries);
				while (pos < st.Length)
				{
					test.ids.Add(st[pos++].Trim());
				}
				test.args = parser.Parse(line.Substring(idx + 1).Trim(), ctx);
				return test;
			}
		}

		/// <summary>
		/// Get Test Queries
		/// </summary>
		/// <param name="parser"></param>
		/// <param name="ctx"></param>
		/// <param name="name"></param>
		/// <param name="in"></param>
		/// <returns></returns>
		public static IEnumerator<SpatialTestQuery> getTestQueries(
			SpatialArgsParser parser,
			SpatialContext ctx,
			String name,
			Stream @in)
		{
			return new SpatialTestQueryLineReader(new StreamReader(@in, Encoding.UTF8), parser, ctx);

		}
	}
}
