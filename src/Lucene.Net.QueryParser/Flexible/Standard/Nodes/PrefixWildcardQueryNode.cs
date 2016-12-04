using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Support;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// A {@link PrefixWildcardQueryNode} represents wildcardquery that matches abc*
    /// or *. This does not apply to phrases, this is a special case on the original
    /// lucene parser. TODO: refactor the code to remove this special case from the
    /// parser. and probably do it on a Processor
    /// </summary>
    public class PrefixWildcardQueryNode : WildcardQueryNode
    {
        /**
        * @param field
        *          - field name
        * @param text
        *          - value including the wildcard
        * @param begin
        *          - position in the query string
        * @param end
        *          - position in the query string
        */
        // LUCENENET specific overload for passing text as string
        public PrefixWildcardQueryNode(string field, string text,
            int begin, int end)
            : this(field, new StringCharSequenceWrapper(text), begin, end)
        {
        }

        /**
        * @param field
        *          - field name
        * @param text
        *          - value including the wildcard
        * @param begin
        *          - position in the query string
        * @param end
        *          - position in the query string
        */
        public PrefixWildcardQueryNode(string field, ICharSequence text,
            int begin, int end)
            : base(field, text, begin, end)
        {
        }

        public PrefixWildcardQueryNode(FieldQueryNode fqn)
            : this(fqn.Field, fqn.Text, fqn.Begin, fqn.End)
        {
        }

        public override string ToString()
        {
            return "<prefixWildcard field='" + this.field + "' term='" + this.text
                + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            PrefixWildcardQueryNode clone = (PrefixWildcardQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
