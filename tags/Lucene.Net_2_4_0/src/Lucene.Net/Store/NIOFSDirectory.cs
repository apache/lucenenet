/**
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements. See the NOTICE file distributed with this
 * work for additional information regarding copyright ownership. The ASF
 * licenses this file to You under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */


/**
 * NIO version of FSDirectory.  Uses FileChannel.read(ByteBuffer dst, long position) method
 * which allows multiple threads to read from the file without synchronizing.  FSDirectory
 * synchronizes in the FSIndexInput.readInternal method which can cause pileups when there
 * are many threads accessing the Directory concurrently.  
 *
 * This class only uses FileChannel when reading; writing
 * with an IndexOutput is inherited from FSDirectory.
 * 
 * Note: NIOFSDirectory is not recommended on Windows because of a bug
 * in how FileChannel.read is implemented in Sun's JRE.
 * Inside of the implementation the position is apparently
 * synchronized.  See here for details:

 * http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6265734 
 * 
 * @see FSDirectory
 */

public class NIOFSDirectory : Lucene.Net.Store.FSDirectory 
{
    public NIOFSDirectory()
    {
        throw new System.NotImplementedException("Waiting for volunteers to implement this class");
            
    }
}