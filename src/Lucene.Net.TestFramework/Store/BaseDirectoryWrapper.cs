using Lucene.Net.Index;
using Lucene.Net.Util;

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
        private volatile bool isOpen = true; // LUCENENET specific - private because volatile is not CLS compliant, but made protected setter

        public BaseDirectoryWrapper(Directory @delegate)
            : base(@delegate)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                isOpen = false;
                if (checkIndexOnClose && DirectoryReader.IndexExists(this))
                {
                    TestUtil.CheckIndex(this, crossCheckTermVectorsOnClose);
                }
                base.Dispose(disposing);
            }
        }

        public virtual bool IsOpen
        {
            get => isOpen;
            protected set => isOpen = value; // LUCENENET specific - added protected setter and marked isOpen private because volatile is not CLS compliant
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