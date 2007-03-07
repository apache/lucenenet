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
using Directory = Lucene.Net.Store.Directory;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using StringHelper = Lucene.Net.Util.StringHelper;

namespace Lucene.Net.Index
{
	
	/// <summary>This stores a monotonically increasing set of <Term, TermInfo> pairs in a
	/// Directory.  A TermInfos can be written once, in order.  
	/// </summary>
	
	public sealed class TermInfosWriter
	{
		/// <summary>The file format version, a negative number. </summary>
		public const int FORMAT = - 2;
		
		private FieldInfos fieldInfos;
		private IndexOutput output;
		private Term lastTerm = new Term("", "");
		private TermInfo lastTi = new TermInfo();
		private long size = 0;
		
		// TODO: the default values for these two parameters should be settable from
		// IndexWriter.  However, once that's done, folks will start setting them to
		// ridiculous values and complaining that things don't work well, as with
		// mergeFactor.  So, let's wait until a number of folks find that alternate
		// values work better.  Note that both of these values are stored in the
		// segment, so that it's safe to change these w/o rebuilding all indexes.
		
		/// <summary>Expert: The fraction of terms in the "dictionary" which should be stored
		/// in RAM.  Smaller values use more memory, but make searching slightly
		/// faster, while larger values use less memory and make searching slightly
		/// slower.  Searching is typically not dominated by dictionary lookup, so
		/// tweaking this is rarely useful.
		/// </summary>
		internal int indexInterval = 128;
		
		/// <summary>Expert: The fraction of {@link TermDocs} entries stored in skip tables,
		/// used to accellerate {@link TermDocs#SkipTo(int)}.  Larger values result in
		/// smaller indexes, greater acceleration, but fewer accelerable cases, while
		/// smaller values result in bigger indexes, less acceleration and more
		/// accelerable cases. More detailed experiments would be useful here. 
		/// </summary>
		internal int skipInterval = 16;
		
		private long lastIndexPointer = 0;
		private bool isIndex = false;
		
		private TermInfosWriter other = null;
		
		public /*internal*/ TermInfosWriter(Directory directory, System.String segment, FieldInfos fis, int interval)
		{
			Initialize(directory, segment, fis, interval, false);
			other = new TermInfosWriter(directory, segment, fis, interval, true);
			other.other = this;
		}
		
		private TermInfosWriter(Directory directory, System.String segment, FieldInfos fis, int interval, bool isIndex)
		{
			Initialize(directory, segment, fis, interval, isIndex);
		}
		
		private void  Initialize(Directory directory, System.String segment, FieldInfos fis, int interval, bool isi)
		{
			indexInterval = interval;
			fieldInfos = fis;
			isIndex = isi;
			output = directory.CreateOutput(segment + (isIndex ? ".tii" : ".tis"));
			output.WriteInt(FORMAT); // write format
			output.WriteLong(0); // leave space for size
			output.WriteInt(indexInterval); // write indexInterval
			output.WriteInt(skipInterval); // write skipInterval
		}
		
		/// <summary>Adds a new <Term, TermInfo> pair to the set.
		/// Term must be lexicographically greater than all previous Terms added.
		/// TermInfo pointers must be positive and greater than all previous.
		/// </summary>
		public /*internal*/ void  Add(Term term, TermInfo ti)
		{
            if (!isIndex && term.CompareTo(lastTerm) <= 0)
            {
                throw new System.IO.IOException("term out of order (\"" + term + "\".compareTo(\"" + lastTerm + "\") <= 0)");
            }
			if (ti.freqPointer < lastTi.freqPointer)
				throw new System.IO.IOException("freqPointer out of order (" + ti.freqPointer + " < " + lastTi.freqPointer + ")");
			if (ti.proxPointer < lastTi.proxPointer)
				throw new System.IO.IOException("proxPointer out of order (" + ti.proxPointer + " < " + lastTi.proxPointer + ")");
			
			if (!isIndex && size % indexInterval == 0)
				other.Add(lastTerm, lastTi); // add an index term
			
			WriteTerm(term); // write term
			output.WriteVInt(ti.docFreq); // write doc freq
			output.WriteVLong(ti.freqPointer - lastTi.freqPointer); // write pointers
			output.WriteVLong(ti.proxPointer - lastTi.proxPointer);
			
			if (ti.docFreq >= skipInterval)
			{
				output.WriteVInt(ti.skipOffset);
			}
			
			if (isIndex)
			{
				output.WriteVLong(other.output.GetFilePointer() - lastIndexPointer);
				lastIndexPointer = other.output.GetFilePointer(); // write pointer
			}
			
			lastTi.Set(ti);
			size++;
		}
		
		private void  WriteTerm(Term term)
		{
			int start = StringHelper.StringDifference(lastTerm.text, term.text);
			int length = term.text.Length - start;
			
			output.WriteVInt(start); // write shared prefix length
			output.WriteVInt(length); // write delta length
			output.WriteChars(term.text, start, length); // write delta chars
			
			output.WriteVInt(fieldInfos.FieldNumber(term.field)); // write field num
			
			lastTerm = term;
		}
		
		
		
		/// <summary>Called to complete TermInfos creation. </summary>
		public /*internal*/ void  Close()
		{
			output.Seek(4); // write size after format
			output.WriteLong(size);
			output.Close();
			
			if (!isIndex)
				other.Close();
		}
	}
}