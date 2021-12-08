using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif
#if FEATURE_CODE_ACCESS_SECURITY
using System.Security.Permissions;
#endif
#nullable enable

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
    // LUCENENET specific: Refactored constructors to be more like a .NET type and eliminated IMessage/NLS support.
    public class QueryNodeParseException : QueryNodeException
    {
#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [NonSerialized]
#endif
        private string? query;

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [NonSerialized]
#endif
        private int beginColumn = -1;

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [NonSerialized]
#endif
        private int beginLine = -1;

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [NonSerialized]
#endif
        private string errorToken = "";

        public QueryNodeParseException(string? message)
            : base(message)
        {
        }

        public QueryNodeParseException(Exception? cause)
            : base(cause)
        {
        }

        public QueryNodeParseException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected QueryNodeParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            query = info.GetString("query");
            beginColumn = info.GetInt32("beginColumn");
            beginLine = info.GetInt32("beginLine");
            errorToken = info.GetString("errorToken")!;
        }

#if FEATURE_CODE_ACCESS_SECURITY
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("query", query, typeof(string));
            info.AddValue("beginColumn", beginColumn, typeof(int));
            info.AddValue("beginLine", beginLine, typeof(int));
            info.AddValue("errorToken", errorToken, typeof(string));
        }
#endif

        public virtual string? Query
        {
            get => this.query;
            set => this.query = value; // LUCENENET specific - set the message only in the constructor
        }

        /// <summary>
        /// The errorToken in the query
        /// </summary>
        public virtual string ErrorToken
        {
            get => this.errorToken;
            protected set => this.errorToken = value ?? throw new ArgumentNullException(nameof(ErrorToken)); // LUCENENET specific - added null guard clause
        }

        // LUCENENET specific - removed SetNonLocalizedMessage() because we only set the message in the constructor

        /// <summary>
        /// For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
        /// string is returned. For the case where the parser is not able to figure out
        /// the line and column number -1 will be returned.
        /// Returns line where the problem was found.
        /// </summary>
        public virtual int BeginLine
        {
            get => this.beginLine;
            protected set => this.beginLine = value;
        }

        /// <summary>
        /// For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
        /// string is returned. For the case where the parser is not able to figure out
        /// the line and column number -1 will be returned. 
        /// Returns column of the first char where the problem was found.
        /// </summary>
        public virtual int BeginColumn
        {
            get => this.beginColumn;
            protected set => this.beginColumn = value;
        }
    }
}
