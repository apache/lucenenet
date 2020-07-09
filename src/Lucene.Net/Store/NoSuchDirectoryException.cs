// LUCENENET specific - excluding this class in favor of DirectoryNotFoundException,
// although that means we need to catch DirectoryNotFoundException everywhere that 
// FileNotFoundException is being caught (because it is a superclass) to be sure we have the same behavior.

//using System;
//using System.IO;

//namespace Lucene.Net.Store
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// This exception is thrown when you try to list a
//    /// non-existent directory.
//    /// </summary>
//    // LUCENENET: It is no longer good practice to use binary serialization. 
//    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
//#if FEATURE_SERIALIZABLE_EXCEPTIONS
//    [Serializable]
//#endif
//    public class NoSuchDirectoryException : FileNotFoundException
//    {
//        public NoSuchDirectoryException(string message)
//            : base(message)
//        {
//        }
//    }
//}