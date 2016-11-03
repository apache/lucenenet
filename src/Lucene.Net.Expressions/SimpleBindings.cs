using System;
using System.Collections.Generic;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;

namespace Lucene.Net.Expressions
{
    /// <summary>
    /// Simple class that binds expression variable names to
    /// <see cref="Lucene.Net.Search.SortField">Lucene.Net.Search.SortField
    /// 	</see>
    /// s
    /// or other
    /// <see cref="Expression">Expression</see>
    /// s.
    /// <p>
    /// Example usage:
    /// <pre class="prettyprint">
    /// SimpleBindings bindings = new SimpleBindings();
    /// // document's text relevance score
    /// bindings.add(new SortField("_score", SortField.Type.SCORE));
    /// // integer NumericDocValues field (or from FieldCache)
    /// bindings.add(new SortField("popularity", SortField.Type.INT));
    /// // another expression
    /// bindings.add("recency", myRecencyExpression);
    /// // create a sort field in reverse order
    /// Sort sort = new Sort(expr.getSortField(bindings, true));
    /// </pre>
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public sealed class SimpleBindings : Bindings
    {
        internal readonly IDictionary<string, object> map = new Dictionary<string, object
            >();

        /// <summary>Adds a SortField to the bindings.</summary>
        /// <remarks>
        /// Adds a SortField to the bindings.
        /// <p>
        /// This can be used to reference a DocValuesField, a field from
        /// FieldCache, the document's score, etc.
        /// </remarks>
        public void Add(SortField sortField)
        {
            map[sortField.Field] = sortField;
        }

        /// <summary>Adds an Expression to the bindings.</summary>
        /// <remarks>
        /// Adds an Expression to the bindings.
        /// <p>
        /// This can be used to reference expressions from other expressions.
        /// </remarks>
        public void Add(string name, Expression expression)
        {
            map[name] = expression;
        }

        public override ValueSource GetValueSource(string name)
        {

            object o;
            //.NET Port. Directly looking up a missing key will throw a KeyNotFoundException
            if (!map.TryGetValue(name, out o))
            {
                throw new ArgumentException("Invalid reference '" + name + "'");
            }
            var expression = o as Expression;
            if (expression != null)
            {
                return expression.GetValueSource(this);
            }
            SortField field = (SortField)o;
            switch (field.Type)
            {
                case SortField.Type_e.INT:
                    {
                        return new IntFieldSource(field.Field, (FieldCache.IIntParser)field.Parser);
                    }

                case SortField.Type_e.LONG:
                    {
                        return new LongFieldSource(field.Field, (FieldCache.ILongParser)field.Parser);
                    }

                case SortField.Type_e.FLOAT:
                    {
                        return new FloatFieldSource(field.Field, (FieldCache.IFloatParser)field.Parser);
                    }

                case SortField.Type_e.DOUBLE:
                    {
                        return new DoubleFieldSource(field.Field, (FieldCache.IDoubleParser)field.Parser);
                    }

                case SortField.Type_e.SCORE:
                    {
                        return GetScoreValueSource();
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }

        }

        /// <summary>Traverses the graph of bindings, checking there are no cycles or missing references
        /// 	</summary>
        /// <exception cref="System.ArgumentException">if the bindings is inconsistent</exception>
        public void Validate()
        {
            foreach (object o in map.Values)
            {
                if (o is Expression)
                {
                    Expression expr = (Expression)o;

                    expr.GetValueSource(this);
                }
            }
        }
    }
}
