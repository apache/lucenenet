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
// {{Aroush}} using junit.framework;
using Lucene.Net.Analysis;
namespace Lucene.Net.Analysis.Snowball
{
	
	public class TestSnowball
	{
		private class AnonymousClassTokenStream : TokenStream
		{
			public AnonymousClassTokenStream(Token tok, TestSnowball enclosingInstance)
			{
				InitBlock(tok, enclosingInstance);
			}
			private void  InitBlock(Token tok, TestSnowball enclosingInstance)
			{
				this.tok = tok;
				this.enclosingInstance = enclosingInstance;
			}
			private Token tok;
			private TestSnowball enclosingInstance;
			public TestSnowball Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
            public override Token Next()
			{
				return tok;
			}
		}
		
		public virtual void  AssertAnalyzesTo(Analyzer a, System.String input, System.String[] output)
		{
			TokenStream ts = a.TokenStream("dummy", new System.IO.StringReader(input));
			for (int i = 0; i < output.Length; i++)
			{
				Token t = ts.Next();
				System.Diagnostics.Trace.Assert(t != null); ////   assertNotNull(t);
				System.Diagnostics.Trace.Assert(output[i] == t.TermText()); //// assertEquals(output[i], t.TermText());
			}
			System.Diagnostics.Trace.Assert(ts.Next() == null); //// assertNull(ts.Next());
			ts.Close();
		}
		
		public virtual void  TestEnglish()
		{
			Analyzer a = new SnowballAnalyzer("English");
			AssertAnalyzesTo(a, "he abhorred accents", new System.String[]{"he", "abhor", "accent"});
		}
		
		public virtual void  TestFilterTokens()
		{
			Token tok = new Token("accents", 2, 7, "wrd");
			tok.SetPositionIncrement(3);
			
			SnowballFilter filter = new SnowballFilter(new AnonymousClassTokenStream(tok, this), "English");
			
			Token newtok = filter.Next();
			
			System.Diagnostics.Trace.Assert("accent" == newtok.TermText()); //// assertEquals("accent", newtok.TermText());
			System.Diagnostics.Trace.Assert(2 == newtok.StartOffset()); //// assertEquals(2, newtok.StartOffset());
			System.Diagnostics.Trace.Assert(7 == newtok.EndOffset()); //// assertEquals(7, newtok.EndOffset());
			System.Diagnostics.Trace.Assert("wrd" == newtok.Type()); //// assertEquals("wrd", newtok.Type());
			System.Diagnostics.Trace.Assert(3 == newtok.GetPositionIncrement()); //// assertEquals(3, newtok.GetPositionIncrement());
		}

        //// {{Aroush}}
        [STAThread]
        public static void  Main(System.String[] args)
        {
            TestSnowball t = new TestSnowball();

            t.TestEnglish();
            t.TestFilterTokens();
        }
        //// {{Aroush}}
	}
}
