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
using Spatial4n.Core.Context;
using Spatial4n.Core.Exceptions;

namespace Lucene.Net.Spatial.Queries
{
	public class SpatialArgsParser
	{
		public SpatialArgs Parse(String v, SpatialContext ctx)
		{
			int idx = v.IndexOf('(');
			int edx = v.LastIndexOf(')');

			if (idx < 0 || idx > edx)
			{
				throw new InvalidSpatialArgument("missing parens: " + v);
			}

			SpatialOperation op = SpatialOperation.Get(v.Substring(0, idx).Trim());

			//Substring in .NET is (startPosn, length), But in Java it's (startPosn, endPosn)
			//see http://docs.oracle.com/javase/1.4.2/docs/api/java/lang/String.html#substring(int, int)
			String body = v.Substring(idx + 1, edx - (idx + 1)).Trim();
			if (body.Length < 1)
			{
				throw new InvalidSpatialArgument("missing body : " + v);
			}

			var shape = ctx.ReadShape(body);
			var args = new SpatialArgs(op, shape);

			if (v.Length > (edx + 1))
			{
				body = v.Substring(edx + 1).Trim();
				if (body.Length > 0)
				{
					Dictionary<String, String> aa = ParseMap(body);
					args.Min = ReadDouble(aa["min"]);
					args.Max = ReadDouble(aa["max"]);
					args.SetDistPrecision(ReadDouble(aa["distPrec"]));
					if (aa.Count > 3)
					{
						throw new InvalidSpatialArgument("unused parameters: " + aa);
					}
				}
			}
			return args;
		}

		protected static double? ReadDouble(String v)
		{
			double val;
			return double.TryParse(v, out val) ? val : (double?)null;
		}

		protected static bool ReadBool(String v, bool defaultValue)
		{
			bool ret;
			return bool.TryParse(v, out ret) ? ret : defaultValue;
		}

		protected static Dictionary<String, String> ParseMap(String body)
		{
			var map = new Dictionary<String, String>();
			int tokenPos = 0;
			var st = body.Split(new[] {' ', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries);
			while (tokenPos < st.Length)
			{
				String a = st[tokenPos++];
				int idx = a.IndexOf('=');
				if (idx > 0)
				{
					String k = a.Substring(0, idx);
					String v = a.Substring(idx + 1);
					map[k] = v;
				}
				else
				{
					map[a] = a;
				}
			}
			return map;
		}

	}
}
