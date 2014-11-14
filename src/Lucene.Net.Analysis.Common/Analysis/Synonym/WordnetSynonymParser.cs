using System;

namespace org.apache.lucene.analysis.synonym
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


	using CharsRef = org.apache.lucene.util.CharsRef;

	/// <summary>
	/// Parser for wordnet prolog format
	/// <para>
	/// See http://wordnet.princeton.edu/man/prologdb.5WN.html for a description of the format.
	/// @lucene.experimental
	/// </para>
	/// </summary>
	// TODO: allow you to specify syntactic categories (e.g. just nouns, etc)
	public class WordnetSynonymParser : SynonymMap.Parser
	{
	  private readonly bool expand;

	  public WordnetSynonymParser(bool dedup, bool expand, Analyzer analyzer) : base(dedup, analyzer)
	  {
		this.expand = expand;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void parse(java.io.Reader in) throws java.io.IOException, java.text.ParseException
	  public override void parse(Reader @in)
	  {
		LineNumberReader br = new LineNumberReader(@in);
		try
		{
		  string line = null;
		  string lastSynSetID = "";
		  CharsRef[] synset = new CharsRef[8];
		  int synsetSize = 0;

		  while ((line = br.readLine()) != null)
		  {
			string synSetID = line.Substring(2, 9);

			if (!synSetID.Equals(lastSynSetID))
			{
			  addInternal(synset, synsetSize);
			  synsetSize = 0;
			}

			if (synset.Length <= synsetSize+1)
			{
			  CharsRef[] larger = new CharsRef[synset.Length * 2];
			  Array.Copy(synset, 0, larger, 0, synsetSize);
			  synset = larger;
			}

			synset[synsetSize] = parseSynonym(line, synset[synsetSize]);
			synsetSize++;
			lastSynSetID = synSetID;
		  }

		  // final synset in the file
		  addInternal(synset, synsetSize);
		}
		catch (System.ArgumentException e)
		{
		  ParseException ex = new ParseException("Invalid synonym rule at line " + br.LineNumber, 0);
		  ex.initCause(e);
		  throw ex;
		}
		finally
		{
		  br.close();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.util.CharsRef parseSynonym(String line, org.apache.lucene.util.CharsRef reuse) throws java.io.IOException
	  private CharsRef parseSynonym(string line, CharsRef reuse)
	  {
		if (reuse == null)
		{
		  reuse = new CharsRef(8);
		}

		int start = line.IndexOf('\'') + 1;
		int end = line.LastIndexOf('\'');

		string text = line.Substring(start, end - start).Replace("''", "'");
		return analyze(text, reuse);
	  }

	  private void addInternal(CharsRef[] synset, int size)
	  {
		if (size <= 1)
		{
		  return; // nothing to do
		}

		if (expand)
		{
		  for (int i = 0; i < size; i++)
		  {
			for (int j = 0; j < size; j++)
			{
			  add(synset[i], synset[j], false);
			}
		  }
		}
		else
		{
		  for (int i = 0; i < size; i++)
		  {
			add(synset[i], synset[0], false);
		  }
		}
	  }
	}

}