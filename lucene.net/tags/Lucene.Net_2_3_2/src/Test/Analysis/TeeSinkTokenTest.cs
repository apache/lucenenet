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

using NUnit.Framework;

using StandardFilter = Lucene.Net.Analysis.Standard.StandardFilter;
using StandardTokenizer = Lucene.Net.Analysis.Standard.StandardTokenizer;
using English = Lucene.Net.Util.English;

namespace Lucene.Net.Analysis
{
	
	/// <summary> tests for the TeeTokenFilter and SinkTokenizer</summary>
	[TestFixture]
	public class TeeSinkTokenTest
	{
		private class AnonymousClassSinkTokenizer : SinkTokenizer
		{
			private void  InitBlock(TeeSinkTokenTest enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TeeSinkTokenTest enclosingInstance;
			public TeeSinkTokenTest Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassSinkTokenizer(TeeSinkTokenTest enclosingInstance, System.Collections.IList Param1) : base(Param1)
			{
				InitBlock(enclosingInstance);
			}
			public override void  Add(Token t)
			{
				if (t != null && t.TermText().ToUpper().Equals("The".ToUpper()))
				{
					base.Add(t);
				}
			}
		}
		
		private class AnonymousClassSinkTokenizer1 : SinkTokenizer
		{
			private void  InitBlock(TeeSinkTokenTest enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TeeSinkTokenTest enclosingInstance;
			public TeeSinkTokenTest Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassSinkTokenizer1(TeeSinkTokenTest enclosingInstance, System.Collections.IList Param1) : base(Param1)
			{
				InitBlock(enclosingInstance);
			}
			public override void  Add(Token t)
			{
				if (t != null && t.TermText().ToUpper().Equals("The".ToUpper()))
				{
					base.Add(t);
				}
			}
		}
		
		private class AnonymousClassSinkTokenizer2 : SinkTokenizer
		{
			private void  InitBlock(TeeSinkTokenTest enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TeeSinkTokenTest enclosingInstance;
			public TeeSinkTokenTest Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassSinkTokenizer2(TeeSinkTokenTest enclosingInstance, System.Collections.IList Param1) : base(Param1)
			{
				InitBlock(enclosingInstance);
			}
			public override void  Add(Token t)
			{
				if (t != null && t.TermText().ToUpper().Equals("Dogs".ToUpper()))
				{
					base.Add(t);
				}
			}
		}
		protected internal System.Text.StringBuilder buffer1;
		protected internal System.Text.StringBuilder buffer2;
		protected internal System.String[] tokens1;
		protected internal System.String[] tokens2;
		
		[SetUp]
		public virtual void  SetUp()
		{
			tokens1 = new System.String[]{"The", "quick", "Burgundy", "Fox", "jumped", "over", "the", "lazy", "Red", "Dogs"};
			tokens2 = new System.String[]{"The", "Lazy", "Dogs", "should", "stay", "on", "the", "porch"};
			buffer1 = new System.Text.StringBuilder();
			
			for (int i = 0; i < tokens1.Length; i++)
			{
				buffer1.Append(tokens1[i]).Append(' ');
			}
			buffer2 = new System.Text.StringBuilder();
			for (int i = 0; i < tokens2.Length; i++)
			{
				buffer2.Append(tokens2[i]).Append(' ');
			}
		}
		
		[TearDown]
		public virtual void  TearDown()
		{
			
		}
		
		[Test]
		public virtual void  Test()
		{
			
			SinkTokenizer sink1 = new AnonymousClassSinkTokenizer(this, null);
			TokenStream source = new TeeTokenFilter(new WhitespaceTokenizer(new System.IO.StringReader(buffer1.ToString())), sink1);
			Token token = null;
			int i = 0;
			while ((token = source.Next()) != null)
			{
				Assert.IsTrue(token.TermText().Equals(tokens1[i]) == true, token.TermText() + " is not equal to " + tokens1[i]);
				i++;
			}
			Assert.IsTrue(i == tokens1.Length, i + " does not equal: " + tokens1.Length);
			Assert.IsTrue(sink1.GetTokens().Count == 2, "sink1 Size: " + sink1.GetTokens().Count + " is not: " + 2);
			i = 0;
			while ((token = sink1.Next()) != null)
			{
				Assert.IsTrue(token.TermText().ToUpper().Equals("The".ToUpper()) == true, token.TermText() + " is not equal to " + "The");
				i++;
			}
			Assert.IsTrue(i == sink1.GetTokens().Count, i + " does not equal: " + sink1.GetTokens().Count);
		}
		
		[Test]
		public virtual void  TestMultipleSources()
		{
			SinkTokenizer theDetector = new AnonymousClassSinkTokenizer1(this, null);
			SinkTokenizer dogDetector = new AnonymousClassSinkTokenizer2(this, null);
			TokenStream source1 = new CachingTokenFilter(new TeeTokenFilter(new TeeTokenFilter(new WhitespaceTokenizer(new System.IO.StringReader(buffer1.ToString())), theDetector), dogDetector));
			TokenStream source2 = new TeeTokenFilter(new TeeTokenFilter(new WhitespaceTokenizer(new System.IO.StringReader(buffer2.ToString())), theDetector), dogDetector);
			Token token = null;
			int i = 0;
			while ((token = source1.Next()) != null)
			{
				Assert.IsTrue(token.TermText().Equals(tokens1[i]) == true, token.TermText() + " is not equal to " + tokens1[i]);
				i++;
			}
			Assert.IsTrue(i == tokens1.Length, i + " does not equal: " + tokens1.Length);
			Assert.IsTrue(theDetector.GetTokens().Count == 2, "theDetector Size: " + theDetector.GetTokens().Count + " is not: " + 2);
			Assert.IsTrue(dogDetector.GetTokens().Count == 1, "dogDetector Size: " + dogDetector.GetTokens().Count + " is not: " + 1);
			i = 0;
			while ((token = source2.Next()) != null)
			{
				Assert.IsTrue(token.TermText().Equals(tokens2[i]) == true, token.TermText() + " is not equal to " + tokens2[i]);
				i++;
			}
			Assert.IsTrue(i == tokens2.Length, i + " does not equal: " + tokens2.Length);
			Assert.IsTrue(theDetector.GetTokens().Count == 4, "theDetector Size: " + theDetector.GetTokens().Count + " is not: " + 4);
			Assert.IsTrue(dogDetector.GetTokens().Count == 2, "dogDetector Size: " + dogDetector.GetTokens().Count + " is not: " + 2);
			i = 0;
			while ((token = theDetector.Next()) != null)
			{
				Assert.IsTrue(token.TermText().ToUpper().Equals("The".ToUpper()) == true, token.TermText() + " is not equal to " + "The");
				i++;
			}
			Assert.IsTrue(i == theDetector.GetTokens().Count, i + " does not equal: " + theDetector.GetTokens().Count);
			i = 0;
			while ((token = dogDetector.Next()) != null)
			{
				Assert.IsTrue(token.TermText().ToUpper().Equals("Dogs".ToUpper()) == true, token.TermText() + " is not equal to " + "Dogs");
				i++;
			}
			Assert.IsTrue(i == dogDetector.GetTokens().Count, i + " does not equal: " + dogDetector.GetTokens().Count);
			source1.Reset();
			TokenStream lowerCasing = new LowerCaseFilter(source1);
			i = 0;
			while ((token = lowerCasing.Next()) != null)
			{
				Assert.IsTrue(token.TermText().Equals(tokens1[i].ToLower()) == true, token.TermText() + " is not equal to " + tokens1[i].ToLower());
				i++;
			}
			Assert.IsTrue(i == tokens1.Length, i + " does not equal: " + tokens1.Length);
		}
		
		/// <summary> Not an explicit test, just useful to print out some info on performance
		/// 
		/// </summary>
		/// <throws>  Exception </throws>
		[Test]
		public virtual void  TestPerformance()
		{
			int[] tokCount = new int[]{100, 500, 1000, 2000, 5000, 10000};
			int[] modCounts = new int[]{1, 2, 5, 10, 20, 50, 100, 200, 500};
			for (int k = 0; k < tokCount.Length; k++)
			{
				System.Text.StringBuilder buffer = new System.Text.StringBuilder();
				System.Console.Out.WriteLine("-----Tokens: " + tokCount[k] + "-----");
				for (int i = 0; i < tokCount[k]; i++)
				{
					buffer.Append(English.IntToEnglish(i).ToUpper()).Append(' ');
				}
				//make sure we produce the same tokens
				ModuloSinkTokenizer sink = new ModuloSinkTokenizer(this, tokCount[k], 100);
				Token next = new Token();
				TokenStream result = new TeeTokenFilter(new StandardFilter(new StandardTokenizer(new System.IO.StringReader(buffer.ToString()))), sink);
				while ((next = result.Next(next)) != null)
				{
				}
				result = new ModuloTokenFilter(this, new StandardFilter(new StandardTokenizer(new System.IO.StringReader(buffer.ToString()))), 100);
				next = new Token();
				System.Collections.IList tmp = new System.Collections.ArrayList();
				while ((next = result.Next(next)) != null)
				{
					tmp.Add(next.Clone());
				}
				System.Collections.IList sinkList = sink.GetTokens();
				Assert.IsTrue(tmp.Count == sinkList.Count, "tmp Size: " + tmp.Count + " is not: " + sinkList.Count);
				for (int i = 0; i < tmp.Count; i++)
				{
					Token tfTok = (Token) tmp[i];
					Token sinkTok = (Token) sinkList[i];
					Assert.IsTrue(tfTok.TermText().Equals(sinkTok.TermText()) == true, tfTok.TermText() + " is not equal to " + sinkTok.TermText() + " at token: " + i);
				}
				//simulate two fields, each being analyzed once, for 20 documents
				
				for (int j = 0; j < modCounts.Length; j++)
				{
					int tfPos = 0;
					long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
					for (int i = 0; i < 20; i++)
					{
						next = new Token();
						result = new StandardFilter(new StandardTokenizer(new System.IO.StringReader(buffer.ToString())));
						while ((next = result.Next(next)) != null)
						{
							tfPos += next.GetPositionIncrement();
						}
						next = new Token();
						result = new ModuloTokenFilter(this, new StandardFilter(new StandardTokenizer(new System.IO.StringReader(buffer.ToString()))), modCounts[j]);
						while ((next = result.Next(next)) != null)
						{
							tfPos += next.GetPositionIncrement();
						}
					}
					long finish = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
					System.Console.Out.WriteLine("ModCount: " + modCounts[j] + " Two fields took " + (finish - start) + " ms");
					int sinkPos = 0;
					//simulate one field with one sink
					start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
					for (int i = 0; i < 20; i++)
					{
						sink = new ModuloSinkTokenizer(this, tokCount[k], modCounts[j]);
						next = new Token();
						result = new TeeTokenFilter(new StandardFilter(new StandardTokenizer(new System.IO.StringReader(buffer.ToString()))), sink);
						while ((next = result.Next(next)) != null)
						{
							sinkPos += next.GetPositionIncrement();
						}
						//System.out.println("Modulo--------");
						result = sink;
						while ((next = result.Next(next)) != null)
						{
							sinkPos += next.GetPositionIncrement();
						}
					}
					finish = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
					System.Console.Out.WriteLine("ModCount: " + modCounts[j] + " Tee fields took " + (finish - start) + " ms");
					Assert.IsTrue(sinkPos == tfPos, sinkPos + " does not equal: " + tfPos);
				}
				System.Console.Out.WriteLine("- End Tokens: " + tokCount[k] + "-----");
			}
		}
		
		
		internal class ModuloTokenFilter : TokenFilter
		{
			private void  InitBlock(TeeSinkTokenTest enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TeeSinkTokenTest enclosingInstance;
			public TeeSinkTokenTest Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			internal int modCount;
			
			internal ModuloTokenFilter(TeeSinkTokenTest enclosingInstance, TokenStream input, int mc):base(input)
			{
				InitBlock(enclosingInstance);
				modCount = mc;
			}
			
			internal int count = 0;
			
			//return every 100 tokens
			public override Token Next(Token result)
			{
				
				while ((result = input.Next(result)) != null && count % modCount != 0)
				{
					count++;
				}
				count++;
				return result;
			}
		}
		
		internal class ModuloSinkTokenizer : SinkTokenizer
		{
			private void  InitBlock(TeeSinkTokenTest enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TeeSinkTokenTest enclosingInstance;
			public TeeSinkTokenTest Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal int count = 0;
			internal int modCount;
			
			
			internal ModuloSinkTokenizer(TeeSinkTokenTest enclosingInstance, int numToks, int mc)
			{
				InitBlock(enclosingInstance);
				modCount = mc;
				lst = new System.Collections.ArrayList(numToks % mc);
			}
			
			public override void  Add(Token t)
			{
				if (t != null && count % modCount == 0)
				{
					lst.Add(t.Clone());
				}
				count++;
			}
		}
	}
}