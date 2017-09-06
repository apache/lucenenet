using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.QueryParsers.Flexible.Core
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
    /// This should be thrown when an exception happens during the query parsing from
    /// string to the query node tree.
    /// </summary>
    /// <seealso cref="QueryNodeException"/>
    /// <seealso cref="Parser.ISyntaxParser"/>
    /// <seealso cref="Nodes.IQueryNode"/>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class QueryNodeParseException : QueryNodeException
    {
        private string query;

        private int beginColumn = -1;

        private int beginLine = -1;

        private string errorToken = "";

        public QueryNodeParseException(IMessage message)
            : base(message)
        {
        }

        public QueryNodeParseException(Exception throwable)
            : base(throwable)
        {
        }

        public QueryNodeParseException(IMessage message, Exception throwable)
            : base(message, throwable)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        // For testing
        public QueryNodeParseException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public QueryNodeParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        public virtual void SetQuery(string query)
        {
            this.query = query;
            this.m_message = new Message(
                QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query, "");
        }

        public virtual string Query
        {
            get { return this.query; }
        }

        /// <summary>
        /// The errorToken in the query
        /// </summary>
        public virtual string ErrorToken
        {
            get { return this.errorToken; }
            protected set { this.errorToken = value; }
        }

        public virtual void SetNonLocalizedMessage(IMessage message)
        {
            this.m_message = message;
        }

        /// <summary>
        /// For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
        /// string is returned. For the case where the parser is not able to figure out
        /// the line and column number -1 will be returned.
        /// Returns line where the problem was found.
        /// </summary>
        public virtual int BeginLine
        {
            get { return this.beginLine; }
            protected set { this.beginLine = value; }
        }

        /// <summary>
        /// For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
        /// string is returned. For the case where the parser is not able to figure out
        /// the line and column number -1 will be returned. 
        /// Returns column of the first char where the problem was found.
        /// </summary>
        public virtual int BeginColumn
        {
            get { return this.beginColumn; }
            protected set { this.beginColumn = value; }
        }
    }
}
