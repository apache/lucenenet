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
using IndexInput = Lucene.Net.Store.IndexInput;

namespace Lucene.Net.Index
{
	
	sealed public class SegmentInfo : System.ICloneable
	{
		public System.String name; // unique name in dir
		public int docCount; // number of docs in seg
		public Directory dir; // where segment resides
		
		private bool preLockless; // true if this is a segments file written before
		// lock-less commits (2.1)
		
		private long delGen; // current generation of del file; -1 if there
		// are no deletes; 0 if it's a pre-2.1 segment
		// (and we must check filesystem); 1 or higher if
		// there are deletes at generation N
		
		private long[] normGen; // current generations of each field's norm file.
		// If this array is null, we must check filesystem
		// when preLockLess is true.  Else,
		// there are no separate norms
		
		private sbyte isCompoundFile; // -1 if it is not; 1 if it is; 0 if it's
		// pre-2.1 (ie, must check file system to see
		// if <name>.cfs and <name>.nrm exist)         
		
		private bool hasSingleNormFile; // true if this segment maintains norms in a single file; 
		// false otherwise
		// this is currently false for segments populated by DocumentWriter
		// and true for newly created merged segments (both
		// compound and non compound).
		
		public SegmentInfo(System.String name, int docCount, Directory dir)
		{
			this.name = name;
			this.docCount = docCount;
			this.dir = dir;
			delGen = - 1;
			isCompoundFile = 0;
			preLockless = true;
			hasSingleNormFile = false;
		}
		
		public SegmentInfo(System.String name, int docCount, Directory dir, bool isCompoundFile, bool hasSingleNormFile) : this(name, docCount, dir)
		{
			this.isCompoundFile = (sbyte) (isCompoundFile ? 1 : - 1);
			this.hasSingleNormFile = hasSingleNormFile;
			preLockless = false;
		}
		
		/// <summary> Copy everything from src SegmentInfo into our instance.</summary>
		internal void  Reset(SegmentInfo src)
		{
			name = src.name;
			docCount = src.docCount;
			dir = src.dir;
			preLockless = src.preLockless;
			delGen = src.delGen;
			if (src.normGen == null)
			{
				normGen = null;
			}
			else
			{
				normGen = new long[src.normGen.Length];
				Array.Copy(src.normGen, 0, normGen, 0, src.normGen.Length);
			}
			isCompoundFile = src.isCompoundFile;
			hasSingleNormFile = src.hasSingleNormFile;
		}
		
		/// <summary> Construct a new SegmentInfo instance by reading a
		/// previously saved SegmentInfo from input.
		/// 
		/// </summary>
		/// <param name="dir">directory to load from
		/// </param>
		/// <param name="format">format of the segments info file
		/// </param>
		/// <param name="input">input handle to read segment info from
		/// </param>
		public SegmentInfo(Directory dir, int format, IndexInput input)
		{
			this.dir = dir;
			name = input.ReadString();
			docCount = input.ReadInt();
			if (format <= SegmentInfos.FORMAT_LOCKLESS)
			{
				delGen = input.ReadLong();
				if (format <= SegmentInfos.FORMAT_SINGLE_NORM_FILE)
				{
					hasSingleNormFile = (1 == input.ReadByte());
				}
				else
				{
					hasSingleNormFile = false;
				}
				int numNormGen = input.ReadInt();
				if (numNormGen == - 1)
				{
					normGen = null;
				}
				else
				{
					normGen = new long[numNormGen];
					for (int j = 0; j < numNormGen; j++)
					{
						normGen[j] = input.ReadLong();
					}
				}
				isCompoundFile = (sbyte) input.ReadByte();
				preLockless = isCompoundFile == 0;
			}
			else
			{
				delGen = 0;
				normGen = null;
				isCompoundFile = 0;
				preLockless = true;
				hasSingleNormFile = false;
			}
		}
		
		internal void  SetNumFields(int numFields)
		{
			if (normGen == null)
			{
				// normGen is null if we loaded a pre-2.1 segment
				// file, or, if this segments file hasn't had any
				// norms set against it yet:
				normGen = new long[numFields];
				
				if (!preLockless)
				{
					// This is a FORMAT_LOCKLESS segment, which means
					// there are no norms:
					for (int i = 0; i < numFields; i++)
					{
						normGen[i] = - 1;
					}
				}
			}
		}
		
		internal bool HasDeletions()
		{
			// Cases:
			//
			//   delGen == -1: this means this segment was written
			//     by the LOCKLESS code and for certain does not have
			//     deletions yet
			//
			//   delGen == 0: this means this segment was written by
			//     pre-LOCKLESS code which means we must check
			//     directory to see if .del file exists
			//
			//   delGen > 0: this means this segment was written by
			//     the LOCKLESS code and for certain has
			//     deletions
			//
			if (delGen == - 1)
			{
				return false;
			}
			else if (delGen > 0)
			{
				return true;
			}
			else
			{
				return dir.FileExists(GetDelFileName());
			}
		}
		
		internal void  AdvanceDelGen()
		{
			// delGen 0 is reserved for pre-LOCKLESS format
			if (delGen == - 1)
			{
				delGen = 1;
			}
			else
			{
				delGen++;
			}
		}
		
		internal void  ClearDelGen()
		{
			delGen = - 1;
		}
		
		public System.Object Clone()
		{
			SegmentInfo si = new SegmentInfo(name, docCount, dir);
			si.isCompoundFile = isCompoundFile;
			si.delGen = delGen;
			si.preLockless = preLockless;
			si.hasSingleNormFile = hasSingleNormFile;
			if (normGen != null)
			{
				si.normGen = new long[normGen.Length];
				normGen.CopyTo(si.normGen, 0);
			}
			return si;
		}
		
		internal System.String GetDelFileName()
		{
			if (delGen == - 1)
			{
				// In this case we know there is no deletion filename
				// against this segment
				return null;
			}
			else
			{
				// If delGen is 0, it's the pre-lockless-commit file format
				return IndexFileNames.FileNameFromGeneration(name, ".del", delGen);
			}
		}
		
		/// <summary> Returns true if this field for this segment has saved a separate norms file (_<segment>_N.sX).
		/// 
		/// </summary>
		/// <param name="fieldNumber">the field index to check
		/// </param>
		internal bool HasSeparateNorms(int fieldNumber)
		{
			if ((normGen == null && preLockless) || (normGen != null && normGen[fieldNumber] == 0))
			{
				// Must fallback to directory file exists check:
				System.String fileName = name + ".s" + fieldNumber;
				return dir.FileExists(fileName);
			}
			else if (normGen == null || normGen[fieldNumber] == - 1)
			{
				return false;
			}
			else
			{
				return true;
			}
		}
		
		/// <summary> Returns true if any fields in this segment have separate norms.</summary>
		internal bool HasSeparateNorms()
		{
			if (normGen == null)
			{
				if (!preLockless)
				{
					// This means we were created w/ LOCKLESS code and no
					// norms are written yet:
					return false;
				}
				else
				{
					// This means this segment was saved with pre-LOCKLESS
					// code.  So we must fallback to the original
					// directory list check:
					System.String[] result = dir.List();
					System.String pattern;
					pattern = name + ".s";
					int patternLength = pattern.Length;
					for (int i = 0; i < result.Length; i++)
					{
						if (result[i].StartsWith(pattern) && System.Char.IsDigit(result[i][patternLength]))
							return true;
					}
					return false;
				}
			}
			else
			{
				// This means this segment was saved with LOCKLESS
				// code so we first check whether any normGen's are >
				// 0 (meaning they definitely have separate norms):
				for (int i = 0; i < normGen.Length; i++)
				{
					if (normGen[i] > 0)
					{
						return true;
					}
				}
				// Next we look for any == 0.  These cases were
				// pre-LOCKLESS and must be checked in directory:
				for (int i = 0; i < normGen.Length; i++)
				{
					if (normGen[i] == 0)
					{
						if (HasSeparateNorms(i))
						{
							return true;
						}
					}
				}
			}
			
			return false;
		}
		
		/// <summary> Increment the generation count for the norms file for
		/// this field.
		/// 
		/// </summary>
		/// <param name="fieldIndex">field whose norm file will be rewritten
		/// </param>
		internal void  AdvanceNormGen(int fieldIndex)
		{
			if (normGen[fieldIndex] == - 1)
			{
				normGen[fieldIndex] = 1;
			}
			else
			{
				normGen[fieldIndex]++;
			}
		}
		
		/// <summary> Get the file name for the norms file for this field.
		/// 
		/// </summary>
		/// <param name="number">field index
		/// </param>
		internal System.String GetNormFileName(int number)
		{
			System.String prefix;
			
			long gen;
			if (normGen == null)
			{
				gen = 0;
			}
			else
			{
				gen = normGen[number];
			}
			
			if (HasSeparateNorms(number))
			{
				// case 1: separate norm
				prefix = ".s";
				return IndexFileNames.FileNameFromGeneration(name, prefix + number, gen);
			}
			
			if (hasSingleNormFile)
			{
				// case 2: lockless (or nrm file exists) - single file for all norms 
				prefix = "." + IndexFileNames.NORMS_EXTENSION;
				return IndexFileNames.FileNameFromGeneration(name, prefix, 0);
			}
			
			// case 3: norm file for each field
			prefix = ".f";
			return IndexFileNames.FileNameFromGeneration(name, prefix + number, 0);
		}
		
		/// <summary> Mark whether this segment is stored as a compound file.
		/// 
		/// </summary>
		/// <param name="isCompoundFile">true if this is a compound file;
		/// else, false
		/// </param>
		internal void  SetUseCompoundFile(bool isCompoundFile)
		{
			if (isCompoundFile)
			{
				this.isCompoundFile = 1;
			}
			else
			{
				this.isCompoundFile = - 1;
			}
		}
		
		/// <summary> Returns true if this segment is stored as a compound
		/// file; else, false.
		/// </summary>
		internal bool GetUseCompoundFile()
		{
			if (isCompoundFile == - 1)
			{
				return false;
			}
			else if (isCompoundFile == 1)
			{
				return true;
			}
			else
			{
				return dir.FileExists(name + ".cfs");
			}
		}
		
		/// <summary> Save this segment's info.</summary>
		internal void  Write(IndexOutput output)
		{
			output.WriteString(name);
			output.WriteInt(docCount);
			output.WriteLong(delGen);
			output.WriteByte((byte) (hasSingleNormFile ? 1 : 0));
			if (normGen == null)
			{
				output.WriteInt(- 1);
			}
			else
			{
				output.WriteInt(normGen.Length);
				for (int j = 0; j < normGen.Length; j++)
				{
					output.WriteLong(normGen[j]);
				}
			}
			output.WriteByte((byte) isCompoundFile);
		}
	}
}