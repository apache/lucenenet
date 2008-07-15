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

using IndexInput = Lucene.Net.Store.IndexInput;
using BitVector = Lucene.Net.Util.BitVector;

namespace Lucene.Net.Index
{
	
	public class SegmentTermDocs : TermDocs
	{
		protected internal SegmentReader parent;
		protected internal IndexInput freqStream;
		protected internal int count;
		protected internal int df;
		protected internal BitVector deletedDocs;
		internal int doc = 0;
		internal int freq;
		
		private int skipInterval;
		private int maxSkipLevels;
		private DefaultSkipListReader skipListReader;
		
		private long freqBasePointer;
		private long proxBasePointer;
		
		private long skipPointer;
		private bool haveSkipped;
		
		protected internal bool currentFieldStoresPayloads;

        // for testing
        public IndexInput FreqStream_ForNUnitTest
        {
            get { return freqStream; }
            set { freqStream = value; }
        }
		
		public SegmentTermDocs(SegmentReader parent)
		{
			this.parent = parent;
			this.freqStream = (IndexInput) parent.freqStream.Clone();
			this.deletedDocs = parent.deletedDocs;
			this.skipInterval = parent.tis.GetSkipInterval();
			this.maxSkipLevels = parent.tis.GetMaxSkipLevels();
		}
		
		public virtual void  Seek(Term term)
		{
			TermInfo ti = parent.tis.Get(term);
			Seek(ti, term);
		}
		
		public virtual void  Seek(TermEnum termEnum)
		{
			TermInfo ti;
			Term term;
			
			// use comparison of fieldinfos to verify that termEnum belongs to the same segment as this SegmentTermDocs
			if (termEnum is SegmentTermEnum && ((SegmentTermEnum) termEnum).fieldInfos == parent.fieldInfos)
			{
				// optimized case
				SegmentTermEnum segmentTermEnum = ((SegmentTermEnum) termEnum);
				term = segmentTermEnum.Term();
				ti = segmentTermEnum.TermInfo();
			}
			else
			{
				// punt case
				term = termEnum.Term();
				ti = parent.tis.Get(term);
			}
			
			Seek(ti, term);
		}
		
		internal virtual void  Seek(TermInfo ti, Term term)
		{
			count = 0;
			FieldInfo fi = parent.fieldInfos.FieldInfo(term.field);
			currentFieldStoresPayloads = (fi != null) ? fi.storePayloads : false;
			if (ti == null)
			{
				df = 0;
			}
			else
			{
				df = ti.docFreq;
				doc = 0;
				freqBasePointer = ti.freqPointer;
				proxBasePointer = ti.proxPointer;
				skipPointer = freqBasePointer + ti.skipOffset;
				freqStream.Seek(freqBasePointer);
				haveSkipped = false;
			}
		}
		
		public virtual void  Close()
		{
			freqStream.Close();
			if (skipListReader != null)
				skipListReader.Close();
		}
		
		public int Doc()
		{
			return doc;
		}
		public int Freq()
		{
			return freq;
		}
		
		protected internal virtual void  SkippingDoc()
		{
		}
		
		public virtual bool Next()
		{
			while (true)
			{
				if (count == df)
					return false;
				
				int docCode = freqStream.ReadVInt();
				doc += (int) (((uint) docCode) >> 1); // shift off low bit
				if ((docCode & 1) != 0)
				// if low bit is set
					freq = 1;
				// freq is one
				else
					freq = freqStream.ReadVInt(); // else read freq
				
				count++;
				
				if (deletedDocs == null || !deletedDocs.Get(doc))
					break;
				SkippingDoc();
			}
			return true;
		}
		
		/// <summary>Optimized implementation. </summary>
		public virtual int Read(int[] docs, int[] freqs)
		{
			int length = docs.Length;
			int i = 0;
			while (i < length && count < df)
			{
				
				// manually inlined call to next() for speed
				int docCode = freqStream.ReadVInt();
				doc += (int) (((uint) docCode) >> 1); // shift off low bit
				if ((docCode & 1) != 0)
				// if low bit is set
					freq = 1;
				// freq is one
				else
					freq = freqStream.ReadVInt(); // else read freq
				count++;
				
				if (deletedDocs == null || !deletedDocs.Get(doc))
				{
					docs[i] = doc;
					freqs[i] = freq;
					++i;
				}
			}
			return i;
		}
		
		/// <summary>Overridden by SegmentTermPositions to skip in prox stream. </summary>
		protected internal virtual void  SkipProx(long proxPointer)
		{
		}
		
        protected internal virtual void SkipProx(long proxPointer, int payloadLength)
        {
        }
		
		/// <summary>Optimized implementation. </summary>
		public virtual bool SkipTo(int target)
		{
			if (df >= skipInterval)
			{
				// optimized case
				if (skipListReader == null)
					skipListReader = new DefaultSkipListReader((IndexInput) freqStream.Clone(), maxSkipLevels, skipInterval); // lazily clone
				
				if (!haveSkipped)
				{
					// lazily initialize skip stream
					skipListReader.Init(skipPointer, freqBasePointer, proxBasePointer, df, currentFieldStoresPayloads);
					haveSkipped = true;
				}
				
				int newCount = skipListReader.SkipTo(target);
				if (newCount > count)
				{
					freqStream.Seek(skipListReader.GetFreqPointer());
					SkipProx(skipListReader.GetProxPointer(), skipListReader.GetPayloadLength());
					
					doc = skipListReader.GetDoc();
					count = newCount;
				}
			}
			
			// done skipping, now just scan
			do 
			{
				if (!Next())
					return false;
			}
			while (target > doc);
			return true;
		}
	}
}