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

using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{

    /**
     * Calls check index on close.
     */
    // do NOT make any methods in this class synchronized, volatile
    // do NOT import anything from the concurrency package.
    // no randoms, no nothing.
    public class BaseDirectoryWrapper : Directory
    {
        protected readonly Directory @delegate;


        public BaseDirectoryWrapper(Directory @delegate)
        {
            this.@delegate = @delegate;
            this.CheckIndexOnClose = false;
            this.CrossCheckTermVectorysOnClose = false;
        }

        public bool CheckIndexOnClose { get; private set; }
        public bool CrossCheckTermVectorysOnClose {get; private set;}

        public bool IsOpenFlag
        {
            get { return this.isOpen; }
        }

        public override LockFactory LockFactory
        {
	       get{ return this.@delegate.LockFactory;}
           set{this.@delegate.LockFactory = value;}
        }        public override string LockId
        {
	        get 
	        { 
		         return this.@delegate.LockId;
	        }
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
 	         this.@delegate.Copy(to, src, dest, context);
        }

        public override void ClearLock(string name)
        {
 	         this.@delegate.ClearLock(name);
        }

        public override Directory.IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
 	
             return this.@delegate.CreateSlicer(name, context);
        }

        public override string[] ListAll()
        {
            
            return this.@delegate.ListAll();
        }


        public override Lock MakeLock(string name)
        {
 	         return this.@delegate.MakeLock(name);
        }

        public override bool FileExists(string name)
        {
           return this.@delegate.FileExists(name);
        }

        public override void DeleteFile(string name)
        {
            this.@delegate.DeleteFile(name);
        }

        public override long FileLength(string name)
        {
            return this.@delegate.FileLength(name);
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            return this.@delegate.CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
           this.@delegate.Sync(names);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
           return this.@delegate.OpenInput(name, context); 
        }

        public override string ToString()
        {
 	         return string.Format("BaseDirectoryWrapper({0})", this.@delegate.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            this.isOpen = false;
            if(this.CheckIndexOnClose && DirectoryReader.IndexExists(this))
            {
                _TestUtil.CheckIndex(this, this.CrossCheckTermVectorysOnClose);
            }
            this.@delegate.Dispose();
        }
    }
}
