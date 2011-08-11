// -----------------------------------------------------------------------
// <copyright company="Apache" file="Analyzer.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Analysis
{
    using System;
    using System.IO;
    using System.Reflection;
    using Lucene.Net.Support;
    using Lucene.Net.Support.Threading;

    /// <summary>
    /// TODO: update
    /// </summary>
    public abstract class Analyzer : IDisposable
    {
        private ThreadLocal<TokenStream> threadLocalTokenStream;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Analyzer"/> class.
        /// </summary>
        protected Analyzer()
        {
            if (this.AssertSealed() == false)
                throw new TypeLoadException(
                    string.Format(
                         "{0} can not be used. It must be sealed or at least seal" +
                         " the TokenStream and ResuableTokenStream methods",
                         this.GetType().Name));
        }


        /// <summary>
        /// Gets or sets the previous token stream.
        /// </summary>
        /// <value>The previous token stream. Returns null if the value has not been set.</value>
        /// <exception cref="ObjectDisposedException">
        ///     Thrown when <see cref="Analyzer"/> is already disposed.
        /// </exception>
        protected TokenStream PreviousTokenStream
        {
            get
            {
                if (this.disposed)
                    throw new ObjectDisposedException(
                        string.Format(
                             "This analyzer '{0}' has already been disposed",
                             this.GetType().FullName));

                if (!this.threadLocalTokenStream.IsValueCreated)
                    return null;
                return this.threadLocalTokenStream.Value;
            }

            set
            {
                if (this.disposed)
                    throw new ObjectDisposedException(
                        string.Format(
                             "This analyzer '{0}' has already been disposed",
                             this.GetType().FullName));

                this.threadLocalTokenStream.Value = value;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            this.disposed = true;
            this.threadLocalTokenStream.Dispose();
            this.threadLocalTokenStream = null;
        }


        /// <summary>
        /// Gets the offset gap.
        /// </summary>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public virtual int GetOffsetGap()
        {
            return 0;
        }

        /// <summary>
        /// Gets the position increment gap.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public virtual int GetPositionIncrementGap(string fieldName)
        {
            return 0;
        }

        /// <summary>
        /// Tokens the stream.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="TokenStream"/>.
        /// </returns>
        public abstract TokenStream TokenStream(string fieldName, TextReader reader);

        /// <summary>
        /// Reusable the token stream.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="TokenStream"/>.
        /// </returns>
        public TokenStream ReusableTokenStream(string fieldName, TextReader reader)
        {
            return this.TokenStream(fieldName, reader);
        }


        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        // this was ported from lucene, but I'm not convinced this is best way
        // to handle this kind of design decision.
        private bool AssertSealed()
        {
            Type type = this.GetType();

            if (type.IsSealed || type.IsAbstract)
                return true;

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            MethodInfo tokenStreamMethod = type.GetMethod("TokenStream", flags);
            MethodInfo reuseableTokenStreamMethod = type.GetMethod("ReusableTokenStreamMethod", flags);


            if (tokenStreamMethod != null && !tokenStreamMethod.IsFinal)
                return false;

            return reuseableTokenStreamMethod == null || reuseableTokenStreamMethod.IsFinal;
        }
    }
}