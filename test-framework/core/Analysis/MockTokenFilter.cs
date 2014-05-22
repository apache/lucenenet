using Lucene.Net.Analysis.Tokenattributes;
namespace Lucene.Net.Analysis
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
	import static Lucene.Net.Util.Automaton.BasicAutomata.makeEmpty;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Util.Automaton.BasicAutomata.makeString;


	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
	using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;

	/// <summary>
	/// A tokenfilter for testing that removes terms accepted by a DFA.
	/// <ul>
	///  <li>Union a list of singletons to act like a stopfilter.
	///  <li>Use the complement to act like a keepwordfilter
	///  <li>Use a regex like <code>.{12,}</code> to act like a lengthfilter
	/// </ul>
	/// </summary>
	public sealed class MockTokenFilter : TokenFilter
	{
	  /// <summary>
	  /// Empty set of stopwords </summary>
	  public static readonly CharacterRunAutomaton EMPTY_STOPSET = new CharacterRunAutomaton(makeEmpty());

	  /// <summary>
	  /// Set of common english stopwords </summary>
	  public static readonly CharacterRunAutomaton ENGLISH_STOPSET = new CharacterRunAutomaton(BasicOperations.union(Arrays.asList(makeString("a"), makeString("an"), makeString("and"), makeString("are"), makeString("as"), makeString("at"), makeString("be"), makeString("but"), makeString("by"), makeString("for"), makeString("if"), makeString("in"), makeString("into"), makeString("is"), makeString("it"), makeString("no"), makeString("not"), makeString("of"), makeString("on"), makeString("or"), makeString("such"), makeString("that"), makeString("the"), makeString("their"), makeString("then"), makeString("there"), makeString("these"), makeString("they"), makeString("this"), makeString("to"), makeString("was"), makeString("will"), makeString("with"))));

	  private readonly CharacterRunAutomaton Filter;

	  private readonly CharTermAttribute TermAtt = AddAttribute<CharTermAttribute>();
	  private readonly PositionIncrementAttribute PosIncrAtt = AddAttribute<PositionIncrementAttribute>();
	  private int SkippedPositions;

	  /// <summary>
	  /// Create a new MockTokenFilter.
	  /// </summary>
	  /// <param name="input"> TokenStream to filter </param>
	  /// <param name="filter"> DFA representing the terms that should be removed. </param>
	  public MockTokenFilter(TokenStream input, CharacterRunAutomaton filter) : base(input)
	  {
		this.Filter = filter;
	  }

	  public override bool IncrementToken()
	  {
		// TODO: fix me when posInc=false, to work like FilteringTokenFilter in that case and not return
		// initial token with posInc=0 ever

		// return the first non-stop word found
		SkippedPositions = 0;
		while (Input.IncrementToken())
		{
		  if (!Filter.run(TermAtt.Buffer(), 0, TermAtt.Length))
		  {
			PosIncrAtt.PositionIncrement = PosIncrAtt.PositionIncrement + SkippedPositions;
			return true;
		  }
		  SkippedPositions += PosIncrAtt.PositionIncrement;
		}
		// reached EOS -- return false
		return false;
	  }

	  public override void End()
	  {
		base.End();
		PosIncrAtt.PositionIncrement = PosIncrAtt.PositionIncrement + SkippedPositions;
	  }

	  public override void Reset()
	  {
		base.Reset();
		SkippedPositions = 0;
	  }
	}

}