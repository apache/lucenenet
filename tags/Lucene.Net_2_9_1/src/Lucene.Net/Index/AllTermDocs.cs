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

using BitVector = Lucene.Net.Util.BitVector;

namespace Lucene.Net.Index
{
	
	class AllTermDocs : TermDocs
	{
		protected internal BitVector deletedDocs;
		protected internal int maxDoc;
		protected internal int doc = - 1;
		
		protected internal AllTermDocs(SegmentReader parent)
		{
			lock (parent)
			{
				this.deletedDocs = parent.deletedDocs;
			}
			this.maxDoc = parent.MaxDoc();
		}
		
		public virtual void  Seek(Term term)
		{
			if (term == null)
			{
				doc = - 1;
			}
			else
			{
				throw new System.NotSupportedException();
			}
		}
		
		public virtual void  Seek(TermEnum termEnum)
		{
			throw new System.NotSupportedException();
		}
		
		public virtual int Doc()
		{
			return doc;
		}
		
		public virtual int Freq()
		{
			return 1;
		}
		
		public virtual bool Next()
		{
			return SkipTo(doc + 1);
		}
		
		public virtual int Read(int[] docs, int[] freqs)
		{
			int length = docs.Length;
			int i = 0;
			while (i < length && doc < maxDoc)
			{
				if (deletedDocs == null || !deletedDocs.Get(doc))
				{
					docs[i] = doc;
					freqs[i] = 1;
					++i;
				}
				doc++;
			}
			return i;
		}
		
		public virtual bool SkipTo(int target)
		{
			doc = target;
			while (doc < maxDoc)
			{
				if (deletedDocs == null || !deletedDocs.Get(doc))
				{
					return true;
				}
				doc++;
			}
			return false;
		}
		
		public virtual void  Close()
		{
		}
	}
}