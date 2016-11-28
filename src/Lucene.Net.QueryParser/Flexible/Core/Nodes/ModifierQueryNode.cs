using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link ModifierQueryNode} indicates the modifier value (+,-,?,NONE) for
    /// each term on the query string. For example "+t1 -t2 t3" will have a tree of:
    /// <blockquote>
    /// &lt;BooleanQueryNode&gt; &lt;ModifierQueryNode modifier="MOD_REQ"&gt; &lt;t1/&gt;
    /// &lt;/ModifierQueryNode&gt; &lt;ModifierQueryNode modifier="MOD_NOT"&gt; &lt;t2/&gt;
    /// &lt;/ModifierQueryNode&gt; &lt;t3/&gt; &lt;/BooleanQueryNode&gt;
    /// </blockquote>
    /// </summary>
    public class ModifierQueryNode : QueryNodeImpl
    {
        
        // LUCENENET NOTE: Modifier enum moved outside of this class



        private Modifier modifier = Modifier.MOD_NONE;

        /**
         * Used to store the modifier value on the original query string
         * 
         * @param query
         *          - QueryNode subtree
         * @param mod
         *          - Modifier Value
         */
        public ModifierQueryNode(IQueryNode query, Modifier mod)
        {
            if (query == null)
            {
                throw new QueryNodeError(new MessageImpl(
                    QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED, "query", "null"));
            }

            Allocate();
            SetLeaf(false);
            Add(query);
            this.modifier = mod;
        }

        public IQueryNode GetChild()
        {
            return GetChildren()[0];
        }

        public Modifier GetModifier()
        {
            return this.modifier;
        }


        public override string ToString()
        {
            return "<modifier operation='" + this.modifier.ToString() + "'>" + "\n"
                + GetChild().ToString() + "\n</modifier>";
        }


        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (GetChild() == null)
                return "";

            String leftParenthensis = "";
            String rightParenthensis = "";

            if (GetChild() != null && GetChild() is ModifierQueryNode)
            {
                leftParenthensis = "(";
                rightParenthensis = ")";
            }

            if (GetChild() is BooleanQueryNode)
            {
                return this.modifier.ToLargeString() + leftParenthensis
                    + GetChild().ToQueryString(escapeSyntaxParser) + rightParenthensis;
            }
            else
            {
                return this.modifier.ToDigitString() + leftParenthensis
                    + GetChild().ToQueryString(escapeSyntaxParser) + rightParenthensis;
            }
        }


        public override IQueryNode CloneTree()
        {
            ModifierQueryNode clone = (ModifierQueryNode)base.CloneTree();

            clone.modifier = this.modifier;

            return clone;
        }

        public void setChild(IQueryNode child)
        {
            List<IQueryNode> list = new List<IQueryNode>();
            list.Add(child);
            this.Set(list);
        }
    }

    /**
   * Modifier type: such as required (REQ), prohibited (NOT)
   */
    public enum Modifier
    {
        MOD_NONE,
        MOD_NOT,
        MOD_REQ
    }

    public static class ModifierExtensions
    {
        // LUCENENET TODO: Work out how to override ToString() (or test this) so this string can be made
        //public static string ToString()
        //{
        //    switch (this)
        //    {
        //        case MOD_NONE:
        //            return "MOD_NONE";
        //        case MOD_NOT:
        //            return "MOD_NOT";
        //        case MOD_REQ:
        //            return "MOD_REQ";
        //    }
        //    // this code is never executed
        //    return "MOD_DEFAULT";
        //}

        public static string ToDigitString(this Modifier modifier)
        {
            switch (modifier)
            {
                case Modifier.MOD_NONE:
                    return "";
                case Modifier.MOD_NOT:
                    return "-";
                case Modifier.MOD_REQ:
                    return "+";
            }
            // this code is never executed
            return "";
        }

        public static string ToLargeString(this Modifier modifier)
        {
            switch (modifier)
            {
                case Modifier.MOD_NONE:
                    return "";
                case Modifier.MOD_NOT:
                    return "NOT ";
                case Modifier.MOD_REQ:
                    return "+";
            }
            // this code is never executed
            return "";
        }
    }
}
