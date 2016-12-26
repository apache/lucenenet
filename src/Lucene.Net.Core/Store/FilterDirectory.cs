using System;
using System.Collections.Generic;

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
    /// Directory implementation that delegates calls to another directory.
    ///  this class can be used to add limitations on top of an existing
    ///  <seealso cref="Directory"/> implementation such as
    ///  <seealso cref="RateLimitedDirectoryWrapper rate limiting"/> or to add additional
    ///  sanity checks for tests. However, if you plan to write your own
    ///  <seealso cref="Directory"/> implementation, you should consider extending directly
    ///  <seealso cref="Directory"/> or <seealso cref="BaseDirectory"/> rather than try to reuse
    ///  functionality of existing <seealso cref="Directory"/>s by extending this class.
    ///  @lucene.internal
    /// </summary>
    public class FilterDirectory : Directory
    {
        protected internal readonly Directory @in; // LUCENENET TODO: Rename m_in

        /// <summary>
        /// Sole constructor, typically called from sub-classes. </summary>
        protected FilterDirectory(Directory @in)
        {
            this.@in = @in;
        }

        /// <summary>
        /// Return the wrapped <seealso cref="Directory"/>. </summary>
        public Directory Delegate
        {
            get
            {
                return @in;
            }
        }

        public override string[] ListAll()
        {
            return @in.ListAll();
        }

        public override bool FileExists(string name)
        {
            return @in.FileExists(name);
        }

        public override void DeleteFile(string name)
        {
            @in.DeleteFile(name);
        }

        public override long FileLength(string name)
        {
            return @in.FileLength(name);
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            return @in.CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            @in.Sync(names);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            return @in.OpenInput(name, context);
        }

        public override Lock MakeLock(string name)
        {
            return @in.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            @in.ClearLock(name);
        }

        public override void Dispose()
        {
            @in.Dispose();
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            @in.SetLockFactory(lockFactory);
        }

        public override LockFactory LockFactory
        {
            get
            {
                return @in.LockFactory;
            }
        }

        public override string GetLockID()
        {
            return @in.GetLockID();
        }

        public override string ToString()
        {
            return this.GetType().Name + "(" + @in.ToString() + ")";
        }
    }
}