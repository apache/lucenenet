using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Threading;

namespace Lucene.Net.Store
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
    /// Calls check index on dispose.
    /// </summary>
    // do NOT make any methods in this class synchronized, volatile
    // do NOT import anything from the concurrency package.
    // no randoms, no nothing.
    public class BaseDirectoryWrapper : FilterDirectory
    {
        private bool checkIndexOnClose = true;
        private bool crossCheckTermVectorsOnClose = true;

        // LUCENENET specific - setup to make it safe to call dispose multiple times
        private const int True = 1;
        private const int False = 0;

        // LUCENENET specific - using Interlocked intead of a volatile field for IsOpen.
        private int isOpen = True; // LUCENENET: Changed from bool to int so we can use Interlocked.

        /// <summary>
        /// Atomically sets the value to the given updated value
        /// if the current value <c>==</c> the expected value.
        /// <para/>
        /// Expert: Use this in the <see cref="Directory.Dispose(bool)"/> call to skip
        /// duplicate calls by using the folling if block to guard the
        /// dispose logic.
        /// <code>
        /// protected override void Dispose(bool disposing)
        /// {
        ///     if (!CompareAndSetIsOpen(expect: true, update: false)) return;
        /// 
        ///     // Dispose unmanaged resources
        ///     if (disposing)
        ///     {
        ///         // Dispose managed resources
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <param name="expect">The expected value (the comparand).</param>
        /// <param name="update">The new value.</param>
        /// <returns><c>true</c> if successful. A <c>false</c> return value indicates that
        /// the actual value was not equal to the expected value.</returns>
        // LUCENENET specific - setup to make it safe to call dispose multiple times
        protected internal bool CompareAndSetIsOpen(bool expect, bool update)
        {
            int e = expect ? True : False;
            int u = update ? True : False;

            int original = Interlocked.CompareExchange(ref isOpen, u, e);

            return original == e;
        }

        public BaseDirectoryWrapper(Directory @delegate)
            : base(@delegate)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!CompareAndSetIsOpen(expect: true, update: false)) return; // LUCENENET: allow dispose more than once as per https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern

            if (disposing)
            {
                // LUCENENET: Removed setter for isOpen and put it above in the if check so it is atomic
                if (checkIndexOnClose && DirectoryReader.IndexExists(this))
                {
                    TestUtil.CheckIndex(this, crossCheckTermVectorsOnClose);
                }
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Directory"/> instance is open.
        /// <para/>
        /// Expert: This is useful for implementing the <see cref="Directory.EnsureOpen()"/> logic.
        /// </summary>
        public virtual bool IsOpen
        {
            get => Interlocked.CompareExchange(ref isOpen, False, False) == True ? true : false;
            protected set => Interlocked.Exchange(ref this.isOpen, value ? True : False); // LUCENENET specific - added protected setter
        }

        /// <summary>
        /// Set whether or not checkindex should be run
        /// on dispose
        /// </summary>
        public virtual bool CheckIndexOnDispose  // LUCENENET specific - renamed from CheckIndexOnClose
        {
            get => checkIndexOnClose;
            set => checkIndexOnClose = value;
        }

        public virtual bool CrossCheckTermVectorsOnDispose  // LUCENENET specific - renamed from CrossCheckTermVectorsOnClose
        {
            get => crossCheckTermVectorsOnClose;
            set => crossCheckTermVectorsOnClose = value;
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            m_input.Copy(to, src, dest, context);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            return m_input.CreateSlicer(name, context);
        }
    }
}