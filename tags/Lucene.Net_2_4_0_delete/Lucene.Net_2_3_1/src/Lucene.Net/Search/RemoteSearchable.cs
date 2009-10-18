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
using FieldSelector = Lucene.Net.Documents.FieldSelector;
using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
using Term = Lucene.Net.Index.Term;

namespace Lucene.Net.Search
{
	
	/// <summary> A remote searchable implementation.
	/// 
	/// </summary>
	/// <version>  $Id: RemoteSearchable.java 472959 2006-11-09 16:21:50Z yonik $
	/// </version>
	[Serializable]
	public class RemoteSearchable : System.MarshalByRefObject, Lucene.Net.Search.Searchable
	{
		
		private Lucene.Net.Search.Searchable local;
		
		/// <summary>Constructs and exports a remote searcher. </summary>
		public RemoteSearchable(Lucene.Net.Search.Searchable local) : base()
		{
			this.local = local;
		}
		
		
		public virtual void  Search(Weight weight, Filter filter, HitCollector results)
		{
			local.Search(weight, filter, results);
		}
		
		public virtual void  Close()
		{
			local.Close();
		}
		
		public virtual int DocFreq(Term term)
		{
			return local.DocFreq(term);
		}
		
		
		public virtual int[] DocFreqs(Term[] terms)
		{
			return local.DocFreqs(terms);
		}
		
		public virtual int MaxDoc()
		{
			return local.MaxDoc();
		}
		
		public virtual TopDocs Search(Weight weight, Filter filter, int n)
		{
			return local.Search(weight, filter, n);
		}
		
		
		public virtual TopFieldDocs Search(Weight weight, Filter filter, int n, Sort sort)
		{
			return local.Search(weight, filter, n, sort);
		}
		
		public virtual Document Doc(int i)
		{
			return local.Doc(i);
		}
		
		public virtual Document Doc(int i, FieldSelector fieldSelector)
		{
			return local.Doc(i, fieldSelector);
		}
		
		public virtual Query Rewrite(Query original)
		{
			return local.Rewrite(original);
		}
		
		public virtual Explanation Explain(Weight weight, int doc)
		{
			return local.Explain(weight, doc);
		}
		
		/// <summary>Exports a searcher for the index in args[0] named
		/// "//localhost/Searchable". 
		/// </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			System.Runtime.Remoting.RemotingConfiguration.Configure("Lucene.Net.Search.RemoteSearchable.config", false);
			System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(new System.Runtime.Remoting.Channels.Http.HttpChannel(1099), false);
			System.String indexName = null;
			
			if (args != null && args.Length == 1)
				indexName = args[0];
			
			if (indexName == null)
			{
				System.Console.Out.WriteLine("Usage: Lucene.Net.Search.RemoteSearchable <index>");
				return ;
			}
			
			// create and install a security manager
            if (true)  // if (System_Renamed.getSecurityManager() == null) // {{Aroush-1.4.3}} Do we need this line?!
            {
                // System_Renamed.setSecurityManager(new RMISecurityManager());     // {{Aroush-1.4.3}} Do we need this line?!
            }
			
			Lucene.Net.Search.Searchable local = new IndexSearcher(indexName);
			RemoteSearchable impl = new RemoteSearchable(local);
			
			// bind the implementation to "Searchable"
			System.Runtime.Remoting.RemotingServices.Marshal(impl, "localhost/Searchable");
			System.Console.ReadLine();
		}
	}
}