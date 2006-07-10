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
using NUnit.Framework;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	[TestFixture]
	public class TestTermVectorsReader
	{
		private void  InitBlock()
		{
			positions = new int[testTerms.Length][];
			offsets = new TermVectorOffsetInfo[testTerms.Length][];
		}
		private TermVectorsWriter writer = null;
		//Must be lexicographically sorted, will do in setup, versus trying to maintain here
		private System.String[] testFields = new System.String[]{"f1", "f2", "f3"};
		private bool[] testFieldsStorePos = new bool[]{true, false, true, false};
		private bool[] testFieldsStoreOff = new bool[]{true, false, false, true};
		private System.String[] testTerms = new System.String[]{"this", "is", "a", "test"};
		private int[][] positions;
		private TermVectorOffsetInfo[][] offsets;
		private RAMDirectory dir = new RAMDirectory();
		private System.String seg = "testSegment";
		private FieldInfos fieldInfos = new FieldInfos();
		
        public TestTermVectorsReader()
        {
            InitBlock();
        }

        public TestTermVectorsReader(System.String s)
		{
			InitBlock();
		}
		
		[SetUp]
        public virtual void  SetUp()
		{
			for (int i = 0; i < testFields.Length; i++)
			{
				fieldInfos.Add(testFields[i], true, true, testFieldsStorePos[i], testFieldsStoreOff[i]);
			}
			
			for (int i = 0; i < testTerms.Length; i++)
			{
				positions[i] = new int[3];
				for (int j = 0; j < positions[i].Length; j++)
				{
					// poditions are always sorted in increasing order
					positions[i][j] = (int) (j * 10 + (new System.Random().NextDouble()) * 10);
				}
				offsets[i] = new TermVectorOffsetInfo[3];
				for (int j = 0; j < offsets[i].Length; j++)
				{
					// ofsets are alway sorted in increasing order
					offsets[i][j] = new TermVectorOffsetInfo(j * 10, j * 10 + testTerms[i].Length);
				}
			}
			System.Array.Sort(testTerms);
			for (int j = 0; j < 5; j++)
			{
				writer = new TermVectorsWriter(dir, seg, fieldInfos);
				writer.OpenDocument();
				
				for (int k = 0; k < testFields.Length; k++)
				{
					writer.OpenField(testFields[k]);
					for (int i = 0; i < testTerms.Length; i++)
					{
						writer.AddTerm(testTerms[i], 3, positions[i], offsets[i]);
					}
					writer.CloseField();
				}
				writer.CloseDocument();
				writer.Close();
			}
		}
		
		[TearDown]
        public virtual void  TearDown()
		{
			
		}
		
		[Test]
        public virtual void  Test()
		{
			//Check to see the files were created properly in setup
			Assert.IsTrue(writer.IsDocumentOpen() == false);
			Assert.IsTrue(dir.FileExists(seg + TermVectorsWriter.TvdExtension));
			Assert.IsTrue(dir.FileExists(seg + TermVectorsWriter.TvxExtension));
		}
		
		[Test]
        public virtual void  TestReader()
		{
			TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
			Assert.IsTrue(reader != null);
			TermFreqVector vector = reader.Get(0, testFields[0]);
			Assert.IsTrue(vector != null);
			System.String[] terms = vector.GetTerms();
			Assert.IsTrue(terms != null);
			Assert.IsTrue(terms.Length == testTerms.Length);
			for (int i = 0; i < terms.Length; i++)
			{
				System.String term = terms[i];
				//System.out.println("Term: " + term);
				Assert.IsTrue(term.Equals(testTerms[i]));
			}
		}
		
		[Test]
        public virtual void  TestPositionReader()
		{
			TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
			Assert.IsTrue(reader != null);
			TermPositionVector vector;
			System.String[] terms;
			vector = (TermPositionVector) reader.Get(0, testFields[0]);
			Assert.IsTrue(vector != null);
			terms = vector.GetTerms();
			Assert.IsTrue(terms != null);
			Assert.IsTrue(terms.Length == testTerms.Length);
			for (int i = 0; i < terms.Length; i++)
			{
				System.String term = terms[i];
				//System.out.println("Term: " + term);
				Assert.IsTrue(term.Equals(testTerms[i]));
				int[] positions = vector.GetTermPositions(i);
				Assert.IsTrue(positions != null);
				Assert.IsTrue(positions.Length == this.positions[i].Length);
				for (int j = 0; j < positions.Length; j++)
				{
					int position = positions[j];
					Assert.IsTrue(position == this.positions[i][j]);
				}
				TermVectorOffsetInfo[] offset = vector.GetOffsets(i);
				Assert.IsTrue(offset != null);
				Assert.IsTrue(offset.Length == this.offsets[i].Length);
				for (int j = 0; j < offset.Length; j++)
				{
					TermVectorOffsetInfo termVectorOffsetInfo = offset[j];
					Assert.IsTrue(termVectorOffsetInfo.Equals(offsets[i][j]));
				}
			}
			
			TermFreqVector freqVector = reader.Get(0, testFields[1]); //no pos, no offset
			Assert.IsTrue(freqVector != null);
			Assert.IsTrue(freqVector is TermPositionVector == false);
			terms = freqVector.GetTerms();
			Assert.IsTrue(terms != null);
			Assert.IsTrue(terms.Length == testTerms.Length);
			for (int i = 0; i < terms.Length; i++)
			{
				System.String term = terms[i];
				//System.out.println("Term: " + term);
				Assert.IsTrue(term.Equals(testTerms[i]));
			}
		}
		
		[Test]
        public virtual void  TestOffsetReader()
		{
			TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
			Assert.IsTrue(reader != null);
			TermPositionVector vector = (TermPositionVector) reader.Get(0, testFields[0]);
			Assert.IsTrue(vector != null);
			System.String[] terms = vector.GetTerms();
			Assert.IsTrue(terms != null);
			Assert.IsTrue(terms.Length == testTerms.Length);
			for (int i = 0; i < terms.Length; i++)
			{
				System.String term = terms[i];
				//System.out.println("Term: " + term);
				Assert.IsTrue(term.Equals(testTerms[i]));
				int[] positions = vector.GetTermPositions(i);
				Assert.IsTrue(positions != null);
				Assert.IsTrue(positions.Length == this.positions[i].Length);
				for (int j = 0; j < positions.Length; j++)
				{
					int position = positions[j];
					Assert.IsTrue(position == this.positions[i][j]);
				}
				TermVectorOffsetInfo[] offset = vector.GetOffsets(i);
				Assert.IsTrue(offset != null);
				Assert.IsTrue(offset.Length == this.offsets[i].Length);
				for (int j = 0; j < offset.Length; j++)
				{
					TermVectorOffsetInfo termVectorOffsetInfo = offset[j];
					Assert.IsTrue(termVectorOffsetInfo.Equals(offsets[i][j]));
				}
			}
		}
		
		
		/// <summary> Make sure exceptions and bad params are handled appropriately</summary>
		[Test]
        public virtual void  TestBadParams()
		{
			try
			{
				TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
				Assert.IsTrue(reader != null);
				//Bad document number, good field number
				reader.Get(50, testFields[0]);
				Assert.Fail();
			}
			catch (System.IO.IOException e)
			{
				// expected exception
			}
			try
			{
				TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
				Assert.IsTrue(reader != null);
				//Bad document number, no field
				reader.Get(50);
				Assert.Fail();
			}
			catch (System.IO.IOException e)
			{
				// expected exception
			}
			try
			{
				TermVectorsReader reader = new TermVectorsReader(dir, seg, fieldInfos);
				Assert.IsTrue(reader != null);
				//good document number, bad field number
				TermFreqVector vector = reader.Get(0, "f50");
				Assert.IsTrue(vector == null);
			}
			catch (System.IO.IOException e)
			{
				Assert.Fail();
			}
		}
	}
}