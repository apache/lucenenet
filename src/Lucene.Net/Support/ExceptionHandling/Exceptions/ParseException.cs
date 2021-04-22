using System;
using System.Runtime.Serialization;

namespace Lucene
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
    /// Signals that an error has been reached unexpectedly while parsing.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// use the <see cref="ExceptionExtensions.IsParseException(Exception)"/> method
    /// everywhere except for in QueryParser.
    /// <code>
    /// catch (Exception ex) when (ex.IsParseException())
    /// </code>
    /// <para/>
    /// IMPORTANT: QueryParser has its own ParseException types (there are multiple),
    /// so be sure not to use this exception instead of the ones in QueryParser.
    /// For QueryParser exceptions, there are no extension methods to use for identification
    /// in catch blocks, you should instead use the fully-qualified name of the exception.
    /// <code>
    /// catch (Lucene.Net.QueryParsers.Surround.Parser.ParseException e)
    /// </code>
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class ParseException : FormatException
    {
        [Obsolete("Use ParseException.Create() instead.", error: true)]
        public ParseException(string message, int errorOffset) : base(string.Concat(message, ", ErrorOffset = ", errorOffset.ToString()))
        {
            ErrorOffset = errorOffset;
        }

        [Obsolete("Use ParseException.Create() instead.", error: true)]
        public ParseException(string message, int errorOffset, Exception innerException) : base(string.Concat(message, ", ErrorOffset = ", errorOffset.ToString()), innerException)
        {
            ErrorOffset = errorOffset;
        }

        // LUCENENET: For testing purposes
        [Obsolete("Use ParseException.Create() instead.", error: true)]
        internal ParseException()
        {
        }

        // LUCENENET: For testing purposes
        [Obsolete("Use ParseException.Create() instead.", error: true)]
        internal ParseException(string message) : base(message)
        {
        }

        // LUCENENET: For testing purposes
        [Obsolete("Use ParseException.Create() instead.", error: true)]
        internal ParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        /// <summary>
        /// Returns the position where the error was found.
        /// </summary>
        public int ErrorOffset { get; private set; }


        // Static factory methods

        public static Exception Create(string message, int errorOffset) => new FormatException(string.Concat(message, ", ErrorOffset = ", errorOffset.ToString()));

        public static Exception Create(string message, int errorOffset, Exception innerException) => new FormatException(string.Concat(message, ", ErrorOffset = ", errorOffset.ToString()), innerException);

        public static Exception Create() => new FormatException();

        public static Exception Create(string message) => new FormatException(message);

        public static Exception Create(string message, Exception innerException) => new FormatException(message, innerException);

        public static Exception Create(Exception cause) => new FormatException(cause.Message, cause);
    }
}
