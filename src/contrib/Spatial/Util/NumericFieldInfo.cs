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

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// Hold some of the parameters used by solr...
	/// </summary>
	public class NumericFieldInfo
	{
		public int precisionStep = 8; // same as solr default
		public bool store = true;
		public bool index = true;

		public void SetPrecisionStep(int p)
		{
			precisionStep = p;
			if (precisionStep <= 0 || precisionStep >= 64)
				precisionStep = int.MaxValue;
		}

		public AbstractField CreateDouble(String name, double v)
		{
			if (!store && !index)
				throw new ArgumentException("field must be indexed or stored");

			var fieldType = new NumericField(name, precisionStep, store ? Field.Store.YES : Field.Store.NO, index);
			fieldType.SetDoubleValue(v);
			//fieldType.SetOmitTermFreqAndPositions(true);
			fieldType.OmitNorms = true;
			return fieldType;
		}
	}
}
