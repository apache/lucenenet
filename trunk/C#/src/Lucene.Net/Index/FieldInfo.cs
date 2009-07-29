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
        internal bool omitTf;

        public bool omitTf_ForNUnitTest
        {
            get { return omitTf; }
        }

		internal bool storePayloads; // whether this field stores payloads together with term positions
		
		internal FieldInfo(System.String na, bool tk, int nu, bool storeTermVector, bool storePositionWithTermVector, bool storeOffsetWithTermVector, bool omitNorms, bool storePayloads, bool omitTf)
		{
			name = na;
			isIndexed = tk;
			number = nu;
			this.storeTermVector = storeTermVector;
			this.storeOffsetWithTermVector = storeOffsetWithTermVector;
			this.storePositionWithTermVector = storePositionWithTermVector;
			this.omitNorms = omitNorms;
			this.storePayloads = storePayloads;
            this.omitTf = omitTf;
		}
		
		public object Clone()
		{
			return new FieldInfo(name, isIndexed, number, storeTermVector, storePositionWithTermVector, storeOffsetWithTermVector, omitNorms, storePayloads, omitTf);
		}

        internal void update(bool isIndexed, bool storeTermVector, bool storePositionWithTermVector,
                    bool storeOffsetWithTermVector, bool omitNorms, bool storePayloads, bool omitTf)
        {
            if (this.isIndexed != isIndexed)
            {
                this.isIndexed = true;                      // once indexed, always index
            }
            if (this.storeTermVector != storeTermVector)
            {
                this.storeTermVector = true;                // once vector, always vector
            }
            if (this.storePositionWithTermVector != storePositionWithTermVector)
            {
                this.storePositionWithTermVector = true;                // once vector, always vector
            }
            if (this.storeOffsetWithTermVector != storeOffsetWithTermVector)
            {
                this.storeOffsetWithTermVector = true;                // once vector, always vector
            }
            if (this.omitNorms != omitNorms)
            {
                this.omitNorms = false;                // once norms are stored, always store
            }
            if (this.omitTf != omitTf)
            {
                this.omitTf = true;                // if one require omitTf at least once, it remains off for life
            }
            if (this.storePayloads != storePayloads)
            {
                this.storePayloads = true;
            }
        }

        internal void update(FieldInfo other)
        {
            if (isIndexed != other.isIndexed)
            {
                isIndexed = true;                      // once indexed, always index
            }
            if (storeTermVector != other.storeTermVector)
            {
                storeTermVector = true;                // once vector, always vector
            }
            if (storePositionWithTermVector != other.storePositionWithTermVector)
            {
                storePositionWithTermVector = true;                // once vector, always vector
            }
            if (storeOffsetWithTermVector != other.storeOffsetWithTermVector)
            {
                storeOffsetWithTermVector = true;                // once vector, always vector
            }
            if (omitNorms != other.omitNorms)
            {
                omitNorms = false;                // once norms are stored, always store
            }
            if (omitTf != other.omitTf)
            {
                omitTf = true;                // if one require omitTf at least once, it remains off for life
            }
            if (storePayloads != other.storePayloads)
            {
                storePayloads = true;
            }
        }

        // For testing only
        public string Name_ForNUnitTest
        {
            get { return name; }
        }

        // For testing only
        public bool StorePayloads_ForNUnitTest
        {
            get { return storePayloads; }
        }

        public bool IsIndexed_ForNUnitTest()
        {
            return isIndexed;
        }
    }
}