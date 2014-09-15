namespace org.apache.lucene.queries.function
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

	using IndexReader = org.apache.lucene.index.IndexReader;
	using MultiFields = org.apache.lucene.index.MultiFields;
	using Scorer = org.apache.lucene.search.Scorer;
	using Bits = org.apache.lucene.util.Bits;

	/// <summary>
	/// <seealso cref="Scorer"/> which returns the result of <seealso cref="FunctionValues#floatVal(int)"/> as
	/// the score for a document.
	/// </summary>
	public class ValueSourceScorer : Scorer
	{
	  protected internal readonly IndexReader reader;
	  private int doc = -1;
	  protected internal readonly int maxDoc;
	  protected internal readonly FunctionValues values;
	  protected internal bool checkDeletes;
	  private readonly Bits liveDocs;

	  protected internal ValueSourceScorer(IndexReader reader, FunctionValues values) : base(null)
	  {
		this.reader = reader;
		this.maxDoc = reader.maxDoc();
		this.values = values;
		CheckDeletes = true;
		this.liveDocs = MultiFields.getLiveDocs(reader);
	  }

	  public virtual IndexReader Reader
	  {
		  get
		  {
			return reader;
		  }
	  }

	  public virtual bool CheckDeletes
	  {
		  set
		  {
			this.checkDeletes = value && reader.hasDeletions();
		  }
	  }

	  public virtual bool matches(int doc)
	  {
		return (!checkDeletes || liveDocs.get(doc)) && matchesValue(doc);
	  }

	  public virtual bool matchesValue(int doc)
	  {
		return true;
	  }

	  public override int docID()
	  {
		return doc;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int nextDoc() throws java.io.IOException
	  public override int nextDoc()
	  {
		for (; ;)
		{
		  doc++;
		  if (doc >= maxDoc)
		  {
			  return doc = NO_MORE_DOCS;
		  }
		  if (matches(doc))
		  {
			  return doc;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
	  public override int advance(int target)
	  {
		// also works fine when target==NO_MORE_DOCS
		doc = target - 1;
		return nextDoc();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public float score() throws java.io.IOException
	  public override float score()
	  {
		return values.floatVal(doc);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
	  public override int freq()
	  {
		return 1;
	  }

	  public override long cost()
	  {
		return maxDoc;
	  }
	}

}