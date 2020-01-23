using Lucene.Net.Support;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// A delegating <see cref="Directory"/> that records which files were
    /// written to and deleted.
    /// </summary>
    public sealed class TrackingDirectoryWrapper : FilterDirectory
    {
        private readonly ISet<string> createdFileNames = new JCG.HashSet<string>().AsConcurrent();

        public TrackingDirectoryWrapper(Directory @in)
            : base(@in)
        {
        }

        public override void DeleteFile(string name)
        {
            createdFileNames.Remove(name);
            m_input.DeleteFile(name);
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            createdFileNames.Add(name);
            return m_input.CreateOutput(name, context);
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            createdFileNames.Add(dest);
            m_input.Copy(to, src, dest, context);
        }

        public override Directory.IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            return m_input.CreateSlicer(name, context);
        }

        // maybe clone before returning.... all callers are
        // cloning anyway....
        public ISet<string> CreatedFiles => createdFileNames;
    }
}