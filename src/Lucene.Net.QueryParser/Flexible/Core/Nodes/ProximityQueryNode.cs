using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A <see cref="ProximityQueryNode"/> represents a query where the terms should meet
    /// specific distance conditions. (a b c) WITHIN [SENTENCE|PARAGRAPH|NUMBER]
    /// [INORDER] ("a" "b" "c") WITHIN [SENTENCE|PARAGRAPH|NUMBER] [INORDER]
    /// 
    /// TODO: Add this to the future standard Lucene parser/processor/builder
    /// </summary>
    public class ProximityQueryNode : BooleanQueryNode
    {
        /// <summary>
        /// Distance condition: PARAGRAPH, SENTENCE, or NUMBER
        /// </summary>
        public enum Type
        {
            PARAGRAPH,
            SENTENCE,
            NUMBER
        }

        // LUCENENET NOTE: Moved ProximityType class outside of ProximityQueryNode class to
        // prevent a naming conflict with the ProximityType property.

        private ProximityQueryNode.Type proximityType = ProximityQueryNode.Type.SENTENCE;
        private int distance = -1;
        private readonly bool inorder = false; // LUCENENET: marked readonly
        private string field = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clauses">QueryNode children</param>
        /// <param name="field">field name</param>
        /// <param name="type">type of proximity query</param>
        /// <param name="distance">positive integer that specifies the distance</param>
        /// <param name="inorder">true, if the tokens should be matched in the order of the clauses</param>
        public ProximityQueryNode(IList<IQueryNode> clauses, string field,
            ProximityQueryNode.Type type, int distance, bool inorder)
            : base(clauses)
        {
            IsLeaf = false;
            this.proximityType = type;
            this.inorder = inorder;
            this.field = field;
            if (type == ProximityQueryNode.Type.NUMBER)
            {
                if (distance <= 0)
                {
                    // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                    // LUCENENET: Added paramName parameter and changed the error message.
                    // However, we still need this to be an error type so it is not caught in StandardSyntaxParser.
                    throw new QueryNodeError(QueryParserMessages.NUMBER_CANNOT_BE_NEGATIVE, nameof(distance));
                }
                else
                {
                    this.distance = distance;
                }
            }
            ClearFields(clauses, field);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clauses">QueryNode children</param>
        /// <param name="field">field name</param>
        /// <param name="type">type of proximity query</param>
        /// <param name="inorder">true, if the tokens should be matched in the order of the clauses</param>
        public ProximityQueryNode(IList<IQueryNode> clauses, string field,
            ProximityQueryNode.Type type, bool inorder)
            : this(clauses, field, type, -1, inorder)
        {
        }

        private static void ClearFields(IList<IQueryNode> nodes, string field)
        {
            if (nodes is null || nodes.Count == 0)
                return;

            foreach (IQueryNode clause in nodes)
            {
                if (clause is FieldQueryNode fieldQueryNode)
                {
                    fieldQueryNode.m_toQueryStringIgnoreFields = true;
                    fieldQueryNode.Field = field;
                }
            }
        }

        public virtual ProximityQueryNode.Type ProximityType => this.proximityType;

        public override string ToString()
        {
            string distanceSTR = ((this.distance == -1) ? ("")
                : (" distance='" + this.distance) + "'");

            var children = GetChildren();
            if (children is null || children.Count == 0)
                return "<proximity field='" + this.field + "' inorder='" + this.inorder
                    + "' type='" + this.proximityType.ToString() + "'" + distanceSTR
                    + "/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<proximity field='" + this.field + "' inorder='" + this.inorder
                + "' type='" + this.proximityType.ToString() + "'" + distanceSTR + ">");
            foreach (IQueryNode child in children)
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
            var children = GetChildren();
            if (children is null || children.Count == 0)
            {
                // no children case
            }
            else
            {
                string filler = "";
                foreach (IQueryNode child in children)
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

        /// <summary>
        /// Gets the distance
        /// </summary>
        public virtual int Distance => this.distance;

        /// <summary>
        /// Gets or Sets the field. Returns null if the field was not specified in the query string.
        /// </summary>
        public virtual string Field
        {
            get => this.field;
            set => this.field = value;
        }

        // LUCENENET specific: This method is technically not required because Field is already a string property, not ICharSequence
        /// <summary>
        /// Gets the field as a string. Returns null if the field was not specified in the query string.
        /// </summary>
        /// <returns></returns>
        public virtual string GetFieldAsString()
        {
            if (this.field is null)
                return null;
            else
                return this.field.ToString();
        }

        /// <summary>
        /// terms must be matched in the specified order
        /// </summary>
        public virtual bool IsInOrder => this.inorder;
    }

    /// <summary>
    /// utility class containing the distance condition and number
    /// </summary>
    public class ProximityType
    {
        internal int pDistance = 0;

#pragma warning disable IDE0052 // Assigned never read
        internal ProximityQueryNode.Type pType/* = null*/; // LUCENENET: Not nullable
#pragma warning restore IDE0052 // Assigned never read

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
