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
        private ThreadLocal<object> threadLocalTokenStream;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Analyzer"/> class.
        /// </summary>
        protected Analyzer()
        {
            if (this.AssertSealed() == false)
                throw new TypeLoadException(
                    "{0} can not be used. It must be sealed or at least seal" +
                    " the TokenStream and ResuableTokenStream methods"
                    .Inject(this.GetType().Name));
        }


        /// <summary>
        /// Gets the offset gap.
        /// </summary>
        /// <value>An instance of <see cref="Int32"/>.</value>
        public virtual int OffsetGap
        {
            get { return 0; }
        }


        /// <summary>
        /// Gets or sets the previous token stream or token stream storage object.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This can be used to store the previous token stream directly or it can 
        ///     use a custom storage mechanism like <see cref="ReusableAnalyzerBase.TokenStreamComponents"/>.
        ///     </para>
        ///     <para>
        ///     The property name deviates from the Java version because the name is misleading. 
        ///     </para>
        /// </remarks>
        /// <value>The previous token stream. Returns null if the value has not been set.</value>
        /// <exception cref="ObjectDisposedException">
        ///     Thrown when <see cref="Analyzer"/> is already disposed.
        /// </exception>
        protected object PreviousTokenStreamOrStorage
        {
            get
            {
                if (this.disposed)
                    throw new ObjectDisposedException(this.GetType().FullName);

                if (!this.threadLocalTokenStream.IsValueCreated)
                    return null;
                return this.threadLocalTokenStream.Value;
            }

            set
            {
                if (this.disposed)
                    throw new ObjectDisposedException(this.GetType().FullName);

                this.threadLocalTokenStream.Value = value;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
        /// Creates a <see cref="TokenStream"/> using the specified <see cref="StreamReader"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     Subclasses that implement this method should always be able to handle null 
        ///     values for the field name for backwards compatibility.
        ///     </para>
        /// </remarks>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="TokenStream"/>.
        /// </returns>
        public abstract TokenStream TokenStream(string fieldName, StreamReader reader);

        /// <summary>
        /// Finds or creates a <see cref="TokenStream"/> that is permits the <see cref="TokenStream"/>
        /// to be re-used on the same thread.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     Any Class that manages the current <see cref="Analyzer"/> and does not need to use more
        ///     than one <see cref="TokenStream"/> at the same time should use this method for 
        ///     better performance. 
        ///     </para>
        /// </remarks>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="TokenStream"/>.
        /// </returns>
        public virtual TokenStream ReusableTokenStream(string fieldName, StreamReader reader)
        {
            return this.TokenStream(fieldName, reader);
        }


        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.disposed = true;
                this.threadLocalTokenStream.Dispose();
                this.threadLocalTokenStream = null;
            }
        }

        // AssertSealed was ported from java-lucene-core, but I'm not convinced 
        // this is best way to handle this kind of design decision in .NET.

        // It might be better to create an analysis tool or put these kind of
        // assertions inside the testing framework.
        // TODO: remove AssertSealed() / assertSealed.
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