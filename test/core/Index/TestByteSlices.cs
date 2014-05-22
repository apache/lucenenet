using System;

namespace Lucene.Net.Index
{

	/// <summary>
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using RecyclingByteBlockAllocator = Lucene.Net.Util.RecyclingByteBlockAllocator;

	public class TestByteSlices : LuceneTestCase
	{

	  public virtual void TestBasic()
	  {
		ByteBlockPool pool = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, random().Next(100)));

		int NUM_STREAM = atLeast(100);

		ByteSliceWriter writer = new ByteSliceWriter(pool);

		int[] starts = new int[NUM_STREAM];
		int[] uptos = new int[NUM_STREAM];
		int[] counters = new int[NUM_STREAM];

		ByteSliceReader reader = new ByteSliceReader();

		for (int ti = 0;ti < 100;ti++)
		{

		  for (int stream = 0;stream < NUM_STREAM;stream++)
		  {
			starts[stream] = -1;
			counters[stream] = 0;
		  }

		  int num = atLeast(3000);
		  for (int iter = 0; iter < num; iter++)
		  {
			int stream;
			if (random().nextBoolean())
			{
			  stream = random().Next(3);
			}
			else
			{
			  stream = random().Next(NUM_STREAM);
			}

			if (VERBOSE)
			{
			  Console.WriteLine("write stream=" + stream);
			}

			if (starts[stream] == -1)
			{
			  int spot = pool.newSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
			  starts[stream] = uptos[stream] = spot + pool.byteOffset;
			  if (VERBOSE)
			  {
				Console.WriteLine("  init to " + starts[stream]);
			  }
			}

			writer.init(uptos[stream]);
			int numValue;
			if (random().Next(10) == 3)
			{
			  numValue = random().Next(100);
			}
			else if (random().Next(5) == 3)
			{
			  numValue = random().Next(3);
			}
			else
			{
			  numValue = random().Next(20);
			}

			for (int j = 0;j < numValue;j++)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("    write " + (counters[stream] + j));
			  }
			  // write some large (incl. negative) ints:
			  writer.writeVInt(random().Next());
			  writer.writeVInt(counters[stream] + j);
			}
			counters[stream] += numValue;
			uptos[stream] = writer.Address;
			if (VERBOSE)
			{
			  Console.WriteLine("    addr now " + uptos[stream]);
			}
		  }

		  for (int stream = 0;stream < NUM_STREAM;stream++)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  stream=" + stream + " count=" + counters[stream]);
			}

			if (starts[stream] != -1 && starts[stream] != uptos[stream])
			{
			  reader.init(pool, starts[stream], uptos[stream]);
			  for (int j = 0;j < counters[stream];j++)
			  {
				reader.readVInt();
				Assert.AreEqual(j, reader.readVInt());
			  }
			}
		  }

		  pool.reset();
		}
	  }
	}

}