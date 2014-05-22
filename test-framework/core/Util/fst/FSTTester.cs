using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Util.Fst
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 * <p/>
	 * http://www.apache.org/licenses/LICENSE-2.0
	 * <p/>
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */


	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static org.junit.Assert.Assert.AreEqual;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static org.junit.Assert.Assert.IsFalse;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static org.junit.Assert.Assert.IsNotNull;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static org.junit.Assert.assertNull;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static org.junit.Assert.Assert.IsTrue;

	/// <summary>
	/// Helper class to test FSTs. </summary>
	public class FSTTester<T>
	{

	  internal readonly Random Random;
	  internal readonly IList<InputOutput<T>> Pairs;
	  internal readonly int InputMode;
	  internal readonly Outputs<T> Outputs;
	  internal readonly Directory Dir;
	  internal readonly bool DoReverseLookup;

	  public FSTTester(Random random, Directory dir, int inputMode, IList<InputOutput<T>> pairs, Outputs<T> outputs, bool doReverseLookup)
	  {
		this.Random = random;
		this.Dir = dir;
		this.InputMode = inputMode;
		this.Pairs = pairs;
		this.Outputs = outputs;
		this.DoReverseLookup = doReverseLookup;
	  }

	  internal static string InputToString(int inputMode, IntsRef term)
	  {
		return InputToString(inputMode, term, true);
	  }

	  internal static string InputToString(int inputMode, IntsRef term, bool isValidUnicode)
	  {
		if (!isValidUnicode)
		{
		  return term.ToString();
		}
		else if (inputMode == 0)
		{
		  // utf8
		  return ToBytesRef(term).utf8ToString() + " " + term;
		}
		else
		{
		  // utf32
		  return UnicodeUtil.newString(term.ints, term.offset, term.length) + " " + term;
		}
	  }

	  private static BytesRef ToBytesRef(IntsRef ir)
	  {
		BytesRef br = new BytesRef(ir.length);
		for (int i = 0;i < ir.length;i++)
		{
		  int x = ir.ints[ir.offset + i];
		  Debug.Assert(x >= 0 && x <= 255);
		  br.bytes[i] = (sbyte) x;
		}
		br.length = ir.length;
		return br;
	  }

	  internal static string GetRandomString(Random random)
	  {
		string term;
		if (random.nextBoolean())
		{
		  term = TestUtil.RandomRealisticUnicodeString(random);
		}
		else
		{
		  // we want to mix in limited-alphabet symbols so
		  // we get more sharing of the nodes given how few
		  // terms we are testing...
		  term = SimpleRandomString(random);
		}
		return term;
	  }

	  internal static string SimpleRandomString(Random r)
	  {
		int end = r.Next(10);
		if (end == 0)
		{
		  // allow 0 length
		  return "";
		}
		char[] buffer = new char[end];
		for (int i = 0; i < end; i++)
		{
		  buffer[i] = (char) TestUtil.NextInt(r, 97, 102);
		}
		return new string(buffer, 0, end);
	  }

	  internal static IntsRef ToIntsRef(string s, int inputMode)
	  {
		return ToIntsRef(s, inputMode, new IntsRef(10));
	  }

	  internal static IntsRef ToIntsRef(string s, int inputMode, IntsRef ir)
	  {
		if (inputMode == 0)
		{
		  // utf8
		  return ToIntsRef(new BytesRef(s), ir);
		}
		else
		{
		  // utf32
		  return ToIntsRefUTF32(s, ir);
		}
	  }

	  internal static IntsRef ToIntsRefUTF32(string s, IntsRef ir)
	  {
		int charLength = s.Length;
		int charIdx = 0;
		int intIdx = 0;
		while (charIdx < charLength)
		{
		  if (intIdx == ir.ints.length)
		  {
			ir.grow(intIdx + 1);
		  }
		  int utf32 = s.codePointAt(charIdx);
		  ir.ints[intIdx] = utf32;
		  charIdx += char.charCount(utf32);
		  intIdx++;
		}
		ir.length = intIdx;
		return ir;
	  }

	  internal static IntsRef ToIntsRef(BytesRef br, IntsRef ir)
	  {
		if (br.length > ir.ints.length)
		{
		  ir.grow(br.length);
		}
		for (int i = 0;i < br.length;i++)
		{
		  ir.ints[i] = br.bytes[br.offset + i] & 0xFF;
		}
		ir.length = br.length;
		return ir;
	  }

	  /// <summary>
	  /// Holds one input/output pair. </summary>
	  public class InputOutput<T> : IComparable<InputOutput<T>>
	  {
		public readonly IntsRef Input;
		public readonly T Output;

		public InputOutput(IntsRef input, T output)
		{
		  this.Input = input;
		  this.Output = output;
		}

		public virtual int CompareTo(InputOutput<T> other)
		{
		  if (other is InputOutput)
		  {
			return Input.compareTo((other).Input);
		  }
		  else
		  {
			throw new System.ArgumentException();
		  }
		}
	  }

	  public virtual void DoTest(bool testPruning)
	  {
		// no pruning
		DoTest(0, 0, true);

		if (testPruning)
		{
		  // simple pruning
		  DoTest(TestUtil.NextInt(Random, 1, 1 + Pairs.Count), 0, true);

		  // leafy pruning
		  DoTest(0, TestUtil.NextInt(Random, 1, 1 + Pairs.Count), true);
		}
	  }

	  // runs the term, returning the output, or null if term
	  // isn't accepted.  if prefixLength is non-null it must be
	  // length 1 int array; prefixLength[0] is set to the length
	  // of the term prefix that matches
	  private T Run(FST<T> fst, IntsRef term, int[] prefixLength)
	  {
		Debug.Assert(prefixLength == null || prefixLength.Length == 1);
		FST.Arc<T> arc = fst.getFirstArc(new FST.Arc<T>());
		T NO_OUTPUT = fst.outputs.NoOutput;
		T output = NO_OUTPUT;
		FST.BytesReader fstReader = fst.BytesReader;

		for (int i = 0;i <= term.length;i++)
		{
		  int label;
		  if (i == term.length)
		  {
			label = FST.END_LABEL;
		  }
		  else
		  {
			label = term.ints[term.offset + i];
		  }
		  // System.out.println("   loop i=" + i + " label=" + label + " output=" + fst.outputs.outputToString(output) + " curArc: target=" + arc.target + " isFinal?=" + arc.isFinal());
		  if (fst.findTargetArc(label, arc, arc, fstReader) == null)
		  {
			// System.out.println("    not found");
			if (prefixLength != null)
			{
			  prefixLength[0] = i;
			  return output;
			}
			else
			{
			  return null;
			}
		  }
		  output = fst.outputs.add(output, arc.output);
		}

		if (prefixLength != null)
		{
		  prefixLength[0] = term.length;
		}

		return output;
	  }

	  private T RandomAcceptedWord(FST<T> fst, IntsRef @in)
	  {
		FST.Arc<T> arc = fst.getFirstArc(new FST.Arc<T>());

		IList<FST.Arc<T>> arcs = new List<FST.Arc<T>>();
		@in.length = 0;
		@in.offset = 0;
		T NO_OUTPUT = fst.outputs.NoOutput;
		T output = NO_OUTPUT;
		FST.BytesReader fstReader = fst.BytesReader;

		while (true)
		{
		  // read all arcs:
		  fst.readFirstTargetArc(arc, arc, fstReader);
		  arcs.Add((new FST.Arc<T>()).copyFrom(arc));
		  while (!arc.Last)
		  {
			fst.readNextArc(arc, fstReader);
			arcs.Add((new FST.Arc<T>()).copyFrom(arc));
		  }

		  // pick one
		  arc = arcs[Random.Next(arcs.Count)];
		  arcs.Clear();

		  // accumulate output
		  output = fst.outputs.add(output, arc.output);

		  // append label
		  if (arc.label == FST.END_LABEL)
		  {
			break;
		  }

		  if (@in.ints.length == @in.length)
		  {
			@in.grow(1 + @in.length);
		  }
		  @in.ints[@in.length++] = arc.label;
		}

		return output;
	  }


	  internal virtual FST<T> DoTest(int prune1, int prune2, bool allowRandomSuffixSharing)
	  {
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("\nTEST: prune1=" + prune1 + " prune2=" + prune2);
		}

		bool willRewrite = Random.nextBoolean();

		Builder<T> builder = new Builder<T>(InputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE.BYTE4, prune1, prune2, prune1 == 0 && prune2 == 0, allowRandomSuffixSharing ? Random.nextBoolean() : true, allowRandomSuffixSharing ? TestUtil.NextInt(Random, 1, 10) : int.MaxValue, Outputs, null, willRewrite, PackedInts.DEFAULT, true, 15);
		if (LuceneTestCase.VERBOSE)
		{
		  if (willRewrite)
		  {
			Console.WriteLine("TEST: packed FST");
		  }
		  else
		  {
			Console.WriteLine("TEST: non-packed FST");
		  }
		}

		foreach (InputOutput<T> pair in Pairs)
		{
		  if (pair.Output is IList)
		  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") java.util.List<Long> longValues = (java.util.List<Long>) pair.output;
			IList<long?> longValues = (IList<long?>) pair.Output;
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") final Builder<Object> builderObject = (Builder<Object>) builder;
			Builder<object> builderObject = (Builder<object>) builder;
			foreach (long? value in longValues)
			{
			  builderObject.add(pair.Input, value);
			}
		  }
		  else
		  {
			builder.add(pair.Input, pair.Output);
		  }
		}
		FST<T> fst = builder.finish();

		if (Random.nextBoolean() && fst != null && !willRewrite)
		{
		  IOContext context = LuceneTestCase.NewIOContext(Random);
		  IndexOutput @out = Dir.createOutput("fst.bin", context);
		  fst.save(@out);
		  @out.close();
		  IndexInput @in = Dir.openInput("fst.bin", context);
		  try
		  {
			fst = new FST<>(@in, Outputs);
		  }
		  finally
		  {
			@in.close();
			Dir.deleteFile("fst.bin");
		  }
		}

		if (LuceneTestCase.VERBOSE && Pairs.Count <= 20 && fst != null)
		{
		  Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"), StandardCharsets.UTF_8);
		  Util.toDot(fst, w, false, false);
		  w.close();
		  Console.WriteLine("SAVED out.dot");
		}

		if (LuceneTestCase.VERBOSE)
		{
		  if (fst == null)
		  {
			Console.WriteLine("  fst has 0 nodes (fully pruned)");
		  }
		  else
		  {
			Console.WriteLine("  fst has " + fst.NodeCount + " nodes and " + fst.ArcCount + " arcs");
		  }
		}

		if (prune1 == 0 && prune2 == 0)
		{
		  VerifyUnPruned(InputMode, fst);
		}
		else
		{
		  VerifyPruned(InputMode, fst, prune1, prune2);
		}

		return fst;
	  }

	  protected internal virtual bool OutputsEqual(T a, T b)
	  {
		return a.Equals(b);
	  }

	  // FST is complete
	  private void VerifyUnPruned(int inputMode, FST<T> fst)
	  {

		FST<long?> fstLong;
		Set<long?> validOutputs;
		long minLong = long.MaxValue;
		long maxLong = long.MinValue;

		if (DoReverseLookup)
		{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") FST<Long> fstLong0 = (FST<Long>) fst;
		  FST<long?> fstLong0 = (FST<long?>) fst;
		  fstLong = fstLong0;
		  validOutputs = new HashSet<>();
		  foreach (InputOutput<T> pair in Pairs)
		  {
			long? output = (long?) pair.Output;
			maxLong = Math.Max(maxLong, output);
			minLong = Math.Min(minLong, output);
			validOutputs.add(output);
		  }
		}
		else
		{
		  fstLong = null;
		  validOutputs = null;
		}

		if (Pairs.Count == 0)
		{
		  assertNull(fst);
		  return;
		}

		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: now verify " + Pairs.Count + " terms");
		  foreach (InputOutput<T> pair in Pairs)
		  {
			Assert.IsNotNull(pair);
			Assert.IsNotNull(pair.Input);
			Assert.IsNotNull(pair.Output);
			Console.WriteLine("  " + InputToString(inputMode, pair.Input) + ": " + Outputs.outputToString(pair.Output));
		  }
		}

		Assert.IsNotNull(fst);

		// visit valid pairs in order -- make sure all words
		// are accepted, and FSTEnum's next() steps through
		// them correctly
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: check valid terms/next()");
		}
		{
		  IntsRefFSTEnum<T> fstEnum = new IntsRefFSTEnum<T>(fst);
		  foreach (InputOutput<T> pair in Pairs)
		  {
			IntsRef term = pair.Input;
			if (LuceneTestCase.VERBOSE)
			{
			  Console.WriteLine("TEST: check term=" + InputToString(inputMode, term) + " output=" + fst.outputs.outputToString(pair.Output));
			}
			T output = Run(fst, term, null);
			Assert.IsNotNull("term " + InputToString(inputMode, term) + " is not accepted", output);
			Assert.IsTrue(OutputsEqual(pair.Output, output));

			// verify enum's next
			IntsRefFSTEnum.InputOutput<T> t = fstEnum.next();
			Assert.IsNotNull(t);
			Assert.AreEqual("expected input=" + InputToString(inputMode, term) + " but fstEnum returned " + InputToString(inputMode, t.input), term, t.input);
			Assert.IsTrue(OutputsEqual(pair.Output, t.output));
		  }
		  assertNull(fstEnum.next());
		}

		IDictionary<IntsRef, T> termsMap = new Dictionary<IntsRef, T>();
		foreach (InputOutput<T> pair in Pairs)
		{
		  termsMap[pair.Input] = pair.Output;
		}

		if (DoReverseLookup && maxLong > minLong)
		{
		  // Do random lookups so we test null (output doesn't
		  // exist) case:
		  assertNull(Util.getByOutput(fstLong, minLong - 7));
		  assertNull(Util.getByOutput(fstLong, maxLong + 7));

		  int num = LuceneTestCase.AtLeast(Random, 100);
		  for (int iter = 0;iter < num;iter++)
		  {
			long? v = TestUtil.NextLong(Random, minLong, maxLong);
			IntsRef input = Util.getByOutput(fstLong, v);
			Assert.IsTrue(validOutputs.contains(v) || input == null);
		  }
		}

		// find random matching word and make sure it's valid
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: verify random accepted terms");
		}
		IntsRef scratch = new IntsRef(10);
		int num = LuceneTestCase.AtLeast(Random, 500);
		for (int iter = 0;iter < num;iter++)
		{
		  T output = RandomAcceptedWord(fst, scratch);
		  Assert.IsTrue("accepted word " + InputToString(inputMode, scratch) + " is not valid", termsMap.ContainsKey(scratch));
		  Assert.IsTrue(OutputsEqual(termsMap[scratch], output));

		  if (DoReverseLookup)
		  {
			//System.out.println("lookup output=" + output + " outs=" + fst.outputs);
			IntsRef input = Util.getByOutput(fstLong, (long?) output);
			Assert.IsNotNull(input);
			//System.out.println("  got " + Util.toBytesRef(input, new BytesRef()).utf8ToString());
			Assert.AreEqual(scratch, input);
		  }
		}

		// test IntsRefFSTEnum.seek:
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: verify seek");
		}
		IntsRefFSTEnum<T> fstEnum = new IntsRefFSTEnum<T>(fst);
		num = LuceneTestCase.AtLeast(Random, 100);
		for (int iter = 0;iter < num;iter++)
		{
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("  iter=" + iter);
		  }
		  if (Random.nextBoolean())
		  {
			// seek to term that doesn't exist:
			while (true)
			{
			  IntsRef term = ToIntsRef(GetRandomString(Random), inputMode);
			  int pos = Collections.binarySearch(Pairs, new InputOutput<T>(term, null));
			  if (pos < 0)
			  {
				pos = -(pos + 1);
				// ok doesn't exist
				//System.out.println("  seek " + inputToString(inputMode, term));
				IntsRefFSTEnum.InputOutput<T> seekResult;
				if (Random.Next(3) == 0)
				{
				  if (LuceneTestCase.VERBOSE)
				  {
					Console.WriteLine("  do non-exist seekExact term=" + InputToString(inputMode, term));
				  }
				  seekResult = fstEnum.seekExact(term);
				  pos = -1;
				}
				else if (Random.nextBoolean())
				{
				  if (LuceneTestCase.VERBOSE)
				  {
					Console.WriteLine("  do non-exist seekFloor term=" + InputToString(inputMode, term));
				  }
				  seekResult = fstEnum.seekFloor(term);
				  pos--;
				}
				else
				{
				  if (LuceneTestCase.VERBOSE)
				  {
					Console.WriteLine("  do non-exist seekCeil term=" + InputToString(inputMode, term));
				  }
				  seekResult = fstEnum.seekCeil(term);
				}

				if (pos != -1 && pos < Pairs.Count)
				{
				  //System.out.println("    got " + inputToString(inputMode,seekResult.input) + " output=" + fst.outputs.outputToString(seekResult.output));
				  Assert.IsNotNull("got null but expected term=" + InputToString(inputMode, Pairs[pos].Input), seekResult);
				  if (LuceneTestCase.VERBOSE)
				  {
					Console.WriteLine("    got " + InputToString(inputMode, seekResult.input));
				  }
				  Assert.AreEqual("expected " + InputToString(inputMode, Pairs[pos].Input) + " but got " + InputToString(inputMode, seekResult.input), Pairs[pos].Input, seekResult.input);
				  Assert.IsTrue(OutputsEqual(Pairs[pos].Output, seekResult.output));
				}
				else
				{
				  // seeked before start or beyond end
				  //System.out.println("seek=" + seekTerm);
				  assertNull("expected null but got " + (seekResult == null ? "null" : InputToString(inputMode, seekResult.input)), seekResult);
				  if (LuceneTestCase.VERBOSE)
				  {
					Console.WriteLine("    got null");
				  }
				}

				break;
			  }
			}
		  }
		  else
		  {
			// seek to term that does exist:
			InputOutput<T> pair = Pairs[Random.Next(Pairs.Count)];
			IntsRefFSTEnum.InputOutput<T> seekResult;
			if (Random.Next(3) == 2)
			{
			  if (LuceneTestCase.VERBOSE)
			  {
				Console.WriteLine("  do exists seekExact term=" + InputToString(inputMode, pair.Input));
			  }
			  seekResult = fstEnum.seekExact(pair.Input);
			}
			else if (Random.nextBoolean())
			{
			  if (LuceneTestCase.VERBOSE)
			  {
				Console.WriteLine("  do exists seekFloor " + InputToString(inputMode, pair.Input));
			  }
			  seekResult = fstEnum.seekFloor(pair.Input);
			}
			else
			{
			  if (LuceneTestCase.VERBOSE)
			  {
				Console.WriteLine("  do exists seekCeil " + InputToString(inputMode, pair.Input));
			  }
			  seekResult = fstEnum.seekCeil(pair.Input);
			}
			Assert.IsNotNull(seekResult);
			Assert.AreEqual("got " + InputToString(inputMode, seekResult.input) + " but expected " + InputToString(inputMode, pair.Input), pair.Input, seekResult.input);
			Assert.IsTrue(OutputsEqual(pair.Output, seekResult.output));
		  }
		}

		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: mixed next/seek");
		}

		// test mixed next/seek
		num = LuceneTestCase.AtLeast(Random, 100);
		for (int iter = 0;iter < num;iter++)
		{
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("TEST: iter " + iter);
		  }
		  // reset:
		  fstEnum = new IntsRefFSTEnum<>(fst);
		  int upto = -1;
		  while (true)
		  {
			bool isDone = false;
			if (upto == Pairs.Count - 1 || Random.nextBoolean())
			{
			  // next
			  upto++;
			  if (LuceneTestCase.VERBOSE)
			  {
				Console.WriteLine("  do next");
			  }
			  isDone = fstEnum.next() == null;
			}
			else if (upto != -1 && upto < 0.75 * Pairs.Count && Random.nextBoolean())
			{
			  int attempt = 0;
			  for (;attempt < 10;attempt++)
			  {
				IntsRef term = ToIntsRef(GetRandomString(Random), inputMode);
				if (!termsMap.ContainsKey(term) && term.compareTo(Pairs[upto].Input) > 0)
				{
				  int pos = Collections.binarySearch(Pairs, new InputOutput<T>(term, null));
				  Debug.Assert(pos < 0);
				  upto = -(pos + 1);

				  if (Random.nextBoolean())
				  {
					upto--;
					Assert.IsTrue(upto != -1);
					if (LuceneTestCase.VERBOSE)
					{
					  Console.WriteLine("  do non-exist seekFloor(" + InputToString(inputMode, term) + ")");
					}
					isDone = fstEnum.seekFloor(term) == null;
				  }
				  else
				  {
					if (LuceneTestCase.VERBOSE)
					{
					  Console.WriteLine("  do non-exist seekCeil(" + InputToString(inputMode, term) + ")");
					}
					isDone = fstEnum.seekCeil(term) == null;
				  }

				  break;
				}
			  }
			  if (attempt == 10)
			  {
				continue;
			  }

			}
			else
			{
			  int inc = Random.Next(Pairs.Count - upto - 1);
			  upto += inc;
			  if (upto == -1)
			  {
				upto = 0;
			  }

			  if (Random.nextBoolean())
			  {
				if (LuceneTestCase.VERBOSE)
				{
				  Console.WriteLine("  do seekCeil(" + InputToString(inputMode, Pairs[upto].Input) + ")");
				}
				isDone = fstEnum.seekCeil(Pairs[upto].Input) == null;
			  }
			  else
			  {
				if (LuceneTestCase.VERBOSE)
				{
				  Console.WriteLine("  do seekFloor(" + InputToString(inputMode, Pairs[upto].Input) + ")");
				}
				isDone = fstEnum.seekFloor(Pairs[upto].Input) == null;
			  }
			}
			if (LuceneTestCase.VERBOSE)
			{
			  if (!isDone)
			  {
				Console.WriteLine("    got " + InputToString(inputMode, fstEnum.current().input));
			  }
			  else
			  {
				Console.WriteLine("    got null");
			  }
			}

			if (upto == Pairs.Count)
			{
			  Assert.IsTrue(isDone);
			  break;
			}
			else
			{
			  Assert.IsFalse(isDone);
			  Assert.AreEqual(Pairs[upto].Input, fstEnum.current().input);
			  Assert.IsTrue(OutputsEqual(Pairs[upto].Output, fstEnum.current().output));

			  /*
			    if (upto < pairs.size()-1) {
			    int tryCount = 0;
			    while(tryCount < 10) {
			    final IntsRef t = toIntsRef(getRandomString(), inputMode);
			    if (pairs.get(upto).input.compareTo(t) < 0) {
			    final boolean expected = t.compareTo(pairs.get(upto+1).input) < 0;
			    if (LuceneTestCase.VERBOSE) {
			    System.out.println("TEST: call beforeNext(" + inputToString(inputMode, t) + "); current=" + inputToString(inputMode, pairs.get(upto).input) + " next=" + inputToString(inputMode, pairs.get(upto+1).input) + " expected=" + expected);
			    }
			    Assert.AreEqual(expected, fstEnum.beforeNext(t));
			    break;
			    }
			    tryCount++;
			    }
			    }
			  */
			}
		  }
		}
	  }

	  private class CountMinOutput<T>
	  {
		internal int Count;
		internal T Output;
		internal T FinalOutput;
		internal bool IsLeaf = true;
		internal bool IsFinal;
	  }

	  // FST is pruned
	  private void VerifyPruned(int inputMode, FST<T> fst, int prune1, int prune2)
	  {

		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: now verify pruned " + Pairs.Count + " terms; outputs=" + Outputs);
		  foreach (InputOutput<T> pair in Pairs)
		  {
			Console.WriteLine("  " + InputToString(inputMode, pair.Input) + ": " + Outputs.outputToString(pair.Output));
		  }
		}

		// To validate the FST, we brute-force compute all prefixes
		// in the terms, matched to their "common" outputs, prune that
		// set according to the prune thresholds, then assert the FST
		// matches that same set.

		// NOTE: Crazy RAM intensive!!

		//System.out.println("TEST: tally prefixes");

		// build all prefixes
		IDictionary<IntsRef, CountMinOutput<T>> prefixes = new Dictionary<IntsRef, CountMinOutput<T>>();
		IntsRef scratch = new IntsRef(10);
		foreach (InputOutput<T> pair in Pairs)
		{
		  scratch.copyInts(pair.Input);
		  for (int idx = 0;idx <= pair.Input.length;idx++)
		  {
			scratch.length = idx;
			CountMinOutput<T> cmo = prefixes[scratch];
			if (cmo == null)
			{
			  cmo = new CountMinOutput<>();
			  cmo.Count = 1;
			  cmo.Output = pair.Output;
			  prefixes[IntsRef.deepCopyOf(scratch)] = cmo;
			}
			else
			{
			  cmo.Count++;
			  T output1 = cmo.Output;
			  if (output1.Equals(Outputs.NoOutput))
			  {
				output1 = Outputs.NoOutput;
			  }
			  T output2 = pair.Output;
			  if (output2.Equals(Outputs.NoOutput))
			  {
				output2 = Outputs.NoOutput;
			  }
			  cmo.Output = Outputs.common(output1, output2);
			}
			if (idx == pair.Input.length)
			{
			  cmo.IsFinal = true;
			  cmo.FinalOutput = cmo.Output;
			}
		  }
		}

		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: now prune");
		}

		// prune 'em
		IEnumerator<KeyValuePair<IntsRef, CountMinOutput<T>>> it = prefixes.GetEnumerator();
		while (it.MoveNext())
		{
		  KeyValuePair<IntsRef, CountMinOutput<T>> ent = it.Current;
		  IntsRef prefix = ent.Key;
		  CountMinOutput<T> cmo = ent.Value;
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("  term prefix=" + InputToString(inputMode, prefix, false) + " count=" + cmo.Count + " isLeaf=" + cmo.IsLeaf + " output=" + Outputs.outputToString(cmo.Output) + " isFinal=" + cmo.IsFinal);
		  }
		  bool keep;
		  if (prune1 > 0)
		  {
			keep = cmo.Count >= prune1;
		  }
		  else
		  {
			Debug.Assert(prune2 > 0);
			if (prune2 > 1 && cmo.Count >= prune2)
			{
			  keep = true;
			}
			else if (prefix.length > 0)
			{
			  // consult our parent
			  scratch.length = prefix.length - 1;
			  Array.Copy(prefix.ints, prefix.offset, scratch.ints, 0, scratch.length);
			  CountMinOutput<T> cmo2 = prefixes[scratch];
			  //System.out.println("    parent count = " + (cmo2 == null ? -1 : cmo2.count));
			  keep = cmo2 != null && ((prune2 > 1 && cmo2.Count >= prune2) || (prune2 == 1 && (cmo2.Count >= 2 || prefix.length <= 1)));
			}
			else if (cmo.Count >= prune2)
			{
			  keep = true;
			}
			else
			{
			  keep = false;
			}
		  }

		  if (!keep)
		  {
			it.remove();
			//System.out.println("    remove");
		  }
		  else
		  {
			// clear isLeaf for all ancestors
			//System.out.println("    keep");
			scratch.copyInts(prefix);
			scratch.length--;
			while (scratch.length >= 0)
			{
			  CountMinOutput<T> cmo2 = prefixes[scratch];
			  if (cmo2 != null)
			  {
				//System.out.println("    clear isLeaf " + inputToString(inputMode, scratch));
				cmo2.IsLeaf = false;
			  }
			  scratch.length--;
			}
		  }
		}

		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: after prune");
		  foreach (KeyValuePair<IntsRef, CountMinOutput<T>> ent in prefixes)
		  {
			Console.WriteLine("  " + InputToString(inputMode, ent.Key, false) + ": isLeaf=" + ent.Value.isLeaf + " isFinal=" + ent.Value.isFinal);
			if (ent.Value.isFinal)
			{
			  Console.WriteLine("    finalOutput=" + Outputs.outputToString(ent.Value.finalOutput));
			}
		  }
		}

		if (prefixes.Count <= 1)
		{
		  assertNull(fst);
		  return;
		}

		Assert.IsNotNull(fst);

		// make sure FST only enums valid prefixes
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: check pruned enum");
		}
		IntsRefFSTEnum<T> fstEnum = new IntsRefFSTEnum<T>(fst);
		IntsRefFSTEnum.InputOutput<T> current;
		while ((current = fstEnum.next()) != null)
		{
		  if (LuceneTestCase.VERBOSE)
		  {
			Console.WriteLine("  fstEnum.next prefix=" + InputToString(inputMode, current.input, false) + " output=" + Outputs.outputToString(current.output));
		  }
		  CountMinOutput<T> cmo = prefixes[current.input];
		  Assert.IsNotNull(cmo);
		  Assert.IsTrue(cmo.IsLeaf || cmo.IsFinal);
		  //if (cmo.isFinal && !cmo.isLeaf) {
		  if (cmo.IsFinal)
		  {
			Assert.AreEqual(cmo.FinalOutput, current.output);
		  }
		  else
		  {
			Assert.AreEqual(cmo.Output, current.output);
		  }
		}

		// make sure all non-pruned prefixes are present in the FST
		if (LuceneTestCase.VERBOSE)
		{
		  Console.WriteLine("TEST: verify all prefixes");
		}
		int[] stopNode = new int[1];
		foreach (KeyValuePair<IntsRef, CountMinOutput<T>> ent in prefixes)
		{
		  if (ent.Key.length > 0)
		  {
			CountMinOutput<T> cmo = ent.Value;
			T output = Run(fst, ent.Key, stopNode);
			if (LuceneTestCase.VERBOSE)
			{
			  Console.WriteLine("TEST: verify prefix=" + InputToString(inputMode, ent.Key, false) + " output=" + Outputs.outputToString(cmo.Output));
			}
			// if (cmo.isFinal && !cmo.isLeaf) {
			if (cmo.IsFinal)
			{
			  Assert.AreEqual(cmo.FinalOutput, output);
			}
			else
			{
			  Assert.AreEqual(cmo.Output, output);
			}
			Assert.AreEqual(ent.Key.length, stopNode[0]);
		  }
		}
	  }
	}

}