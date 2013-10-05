using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class ModifierQueryNode : QueryNode
    {
        public enum Modifier
        {
            MOD_NONE, MOD_NOT, MOD_REQ
        }

        private Modifier modifier = Modifier.MOD_NONE;

        public ModifierQueryNode(IQueryNode query, Modifier mod)
        {
            if (query == null)
            {
                throw new QueryNodeError(new Message(QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED, "query", "null"));
            }

            Allocate();
            SetLeaf(false);
            Add(query);
            this.modifier = mod;
        }

        public IQueryNode Child
        {
            get
            {
                return Children[0];
            }
        }

        public Modifier ModifierValue
        {
            get
            {
                return this.modifier;
            }
        }

        public override string ToString()
        {
            return "<modifier operation='" + this.modifier.ToString() + "'>" + "\n"
                + Child.ToString() + "\n</modifier>";
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (Child == null)
                return new StringCharSequenceWrapper("");

            String leftParenthensis = "";
            String rightParenthensis = "";

            if (Child is ModifierQueryNode)
            {
                leftParenthensis = "(";
                rightParenthensis = ")";
            }

            if (Child is BooleanQueryNode)
            {
                return new StringCharSequenceWrapper(this.modifier.ToLargeString() + leftParenthensis
                    + Child.ToQueryString(escapeSyntaxParser) + rightParenthensis);
            }
            else
            {
                return new StringCharSequenceWrapper(this.modifier.ToDigitString() + leftParenthensis
                    + Child.ToQueryString(escapeSyntaxParser) + rightParenthensis);
            }
        }
    }


    public static class ModifierExtensions
    {
        public static string ToString(this ModifierQueryNode.Modifier modifier)
        {
            switch (modifier)
            {
                case ModifierQueryNode.Modifier.MOD_NONE:
                    return "MOD_NONE";
                case ModifierQueryNode.Modifier.MOD_NOT:
                    return "MOD_NOT";
                case ModifierQueryNode.Modifier.MOD_REQ:
                    return "MOD_REQ";
            }
            // this code is never executed
            return "MOD_DEFAULT";
        }

        public static string ToDigitString(this ModifierQueryNode.Modifier modifier)
        {
            switch (modifier)
            {
                case ModifierQueryNode.Modifier.MOD_NONE:
                    return "";
                case ModifierQueryNode.Modifier.MOD_NOT:
                    return "-";
                case ModifierQueryNode.Modifier.MOD_REQ:
                    return "+";
            }
            // this code is never executed
            return "";
        }

        public static string ToLargeString(this ModifierQueryNode.Modifier modifier)
        {
            switch (modifier)
            {
                case ModifierQueryNode.Modifier.MOD_NONE:
                    return "";
                case ModifierQueryNode.Modifier.MOD_NOT:
                    return "NOT ";
                case ModifierQueryNode.Modifier.MOD_REQ:
                    return "+";
            }
            // this code is never executed
            return "";
        }
    }
}
