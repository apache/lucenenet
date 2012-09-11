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
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Test.Analysis
{
	
	/// <summary>Base class for all Lucene unit tests that use TokenStreams.</summary>
	public abstract class BaseTokenStreamTestCase:LuceneTestCase
	{
	    public BaseTokenStreamTestCase()
	    { }

		public BaseTokenStreamTestCase(System.String name):base(name)
		{ }
		
		// some helpers to test Analyzers and TokenStreams:
        public interface ICheckClearAttributesAttribute : Lucene.Net.Util.IAttribute
        {
               bool GetAndResetClearCalled();
        }

        public class CheckClearAttributesAttribute : Lucene.Net.Util.Attribute, ICheckClearAttributesAttribute 
        {
            private bool clearCalled = false;

            public bool GetAndResetClearCalled()
            {
                try
                {
                    return clearCalled;
                }
                finally
                {
                    clearCalled = false;
                }
            }

            public override void Clear()
            {
                clearCalled = true;
            }

            public  override bool Equals(Object other) 
            {
                return (
                other is CheckClearAttributesAttribute &&
                ((CheckClearAttributesAttribute) other).clearCalled == this.clearCalled
                );
            }

            public override int GetHashCode()
            {
                //Java: return 76137213 ^ Boolean.valueOf(clearCalled).hashCode();
                return 76137213 ^ clearCalled.GetHashCode();
            }

            public override void CopyTo(Lucene.Net.Util.Attribute target)
            {
                target.Clear();
            }
        }

        public static void AssertTokenStreamContents(TokenStream ts, System.String[] output, int[] startOffsets, int[] endOffsets, System.String[] types, int[] posIncrements, int? finalOffset)
        {
            Assert.IsNotNull(output);
            ICheckClearAttributesAttribute checkClearAtt = ts.AddAttribute<ICheckClearAttributesAttribute>();

            Assert.IsTrue(ts.HasAttribute<ITermAttribute>(), "has no TermAttribute");
            ITermAttribute termAtt = ts.GetAttribute<ITermAttribute>();

            IOffsetAttribute offsetAtt = null;
            if (startOffsets != null || endOffsets != null || finalOffset != null)
            {
                Assert.IsTrue(ts.HasAttribute<IOffsetAttribute>(), "has no OffsetAttribute");
                offsetAtt = ts.GetAttribute<IOffsetAttribute>();
            }
    
            ITypeAttribute typeAtt = null;
            if (types != null)
            {
                Assert.IsTrue(ts.HasAttribute<ITypeAttribute>(), "has no TypeAttribute");
                typeAtt = ts.GetAttribute<ITypeAttribute>();
            }
            
            IPositionIncrementAttribute posIncrAtt = null;
            if (posIncrements != null)
            {
                Assert.IsTrue(ts.HasAttribute<IPositionIncrementAttribute>(), "has no PositionIncrementAttribute");
                posIncrAtt = ts.GetAttribute<IPositionIncrementAttribute>();
            }

            ts.Reset();
            for (int i = 0; i < output.Length; i++)
            {
                // extra safety to enforce, that the state is not preserved and also assign bogus values
                ts.ClearAttributes();
                termAtt.SetTermBuffer("bogusTerm");
                if (offsetAtt != null) offsetAtt.SetOffset(14584724, 24683243);
                if (typeAtt != null) typeAtt.Type = "bogusType";
                if (posIncrAtt != null) posIncrAtt.PositionIncrement = 45987657;

                checkClearAtt.GetAndResetClearCalled(); // reset it, because we called clearAttribute() before
                Assert.IsTrue(ts.IncrementToken(), "token " + i + " does not exist");
                Assert.IsTrue(checkClearAtt.GetAndResetClearCalled(), "clearAttributes() was not called correctly in TokenStream chain");

                Assert.AreEqual(output[i], termAtt.Term, "term " + i);
                if (startOffsets != null)
                    Assert.AreEqual(startOffsets[i], offsetAtt.StartOffset, "startOffset " + i);
                if (endOffsets != null)
                    Assert.AreEqual(endOffsets[i], offsetAtt.EndOffset, "endOffset " + i);
                if (types != null)
                    Assert.AreEqual(types[i], typeAtt.Type, "type " + i);
                if (posIncrements != null)
                    Assert.AreEqual(posIncrements[i], posIncrAtt.PositionIncrement, "posIncrement " + i);
            }
            Assert.IsFalse(ts.IncrementToken(), "end of stream");
            ts.End();
            if (finalOffset.HasValue)
                Assert.AreEqual(finalOffset, offsetAtt.EndOffset, "finalOffset ");
            ts.Close();
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, String[] types, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output)
        {
            AssertTokenStreamContents(ts, output, null, null, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, String[] types)
        {
            AssertTokenStreamContents(ts, output, null, null, types, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, null, null, null, posIncrements, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, finalOffset);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, String[] types, int[] posIncrements)
        {
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, String[] types)
        {
            AssertAnalyzesTo(a, input, output, null, null, types, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, posIncrements);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, posIncrements);
        }


        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, String[] types, int[] posIncrements)
        {
            AssertTokenStreamContents(a.ReusableTokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, input.Length);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output)
        {
            AssertAnalyzesToReuse(a, input, output, null, null, null, null);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, String[] types)
        {
            AssertAnalyzesToReuse(a, input, output, null, null, types, null);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] posIncrements)
        {
            AssertAnalyzesToReuse(a, input, output, null, null, null, posIncrements);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertAnalyzesToReuse(a, input, output, startOffsets, endOffsets, null, null);
        }

        public static void AssertAnalyzesToReuse(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertAnalyzesToReuse(a, input, output, startOffsets, endOffsets, null, posIncrements);
        }

		// simple utility method for testing stemmers
		
		public static void  CheckOneTerm(Analyzer a, System.String input, System.String expected)
		{
			AssertAnalyzesTo(a, input, new System.String[]{expected});
		}
		
		public static void  CheckOneTermReuse(Analyzer a, System.String input, System.String expected)
		{
			AssertAnalyzesToReuse(a, input, new System.String[]{expected});
		}
	}
}