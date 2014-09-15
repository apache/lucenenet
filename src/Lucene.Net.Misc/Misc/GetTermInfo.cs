using System;

namespace org.apache.lucene.misc
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

	using Directory = org.apache.lucene.store.Directory;
	using FSDirectory = org.apache.lucene.store.FSDirectory;
	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using Term = org.apache.lucene.index.Term;

	/// <summary>
	/// Utility to get document frequency and total number of occurrences (sum of the tf for each doc)  of a term. 
	/// </summary>
	public class GetTermInfo
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void main(String[] args) throws Exception
	  public static void Main(string[] args)
	  {

		FSDirectory dir = null;
		string inputStr = null;
		string field = null;

		if (args.Length == 3)
		{
		  dir = FSDirectory.open(new File(args[0]));
		  field = args[1];
		  inputStr = args[2];
		}
		else
		{
		  usage();
		  Environment.Exit(1);
		}

		getTermInfo(dir,new Term(field, inputStr));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void getTermInfo(org.apache.lucene.store.Directory dir, org.apache.lucene.index.Term term) throws Exception
	  public static void getTermInfo(Directory dir, Term term)
	  {
		IndexReader reader = DirectoryReader.open(dir);
		System.out.printf(Locale.ROOT, "%s:%s \t totalTF = %,d \t doc freq = %,d \n", term.field(), term.text(), reader.totalTermFreq(term), reader.docFreq(term));
	  }

	  private static void usage()
	  {
//JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
		Console.WriteLine("\n\nusage:\n\t" + "java " + typeof(GetTermInfo).FullName + " <index dir> field term \n\n");
	  }
	}

}