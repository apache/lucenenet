namespace Lucene.Net.Codecs.Lucene41
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat.BLOCK_SIZE;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene41.ForUtil.MAX_DATA_SIZE;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene41.ForUtil.MAX_ENCODED_SIZE;


	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

	using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;

	public class TestForUtil : LuceneTestCase
	{

	  public virtual void TestEncodeDecode()
	  {
		int iterations = RandomInts.randomIntBetween(random(), 1, 1000);
		float acceptableOverheadRatio = random().nextFloat();
		int[] values = new int[(iterations - 1) * BLOCK_SIZE + ForUtil.MAX_DATA_SIZE];
		for (int i = 0; i < iterations; ++i)
		{
		  int bpv = random().Next(32);
		  if (bpv == 0)
		  {
			int value = RandomInts.randomIntBetween(random(), 0, int.MaxValue);
			for (int j = 0; j < BLOCK_SIZE; ++j)
			{
			  values[i * BLOCK_SIZE + j] = value;
			}
		  }
		  else
		  {
			for (int j = 0; j < BLOCK_SIZE; ++j)
			{
			  values[i * BLOCK_SIZE + j] = RandomInts.randomIntBetween(random(), 0, (int) PackedInts.maxValue(bpv));
			}
		  }
		}

		Directory d = new RAMDirectory();
		long endPointer;

		{
		  // encode
		  IndexOutput @out = d.createOutput("test.bin", IOContext.DEFAULT);
		  ForUtil forUtil = new ForUtil(acceptableOverheadRatio, @out);

		  for (int i = 0; i < iterations; ++i)
		  {
			forUtil.writeBlock(Arrays.copyOfRange(values, i * BLOCK_SIZE, values.Length), new sbyte[MAX_ENCODED_SIZE], @out);
		  }
		  endPointer = @out.FilePointer;
		  @out.close();
		}

		{
		  // decode
		  IndexInput @in = d.openInput("test.bin", IOContext.READONCE);
		  ForUtil forUtil = new ForUtil(@in);
		  for (int i = 0; i < iterations; ++i)
		  {
			if (random().nextBoolean())
			{
			  forUtil.skipBlock(@in);
			  continue;
			}
			int[] restored = new int[MAX_DATA_SIZE];
			forUtil.readBlock(@in, new sbyte[MAX_ENCODED_SIZE], restored);
			assertArrayEquals(Arrays.copyOfRange(values, i * BLOCK_SIZE, (i + 1) * BLOCK_SIZE), Arrays.copyOf(restored, BLOCK_SIZE));
		  }
		  Assert.AreEqual(endPointer, @in.FilePointer);
		  @in.close();
		}
	  }

	}

}