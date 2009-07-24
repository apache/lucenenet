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

using Document = Lucene.Net.Documents.Document;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;

namespace Lucene.Net.Index
{
	
	/// <summary> Basic tool to check the health of an index and write a
	/// new segments file that removes reference to problematic
	/// segments.  There are many more checks that this tool
	/// could do but does not yet, eg: reconstructing a segments
	/// file by looking for all loadable segments (if no segments
	/// file is found), removing specifically specified segments,
	/// listing files that exist but are not referenced, etc.
	/// </summary>
	
	public class CheckIndex
	{

        public static System.IO.TextWriter out_Renamed;
		
		private class MySegmentTermDocs : SegmentTermDocs
		{
			
			internal int delCount;
			
			internal MySegmentTermDocs(SegmentReader p) : base(p)
			{
			}
			
			public override void  Seek(Term term)
			{
				base.Seek(term);
				delCount = 0;
			}
			
			protected internal override void  SkippingDoc()
			{
				delCount++;
			}
		}
		
		/// <summary>Returns true if index is clean, else false.</summary>
		public static bool Check(Directory dir, bool doFix)
		{
			System.Globalization.NumberFormatInfo nf = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;
			SegmentInfos sis = new SegmentInfos();
			
			try
			{
				sis.Read(dir);
			}
			catch (System.Exception t)
			{
				out_Renamed.WriteLine("ERROR: could not read any segments file in directory");
				out_Renamed.Write(t.StackTrace);
				out_Renamed.Flush();
				return false;
			}
			
			int numSegments = sis.Count;
			System.String segmentsFileName = sis.GetCurrentSegmentFileName();
			IndexInput input = null;
			try
			{
				input = dir.OpenInput(segmentsFileName);
			}
			catch (System.Exception t)
			{
				out_Renamed.WriteLine("ERROR: could not open segments file in directory");
				out_Renamed.Write(t.StackTrace);
				out_Renamed.Flush();
				return false;
			}
			int format = 0;
			try
			{
				format = input.ReadInt();
			}
			catch (System.Exception t)
			{
				out_Renamed.WriteLine("ERROR: could not read segment file version in directory");
				out_Renamed.Write(t.StackTrace);
				out_Renamed.Flush();
				return false;
			}
			finally
			{
				if (input != null)
					input.Close();
			}
			
			System.String sFormat = "";
			bool skip = false;
			
			if (format == SegmentInfos.FORMAT)
				sFormat = "FORMAT [Lucene Pre-2.1]";
			if (format == SegmentInfos.FORMAT_LOCKLESS)
				sFormat = "FORMAT_LOCKLESS [Lucene 2.1]";
			else if (format == SegmentInfos.FORMAT_SINGLE_NORM_FILE)
				sFormat = "FORMAT_SINGLE_NORM_FILE [Lucene 2.2]";
			else if (format == SegmentInfos.FORMAT_SHARED_DOC_STORE)
				sFormat = "FORMAT_SHARED_DOC_STORE [Lucene 2.3]";
			else if (format < SegmentInfos.FORMAT_SHARED_DOC_STORE)
			{
				sFormat = "int=" + format + " [newer version of Lucene than this tool]";
				skip = true;
			}
			else
			{
				sFormat = format + " [Lucene 1.3 or prior]";
			}
			
			out_Renamed.WriteLine("Segments file=" + segmentsFileName + " numSegments=" + numSegments + " version=" + sFormat);
			
			if (skip)
			{
				out_Renamed.WriteLine("\nERROR: this index appears to be created by a newer version of Lucene than this tool was compiled on; please re-compile this tool on the matching version of Lucene; exiting");
				return false;
			}
			
			SegmentInfos newSIS = (SegmentInfos) sis.Clone();
			newSIS.Clear();
			bool changed = false;
			int totLoseDocCount = 0;
			int numBadSegments = 0;
			for (int i = 0; i < numSegments; i++)
			{
				SegmentInfo info = sis.Info(i);
				out_Renamed.WriteLine("  " + (1 + i) + " of " + numSegments + ": name=" + info.name + " docCount=" + info.docCount);
				int toLoseDocCount = info.docCount;
				
				SegmentReader reader = null;
				
				try
				{
					out_Renamed.WriteLine("    compound=" + info.GetUseCompoundFile());
					out_Renamed.WriteLine("    numFiles=" + info.Files().Count);
					out_Renamed.WriteLine(String.Format(nf, "    size (MB)={0:f}", new Object[] { (info.SizeInBytes() / (1024.0 * 1024.0)) }));
					int docStoreOffset = info.GetDocStoreOffset();
					if (docStoreOffset != - 1)
					{
						out_Renamed.WriteLine("    docStoreOffset=" + docStoreOffset);
						out_Renamed.WriteLine("    docStoreSegment=" + info.GetDocStoreSegment());
						out_Renamed.WriteLine("    docStoreIsCompoundFile=" + info.GetDocStoreIsCompoundFile());
					}
					System.String delFileName = info.GetDelFileName();
					if (delFileName == null)
						out_Renamed.WriteLine("    no deletions");
					else
						out_Renamed.WriteLine("    has deletions [delFileName=" + delFileName + "]");
					out_Renamed.Write("    test: open reader.........");
					reader = SegmentReader.Get(info);
					int numDocs = reader.NumDocs();
					toLoseDocCount = numDocs;
					if (reader.HasDeletions())
						out_Renamed.WriteLine("OK [" + (info.docCount - numDocs) + " deleted docs]");
					else
						out_Renamed.WriteLine("OK");
					
					out_Renamed.Write("    test: fields, norms.......");
                    System.Collections.ICollection fieldNames = reader.GetFieldNames(IndexReader.FieldOption.ALL);
                    System.Collections.IEnumerator it = fieldNames.GetEnumerator();
					while (it.MoveNext())
					{
						System.String fieldName = (System.String) it.Current;
						byte[] b = reader.Norms(fieldName);
						if (b.Length != info.docCount)
							throw new System.SystemException("norms for field \"" + fieldName + "\" is length " + b.Length + " != maxDoc " + info.docCount);
					}
					out_Renamed.WriteLine("OK [" + fieldNames.Count + " fields]");
					
					out_Renamed.Write("    test: terms, freq, prox...");
					TermEnum termEnum = reader.Terms();
					TermPositions termPositions = reader.TermPositions();
					
					// Used only to count up # deleted docs for this
					// term
					MySegmentTermDocs myTermDocs = new MySegmentTermDocs(reader);
					
					long termCount = 0;
					long totFreq = 0;
					long totPos = 0;
					while (termEnum.Next())
					{
						termCount++;
						Term term = termEnum.Term();
						int docFreq = termEnum.DocFreq();
						termPositions.Seek(term);
						int lastDoc = - 1;
						int freq0 = 0;
						totFreq += docFreq;
						while (termPositions.Next())
						{
							freq0++;
							int doc = termPositions.Doc();
							int freq = termPositions.Freq();
							if (doc <= lastDoc)
							{
								throw new System.SystemException("term " + term + ": doc " + doc + " < lastDoc " + lastDoc);
							}
							lastDoc = doc;
							if (freq <= 0)
							{
								throw new System.SystemException("term " + term + ": doc " + doc + ": freq " + freq + " is out of bounds");
							}
							
							int lastPos = - 1;
							totPos += freq;
							for (int j = 0; j < freq; j++)
							{
								int pos = termPositions.NextPosition();
								if (pos < -1)
								{
									throw new System.SystemException("term " + term + ": doc " + doc + ": pos " + pos + " is out of bounds");
								}
								if (pos < lastPos)
								{
									throw new System.SystemException("term " + term + ": doc " + doc + ": pos " + pos + " < lastPos " + lastPos);
								}
							}
						}
						
						// Now count how many deleted docs occurred in
						// this term:
						int delCount;
						if (reader.HasDeletions())
						{
							myTermDocs.Seek(term);
							while (myTermDocs.Next())
							{
							}
							delCount = myTermDocs.delCount;
						}
						else
							delCount = 0;
						
						if (freq0 + delCount != docFreq)
						{
							throw new System.SystemException("term " + term + " docFreq=" + docFreq + " != num docs seen " + freq0 + " + num docs deleted " + delCount);
						}
					}
					
					out_Renamed.WriteLine("OK [" + termCount + " terms; " + totFreq + " terms/docs pairs; " + totPos + " tokens]");
					
					out_Renamed.Write("    test: stored fields.......");
					int docCount = 0;
					long totFields = 0;
					for (int j = 0; j < info.docCount; j++)
						if (!reader.IsDeleted(j))
						{
							docCount++;
							Document doc = reader.Document(j);
							totFields += doc.GetFields().Count;
						}
					
					if (docCount != reader.NumDocs())
						throw new System.SystemException("docCount=" + docCount + " but saw " + docCount + " undeleted docs");
					
					out_Renamed.WriteLine(String.Format(nf, "OK [{0:d} total field count; avg {1:f} fields per doc]", new Object[] { totFields, (((float)totFields) / docCount) }));
					
					out_Renamed.Write("    test: term vectors........");
					int totVectors = 0;
					for (int j = 0; j < info.docCount; j++)
						if (!reader.IsDeleted(j))
						{
							TermFreqVector[] tfv = reader.GetTermFreqVectors(j);
							if (tfv != null)
								totVectors += tfv.Length;
						}
					
					out_Renamed.WriteLine(String.Format(nf, "OK [{0:d} total vector count; avg {1:f} term/freq vector fields per doc]", new Object[] { totVectors, (((float)totVectors) / docCount) }));
					out_Renamed.WriteLine("");
				}
				catch (System.Exception t)
				{
					out_Renamed.WriteLine("FAILED");
					System.String comment;
					if (doFix)
						comment = "will remove reference to this segment (-fix is specified)";
					else
						comment = "would remove reference to this segment (-fix was not specified)";
					out_Renamed.WriteLine("    WARNING: " + comment + "; full exception:");
					out_Renamed.Write(t.StackTrace);
					out_Renamed.Flush();
					out_Renamed.WriteLine("");
					totLoseDocCount += toLoseDocCount;
					numBadSegments++;
					changed = true;
					continue;
				}
				finally
				{
					if (reader != null)
						reader.Close();
				}
				
				// Keeper
				newSIS.Add(info.Clone());
			}
			
			if (!changed)
			{
				out_Renamed.WriteLine("No problems were detected with this index.\n");
				return true;
			}
			else
			{
				out_Renamed.WriteLine("WARNING: " + numBadSegments + " broken segments detected");
				if (doFix)
					out_Renamed.WriteLine("WARNING: " + totLoseDocCount + " documents will be lost");
				else
					out_Renamed.WriteLine("WARNING: " + totLoseDocCount + " documents would be lost if -fix were specified");
				out_Renamed.WriteLine();
			}
			
			if (doFix)
			{
				out_Renamed.WriteLine("NOTE: will write new segments file in 5 seconds; this will remove " + totLoseDocCount + " docs from the index. THIS IS YOUR LAST CHANCE TO CTRL+C!");
				for (int i = 0; i < 5; i++)
				{
					try
					{
						System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 1000));
					}
					catch (System.Threading.ThreadInterruptedException)
					{
						SupportClass.ThreadClass.Current().Interrupt();
						i--;
						continue;
					}
					
					out_Renamed.WriteLine("  " + (5 - i) + "...");
				}
				out_Renamed.Write("Writing...");
				try
				{
					newSIS.Write(dir);
				}
				catch (System.Exception t)
				{
					out_Renamed.WriteLine("FAILED; exiting");
					out_Renamed.Write(t.StackTrace);
					out_Renamed.Flush();
					return false;
				}
				out_Renamed.WriteLine("OK");
				out_Renamed.WriteLine("Wrote new segments file \"" + newSIS.GetCurrentSegmentFileName() + "\"");
			}
			else
			{
				out_Renamed.WriteLine("NOTE: would write new segments file [-fix was not specified]");
			}
			out_Renamed.WriteLine("");
			
			return false;
		}

        static bool assertsOn;

        private static bool TestAsserts()
        {
            assertsOn = true;
            return true;
        }

		[STAThread]
		public static void  Main(System.String[] args)
		{
			
			bool doFix = false;
			for (int i = 0; i < args.Length; i++)
				if (args[i].Equals("-fix"))
				{
					doFix = true;
					break;
				}
			
			if (args.Length != (doFix ? 2 : 1))
			{
				out_Renamed.WriteLine("\nUsage: java Lucene.Net.Index.CheckIndex pathToIndex [-fix]\n" + "\n" + "  -fix: actually write a new segments_N file, removing any problematic segments\n" + "\n" + "**WARNING**: -fix should only be used on an emergency basis as it will cause\n" + "documents (perhaps many) to be permanently removed from the index.  Always make\n" + "a backup copy of your index before running this!  Do not run this tool on an index\n" + "that is actively being written to.  You have been warned!\n" + "\n" + "Run without -fix, this tool will open the index, report version information\n" + "and report any exceptions it hits and what action it would take if -fix were\n" + "specified.  With -fix, this tool will remove any segments that have issues and\n" + "write a new segments_N file.  This means all documents contained in the affected\n" + "segments will be removed.\n" + "\n" + "This tool exits with exit code 1 if the index cannot be opened or has has any\n" + "corruption, else 0.\n");
				System.Environment.Exit(1);
			}

            System.Diagnostics.Debug.Assert(TestAsserts());
            if (!assertsOn)
                System.Console.WriteLine("\nNote: testing will be more thorough if you run with System.Diagnostic.Debug.Assert() enabled.");

			System.String dirName = args[0];
			out_Renamed.WriteLine("\nOpening index @ " + dirName + "\n");
			Directory dir = null;
			try
			{
				dir = FSDirectory.GetDirectory(dirName);
			}
			catch (System.Exception t)
			{
				out_Renamed.WriteLine("ERROR: could not open directory \"" + dirName + "\"; exiting");
				out_Renamed.Write(t.StackTrace);
				out_Renamed.Flush();
				System.Environment.Exit(1);
			}
			
			bool isClean = Check(dir, doFix);
			
			int exitCode;
			if (isClean)
				exitCode = 0;
			else
				exitCode = 1;
			System.Environment.Exit(exitCode);
		}
		static CheckIndex()
		{
			System.IO.StreamWriter temp_writer;
            temp_writer = new System.IO.StreamWriter(System.Console.OpenStandardOutput(), System.Console.Out.Encoding);
			temp_writer.AutoFlush = true;
			out_Renamed = temp_writer;
		}
	}
}
