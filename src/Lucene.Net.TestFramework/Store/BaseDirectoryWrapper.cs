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

    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Calls check index on close.
    /// </summary>
    // do NOT make any methods in this class synchronized, volatile
    // do NOT import anything from the concurrency package.
    // no randoms, no nothing.
    public class BaseDirectoryWrapper : FilterDirectory
    {
        private bool checkIndexOnClose = true;
        private bool crossCheckTermVectorsOnClose = true;
        protected internal volatile bool m_isOpen = true;

        public BaseDirectoryWrapper(Directory @delegate)
            : base(@delegate)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_isOpen = false;
                if (checkIndexOnClose && DirectoryReader.IndexExists(this))
                {
                    TestUtil.CheckIndex(this, crossCheckTermVectorsOnClose);
                }
                base.Dispose(disposing);
            }
        }

        public virtual bool Open // LUCENENET TODO: API - rename IsOpen
        {
            get
            {
                return m_isOpen;
            }
        }

        /// <summary>
        /// Set whether or not checkindex should be run
        /// on close
        /// </summary>
        public virtual bool CheckIndexOnClose  // LUCENENET TODO: API - rename CheckIndexOnDispose
        {
            set
            {
                this.checkIndexOnClose = value;
            }
            get
            {
                return checkIndexOnClose;
            }
        }

        public virtual bool CrossCheckTermVectorsOnClose  // LUCENENET TODO: API - rename CrossCheckTermVectorsOnDispose
        {
            set
            {
                this.crossCheckTermVectorsOnClose = value;
            }
            get
            {
                return crossCheckTermVectorsOnClose;
            }
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