/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Expressions;
using Org.Apache.Lucene.Expressions.JS;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Expressions
{
	public class TestExpressionSortField : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestToString()
		{
			Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			bindings.Add(new SortField("popularity", SortField.Type.INT));
			SortField sf = expr.GetSortField(bindings, true);
			NUnit.Framework.Assert.AreEqual("<expr \"sqrt(_score) + ln(popularity)\">!", sf.ToString
				());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEquals()
		{
			Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			bindings.Add(new SortField("popularity", SortField.Type.INT));
			SimpleBindings otherBindings = new SimpleBindings();
			otherBindings.Add(new SortField("_score", SortField.Type.LONG));
			otherBindings.Add(new SortField("popularity", SortField.Type.INT));
			SortField sf1 = expr.GetSortField(bindings, true);
			// different order
			SortField sf2 = expr.GetSortField(bindings, false);
			NUnit.Framework.Assert.IsFalse(sf1.Equals(sf2));
			// different bindings
			sf2 = expr.GetSortField(otherBindings, true);
			NUnit.Framework.Assert.IsFalse(sf1.Equals(sf2));
			// different expression
			Expression other = JavascriptCompiler.Compile("popularity/2");
			sf2 = other.GetSortField(bindings, true);
			NUnit.Framework.Assert.IsFalse(sf1.Equals(sf2));
			// null
			NUnit.Framework.Assert.IsFalse(sf1.Equals(null));
			// same instance:
			NUnit.Framework.Assert.AreEqual(sf1, sf1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNeedsScores()
		{
			SimpleBindings bindings = new SimpleBindings();
			// refers to score directly
			Expression exprA = JavascriptCompiler.Compile("_score");
			// constant
			Expression exprB = JavascriptCompiler.Compile("0");
			// field
			Expression exprC = JavascriptCompiler.Compile("intfield");
			// score + constant
			Expression exprD = JavascriptCompiler.Compile("_score + 0");
			// field + constant
			Expression exprE = JavascriptCompiler.Compile("intfield + 0");
			// expression + constant (score ref'd)
			Expression exprF = JavascriptCompiler.Compile("a + 0");
			// expression + constant
			Expression exprG = JavascriptCompiler.Compile("e + 0");
			// several variables (score ref'd)
			Expression exprH = JavascriptCompiler.Compile("b / c + e * g - sqrt(f)");
			// several variables
			Expression exprI = JavascriptCompiler.Compile("b / c + e * g");
			bindings.Add(new SortField("_score", SortField.Type.SCORE));
			bindings.Add(new SortField("intfield", SortField.Type.INT));
			bindings.Add("a", exprA);
			bindings.Add("b", exprB);
			bindings.Add("c", exprC);
			bindings.Add("d", exprD);
			bindings.Add("e", exprE);
			bindings.Add("f", exprF);
			bindings.Add("g", exprG);
			bindings.Add("h", exprH);
			bindings.Add("i", exprI);
			NUnit.Framework.Assert.IsTrue(exprA.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsFalse(exprB.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsFalse(exprC.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsTrue(exprD.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsFalse(exprE.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsTrue(exprF.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsFalse(exprG.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsTrue(exprH.GetSortField(bindings, true).NeedsScores());
			NUnit.Framework.Assert.IsFalse(exprI.GetSortField(bindings, false).NeedsScores());
		}
	}
}
