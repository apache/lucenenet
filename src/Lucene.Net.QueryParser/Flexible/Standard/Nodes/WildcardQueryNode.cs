using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// A {@link WildcardQueryNode} represents wildcard query This does not apply to
    /// phrases. Examples: a*b*c Fl?w? m?ke*g
    /// </summary>
    public class WildcardQueryNode : FieldQueryNode
    {
        /**
        * @param field
        *          - field name
        * @param text
        *          - value that contains one or more wild card characters (? or *)
        * @param begin
        *          - position in the query string
        * @param end
        *          - position in the query string
        */
        // LUCENENET specific overload for passing text as string
        public WildcardQueryNode(string field, string text, int begin,
            int end)
            : this(field, new StringCharSequenceWrapper(text), begin, end)
        {
        }

        /**
        * @param field
        *          - field name
        * @param text
        *          - value that contains one or more wild card characters (? or *)
        * @param begin
        *          - position in the query string
        * @param end
        *          - position in the query string
        */
        public WildcardQueryNode(string field, ICharSequence text, int begin,
            int end)
            : base(field, text, begin, end)
        {
        }

        public WildcardQueryNode(FieldQueryNode fqn)
            : this(fqn.Field, fqn.Text, fqn.GetBegin(), fqn.GetEnd())
        {
        }


        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return this.text.ToString();
            }
            else
            {
                return this.field + ":" + this.text;
            }
        }

        public override string ToString()
        {
            return "<wildcard field='" + this.field + "' term='" + this.text + "'/>";
        }


        public override IQueryNode CloneTree()
        {
            WildcardQueryNode clone = (WildcardQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
