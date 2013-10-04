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
    public class ProximityQueryNode : BooleanQueryNode
    {
        public enum Type
        {
            PARAGRAPH,
            SENTENCE,
            NUMBER
        }

        // .NET Port: This is necessary as enums are value types in .NET and can't have override/abstract methods
        public static string TypeToQueryString(Type t)
        {
            switch (t)
            {
                case Type.PARAGRAPH:
                    return "WITHIN PARAGRAPH";
                case Type.SENTENCE:
                    return "WITHIN SENTENCE";
                case Type.NUMBER:
                    return "WITHIN";
                default:
                    throw new InvalidOperationException("Not supported");
            }
        }

        public class ProximityType
        {
            internal int pDistance = 0;

            internal Type? pType = null;

            public ProximityType(Type type)
                : this(type, 0)
            {
            }

            public ProximityType(Type type, int distance)
            {
                this.pType = type;
                this.pDistance = distance;
            }
        }

        private Type proximityType = Type.SENTENCE;
        private int distance = -1;
        private bool inorder = false;
        private string field = null;

        public ProximityQueryNode(IList<IQueryNode> clauses, string field, Type type, int distance, bool inorder)
            : base(clauses)
        {
            SetLeaf(false);
            this.proximityType = type;
            this.inorder = inorder;
            this.field = field;
            if (type == Type.NUMBER)
            {
                if (distance <= 0)
                {
                    throw new QueryNodeError(new Message(QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED, "distance", distance));

                }
                else
                {
                    this.distance = distance;
                }

            }
            ClearFields(clauses, field);
        }

        public ProximityQueryNode(IList<IQueryNode> clauses, string field, Type type, bool inorder)
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

        public Type ProximityTypeValue
        {
            get
            {
                return this.proximityType;
            }
        }

        public override string ToString()
        {
            String distanceSTR = ((this.distance == -1) ? ("")
                : (" distance='" + this.distance) + "'");

            if (Children == null || Children.Count == 0)
                return "<proximity field='" + this.field + "' inorder='" + this.inorder
                    + "' type='" + this.proximityType.ToString() + "'" + distanceSTR
                    + "/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<proximity field='" + this.field + "' inorder='" + this.inorder
                + "' type='" + this.proximityType.ToString() + "'" + distanceSTR + ">");
            foreach (IQueryNode child in Children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</proximity>");
            return sb.ToString();
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            String withinSTR = TypeToQueryString(this.proximityType)
                + ((this.distance == -1) ? ("") : (" " + this.distance))
                + ((this.inorder) ? (" INORDER") : (""));

            StringBuilder sb = new StringBuilder();
            if (Children == null || Children.Count == 0)
            {
                // no children case
            }
            else
            {
                String filler = "";
                foreach (IQueryNode child in Children)
                {
                    sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
                    filler = " ";
                }
            }

            if (IsDefaultField(this.field))
            {
                return new StringCharSequenceWrapper("( " + sb.ToString() + " ) " + withinSTR);
            }
            else
            {
                return new StringCharSequenceWrapper(this.field + ":(( " + sb.ToString() + " ) " + withinSTR + ")");
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

        public int Distance
        {
            get { return this.distance; }
        }

        public string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }

        public string FieldAsString
        {
            get { return this.field; }
        }

        public bool IsInOrder
        {
            get { return this.inorder; }
        }
    }
}
