//using System;
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
    /// A {@link ProximityQueryNode} represents a query where the terms should meet
    /// specific distance conditions. (a b c) WITHIN [SENTENCE|PARAGRAPH|NUMBER]
    /// [INORDER] ("a" "b" "c") WITHIN [SENTENCE|PARAGRAPH|NUMBER] [INORDER]
    /// 
    /// TODO: Add this to the future standard Lucene parser/processor/builder
    /// </summary>
    public class ProximityQueryNode : BooleanQueryNode
    {
        /**
   * Distance condition: PARAGRAPH, SENTENCE, or NUMBER
   */

        public enum Type
        {
            PARAGRAPH,
            SENTENCE,
            NUMBER
        }

        //        public enum Type
        //        {
        //            PARAGRAPH /*{
        //      @Override
        //            CharSequence toQueryString() { return "WITHIN PARAGRAPH";
        //        }
        //    }*/,
        //            SENTENCE  /*{ 
        //      @Override
        //      CharSequence toQueryString() { return "WITHIN SENTENCE"; }
        //}*/,
        //            NUMBER   /* {
        //      @Override
        //      CharSequence toQueryString() { return "WITHIN"; }
        //    };*/
        //        }

        // LUCENENET TODO: Implement this on enum
        //    internal abstract string ToQueryString();
        //}

        /** utility class containing the distance condition and number */
        public class ProximityType
        {
            internal int pDistance = 0;

            ProximityQueryNode.Type pType/* = null*/;

            public ProximityType(ProximityQueryNode.Type type)
                    : this(type, 0)
            {
            }

            public ProximityType(ProximityQueryNode.Type type, int distance)
            {
                this.pType = type;
                this.pDistance = distance;
            }
        }

        private ProximityQueryNode.Type proximityType = ProximityQueryNode.Type.SENTENCE;
        private int distance = -1;
        private bool inorder = false;
        private string field = null;

        /**
         * @param clauses
         *          - QueryNode children
         * @param field
         *          - field name
         * @param type
         *          - type of proximity query
         * @param distance
         *          - positive integer that specifies the distance
         * @param inorder
         *          - true, if the tokens should be matched in the order of the
         *          clauses
         */
        public ProximityQueryNode(IList<IQueryNode> clauses, string field,
            ProximityQueryNode.Type type, int distance, bool inorder)
            : base(clauses)
        {

            SetLeaf(false);
            this.proximityType = type;
            this.inorder = inorder;
            this.field = field;
            if (type == ProximityQueryNode.Type.NUMBER)
            {
                if (distance <= 0)
                {
                    throw new QueryNodeError(new MessageImpl(
                        QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED, "distance",
                        distance));

                }
                else
                {
                    this.distance = distance;
                }

            }
            ClearFields(clauses, field);
        }

        /**
         * @param clauses
         *          - QueryNode children
         * @param field
         *          - field name
         * @param type
         *          - type of proximity query
         * @param inorder
         *          - true, if the tokens should be matched in the order of the
         *          clauses
         */
        public ProximityQueryNode(IList<IQueryNode> clauses, string field,
            ProximityQueryNode.Type type, bool inorder)
            : this(clauses, field, type, -1, inorder)
        {

        }

        private static void ClearFields(IList<IQueryNode> nodes, string field)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            foreach (IQueryNode clause in nodes)
            {

                if (clause is FieldQueryNode)
                {
                    ((FieldQueryNode)clause).toQueryStringIgnoreFields = true;
                    ((FieldQueryNode)clause).Field = field;
                }
            }
        }

        public ProximityQueryNode.Type GetProximityType()
        {
            return this.proximityType;
        }

        public override string ToString()
        {
            string distanceSTR = ((this.distance == -1) ? ("")
                : (" distance='" + this.distance) + "'");

            if (GetChildren() == null || GetChildren().Count == 0)
                return "<proximity field='" + this.field + "' inorder='" + this.inorder
                    + "' type='" + this.proximityType.ToString() + "'" + distanceSTR
                    + "/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<proximity field='" + this.field + "' inorder='" + this.inorder
                + "' type='" + this.proximityType.ToString() + "'" + distanceSTR + ">");
            foreach (IQueryNode child in GetChildren())
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</proximity>");
            return sb.ToString();
        }


        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            string withinSTR = this.proximityType.ToQueryString()
                + ((this.distance == -1) ? ("") : (" " + this.distance))
                + ((this.inorder) ? (" INORDER") : (""));

            StringBuilder sb = new StringBuilder();
            if (GetChildren() == null || GetChildren().Count == 0)
            {
                // no children case
            }
            else
            {
                string filler = "";
                foreach (IQueryNode child in GetChildren())
                {
                    sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
                    filler = " ";
                }
            }

            if (IsDefaultField(this.field))
            {
                return "( " + sb.ToString() + " ) " + withinSTR;
            }
            else
            {
                return this.field + ":(( " + sb.ToString() + " ) " + withinSTR + ")";
            }
        }


        public override IQueryNode CloneTree()
        {
            ProximityQueryNode clone = (ProximityQueryNode)base.CloneTree();

            clone.proximityType = this.proximityType;
            clone.distance = this.distance;
            clone.field = this.field;

            return clone;
        }

        /**
         * @return the distance
         */
        public int GetDistance()
        {
            return this.distance;
        }

        /**
         * returns null if the field was not specified in the query string
         * 
         * @return the field
         */
        public string GetField()
        {
            return this.field;
        }

        /**
         * returns null if the field was not specified in the query string
         * 
         * @return the field
         */
        public string GetFieldAsString()
        {
            if (this.field == null)
                return null;
            else
                return this.field.ToString();
        }

        /**
         * @param field
         *          the field to set
         */
        public void SetField(string field)
        {
            this.field = field;
        }

        /**
         * @return terms must be matched in the specified order
         */
        public bool IsInOrder()
        {
            return this.inorder;
        }
    }

    public static class ProximityQueryNode_TypeExtensions
    {
        public static string ToQueryString(this ProximityQueryNode.Type type)
        {
            switch (type)
            {
                case ProximityQueryNode.Type.NUMBER:
                    return "WITHIN";
                case ProximityQueryNode.Type.PARAGRAPH:
                    return "WITHIN PARAGRAPH";
                case ProximityQueryNode.Type.SENTENCE:
                    return "WITHIN SENTENCE";
            }

            throw new ArgumentException("Invalid ProximityQueryNode.Type");
        }
    }
}
