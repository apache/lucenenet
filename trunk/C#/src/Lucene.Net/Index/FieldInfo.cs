/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Index
{
	
	public sealed class FieldInfo : System.ICloneable
	{
		internal System.String name;
		internal bool isIndexed;
		internal int number;
		
		// true if term vector for this field should be stored
		public bool storeTermVector;
		public bool storeOffsetWithTermVector;
		public bool storePositionWithTermVector;
		
		public bool omitNorms; // omit norms associated with indexed fields
		
		public bool IsIndexed()
		{
			return isIndexed;
		}
		
		internal bool storePayloads; // whether this field stores payloads together with term positions
		
		internal FieldInfo(System.String na, bool tk, int nu, bool storeTermVector, bool storePositionWithTermVector, bool storeOffsetWithTermVector, bool omitNorms, bool storePayloads)
		{
			name = na;
			isIndexed = tk;
			number = nu;
			this.storeTermVector = storeTermVector;
			this.storeOffsetWithTermVector = storeOffsetWithTermVector;
			this.storePositionWithTermVector = storePositionWithTermVector;
			this.omitNorms = omitNorms;
			this.storePayloads = storePayloads;
		}
		
		public System.Object Clone()
		{
			return new FieldInfo(name, isIndexed, number, storeTermVector, storePositionWithTermVector, storeOffsetWithTermVector, omitNorms, storePayloads);
		}

        // For testing only
        public System.String Name_ForNUnitTest
        {
            get { return name; }
        }

        // For testing only
        public bool StorePayloads_ForNUnitTest
        {
            get { return storePayloads; }
        }
    }
}