/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;

namespace Lucene.Net.Index
{

    /// <summary>An IndexReader which reads multiple indexes, appending 
    /// their content.
    /// </summary>
    public class MultiReader : BaseCompositeReader<IndexReader>
    {
        private readonly bool closeSubReaders;

        /// <summary> <p/>Construct a MultiReader aggregating the named set of (sub)readers.
        /// Directory locking for delete, undeleteAll, and setNorm operations is
        /// left to the subreaders. <p/>
        /// <p/>Note that all subreaders are closed if this Multireader is closed.<p/>
        /// </summary>
        /// <param name="subReaders">set of (sub)readers
        /// </param>
        /// <throws>  IOException </throws>
        public MultiReader(params IndexReader[] subReaders)
            : this(subReaders, true)
        {
        }

        /// <summary> <p/>Construct a MultiReader aggregating the named set of (sub)readers.
        /// Directory locking for delete, undeleteAll, and setNorm operations is
        /// left to the subreaders. <p/>
        /// </summary>
        /// <param name="closeSubReaders">indicates whether the subreaders should be closed
        /// when this MultiReader is closed
        /// </param>
        /// <param name="subReaders">set of (sub)readers
        /// </param>
        /// <throws>  IOException </throws>
        public MultiReader(IndexReader[] subReaders, bool closeSubReaders)
            : base((IndexReader[])subReaders.Clone())
        {
            this.closeSubReaders = closeSubReaders;
            if (!closeSubReaders)
            {
                for (int i = 0; i < subReaders.Length; i++)
                {
                    subReaders[i].IncRef();
                }
            }
        }

        protected override void DoClose()
        {
            System.IO.IOException ioe = null;
            foreach (IndexReader r in GetSequentialSubReaders())
            {
                try
                {
                    if (closeSubReaders)
                    {
                        r.Dispose();
                    }
                    else
                    {
                        r.DecRef();
                    }
                }
                catch (System.IO.IOException e)
                {
                    if (ioe == null) ioe = e;
                }
            }
            // throw the first exception
            if (ioe != null) throw ioe;
        }
    }
}