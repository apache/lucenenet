/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
	
	public sealed class FieldInfo
	{
		internal System.String name;
		internal bool isIndexed;
		internal int number;
		
		// true if term vector for this field should be stored
		public /*internal*/ bool storeTermVector;
		public /*internal*/ bool storeOffsetWithTermVector;
		public /*internal*/ bool storePositionWithTermVector;
		
		public /*internal*/ bool omitNorms; // omit norms associated with indexed fields
		
        public bool IsIndexed()
        {
            return isIndexed;
        }

		internal FieldInfo(System.String na, bool tk, int nu, bool storeTermVector, bool storePositionWithTermVector, bool storeOffsetWithTermVector, bool omitNorms)
		{
			name = na;
			isIndexed = tk;
			number = nu;
			this.storeTermVector = storeTermVector;
			this.storeOffsetWithTermVector = storeOffsetWithTermVector;
			this.storePositionWithTermVector = storePositionWithTermVector;
			this.omitNorms = omitNorms;
		}
	}
}