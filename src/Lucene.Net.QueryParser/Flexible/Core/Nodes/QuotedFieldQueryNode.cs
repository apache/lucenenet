using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link QuotedFieldQueryNode} represents phrase query. Example:
    /// "life is great"
    /// </summary>
    public class QuotedFieldQueryNode : FieldQueryNode
    {
        /**
        * @param field
        *          - field name
        * @param text
        *          - value
        * @param begin
        *          - position in the query string
        * @param end
        *          - position in the query string
        */
        // LUCENENET specific overload for text string
        public QuotedFieldQueryNode(string field, string text, int begin,
            int end)
            : this(field, text.ToCharSequence(), begin, end)
        {
        }

        /**
        * @param field
        *          - field name
        * @param text
        *          - value
        * @param begin
        *          - position in the query string
        * @param end
        *          - position in the query string
        */
        public QuotedFieldQueryNode(string field, ICharSequence text, int begin,
            int end)
            : base(field, text, begin, end)
        {
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return "\"" + GetTermEscapeQuoted(escaper) + "\"";
            }
            else
            {
                return this.field + ":" + "\"" + GetTermEscapeQuoted(escaper) + "\"";
            }
        }

        public override string ToString()
        {
            return "<quotedfield start='" + this.begin + "' end='" + this.end
                + "' field='" + this.field + "' term='" + this.text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            QuotedFieldQueryNode clone = (QuotedFieldQueryNode)base.CloneTree();
            // nothing to do here
            return clone;
        }
    }
}
