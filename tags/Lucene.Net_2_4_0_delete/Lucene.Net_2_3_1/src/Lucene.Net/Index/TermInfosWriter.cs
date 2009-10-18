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

namespace Lucene.Net.Index
{
	
	/// <summary>This stores a monotonically increasing set of <Term, TermInfo> pairs in a
	/// Directory.  A TermInfos can be written once, in order.  
	/// </summary>
	
	public sealed class TermInfosWriter
	{
		/// <summary>The file format version, a negative number. </summary>
		public const int FORMAT = - 3;
		
		private FieldInfos fieldInfos;
		private IndexOutput output;
		private TermInfo lastTi = new TermInfo();
		private long size;
		
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
		
		/// <summary>Expert: The maximum number of skip levels. Smaller values result in 
		/// slightly smaller indexes, but slower skipping in big posting lists.
		/// </summary>
		internal int maxSkipLevels = 10;
		
		private long lastIndexPointer;
		private bool isIndex;
		private char[] lastTermText = new char[10];
		private int lastTermTextLength;
		private int lastFieldNumber = - 1;
		
		private char[] termTextBuffer = new char[10];
		
		private TermInfosWriter other;
		
		public TermInfosWriter(Directory directory, System.String segment, FieldInfos fis, int interval)
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
			output.WriteInt(maxSkipLevels); // write maxSkipLevels
		}
		
		internal void  Add(Term term, TermInfo ti)
		{
			
			int length = term.text.Length;
			if (termTextBuffer.Length < length)
			{
				termTextBuffer = new char[(int) (length * 1.25)];
			}

            int i = 0;
            System.Collections.Generic.IEnumerator<char> chars = term.text.GetEnumerator();
            while (chars.MoveNext())
            {
                termTextBuffer[i++] = (char)chars.Current;
            }
			
			Add(fieldInfos.FieldNumber(term.field), termTextBuffer, 0, length, ti);
		}
		
		// Currently used only by assert statement
		private int CompareToLastTerm(int fieldNumber, char[] termText, int start, int length)
		{
			int pos = 0;
			
			if (lastFieldNumber != fieldNumber)
			{
				int cmp = String.CompareOrdinal(fieldInfos.FieldName(lastFieldNumber), fieldInfos.FieldName(fieldNumber));
				// If there is a field named "" (empty string) then we
				// will get 0 on this comparison, yet, it's "OK".  But
				// it's not OK if two different field numbers map to
				// the same name.
				if (cmp != 0 || lastFieldNumber != - 1)
					return cmp;
			}
			
			while (pos < length && pos < lastTermTextLength)
			{
				char c1 = lastTermText[pos];
				char c2 = termText[pos + start];
				if (c1 < c2)
					return - 1;
				else if (c1 > c2)
					return 1;
				pos++;
			}
			
			if (pos < lastTermTextLength)
			// Last term was longer
				return 1;
			else if (pos < length)
			// Last term was shorter
				return - 1;
			else
				return 0;
		}
		
		/// <summary>Adds a new <<fieldNumber, termText>, TermInfo> pair to the set.
		/// Term must be lexicographically greater than all previous Terms added.
		/// TermInfo pointers must be positive and greater than all previous.
		/// </summary>
		internal void  Add(int fieldNumber, char[] termText, int termTextStart, int termTextLength, TermInfo ti)
		{
			
			System.Diagnostics.Debug.Assert(CompareToLastTerm(fieldNumber, termText, termTextStart, termTextLength) < 0 ||
				(isIndex && termTextLength == 0 && lastTermTextLength == 0),
				"Terms are out of order: field=" + fieldInfos.FieldName(fieldNumber) +  "(number " + fieldNumber + ")" + 
				" lastField=" + fieldInfos.FieldName(lastFieldNumber) + " (number " + lastFieldNumber + ")" + 
				" text=" + new String(termText, termTextStart, termTextLength) + " lastText=" + new String(lastTermText, 0, lastTermTextLength));
			
			System.Diagnostics.Debug.Assert(ti.freqPointer >= lastTi.freqPointer, "freqPointer out of order (" + ti.freqPointer + " < " + lastTi.freqPointer + ")");
			System.Diagnostics.Debug.Assert(ti.proxPointer >= lastTi.proxPointer, "proxPointer out of order (" + ti.proxPointer + " < " + lastTi.proxPointer + ")");
			
			if (!isIndex && size % indexInterval == 0)
				other.Add(lastFieldNumber, lastTermText, 0, lastTermTextLength, lastTi); // add an index term
			
			WriteTerm(fieldNumber, termText, termTextStart, termTextLength); // write term
			
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
			
			if (lastTermText.Length < termTextLength)
			{
				lastTermText = new char[(int) (termTextLength * 1.25)];
			}
			Array.Copy(termText, termTextStart, lastTermText, 0, termTextLength);
			lastTermTextLength = termTextLength;
			lastFieldNumber = fieldNumber;
			
			lastTi.Set(ti);
			size++;
		}
		
		private void  WriteTerm(int fieldNumber, char[] termText, int termTextStart, int termTextLength)
		{
			
			// Compute prefix in common with last term:
			int start = 0;
			int limit = termTextLength < lastTermTextLength ? termTextLength : lastTermTextLength;
			while (start < limit)
			{
				if (termText[termTextStart + start] != lastTermText[start])
					break;
				start++;
			}
			
			int length = termTextLength - start;
			
			output.WriteVInt(start); // write shared prefix length
			output.WriteVInt(length); // write delta length
			output.WriteChars(termText, start + termTextStart, length); // write delta chars
			output.WriteVInt(fieldNumber); // write field num
		}
		
		/// <summary>Called to complete TermInfos creation. </summary>
		internal void  Close()
		{
			output.Seek(4); // write size after format
			output.WriteLong(size);
			output.Close();
			
			if (!isIndex)
				other.Close();
		}
	}
}