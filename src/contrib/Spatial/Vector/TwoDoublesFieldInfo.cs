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

namespace Lucene.Net.Spatial.Vector
{
	public class TwoDoublesFieldInfo : SpatialFieldInfo
	{
		public static String SUFFIX_X = "__x";
		public static String SUFFIX_Y = "__y";

		private readonly String fieldName;
		private readonly String fieldNameX;
		private readonly String fieldNameY;

		public TwoDoublesFieldInfo(String fieldNamePrefix)
		{
			fieldName = fieldNamePrefix;
			fieldNameX = fieldNamePrefix + SUFFIX_X;
			fieldNameY = fieldNamePrefix + SUFFIX_Y;
		}

		public String GetFieldName()
		{
			return fieldName;
		}

		public String GetFieldNameX()
		{
			return fieldNameX;
		}

		public String GetFieldNameY()
		{
			return fieldNameY;
		}

	}
}
