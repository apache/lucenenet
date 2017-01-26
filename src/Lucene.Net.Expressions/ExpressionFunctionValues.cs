using System;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;

namespace Lucene.Net.Expressions
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Queries.Function.FunctionValues">Lucene.Net.Queries.Function.FunctionValues
	/// 	</see>
	/// which evaluates an expression
	/// </summary>
	internal class ExpressionFunctionValues : DoubleDocValues
	{
		internal readonly Expression expression;
		internal readonly FunctionValues[] functionValues;

		internal int currentDocument = -1;
		internal double currentValue;

		internal ExpressionFunctionValues(ValueSource parent, Expression expression, FunctionValues[] functionValues) 
            : base(parent)
		{
			if (expression == null)
			{
				throw new ArgumentNullException();
			}
			if (functionValues == null)
			{
				throw new ArgumentNullException();
			}
			this.expression = expression;
			this.functionValues = functionValues;
		}

		public override double DoubleVal(int document)
		{
			if (currentDocument != document)
			{
				currentDocument = document;
				currentValue = expression.Evaluate(document, functionValues);
			}
			return currentValue;
		}
	}
}
