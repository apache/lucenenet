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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net
{
    /// <summary>
    /// The 
    /// </summary>
    internal static class Categories
    {
        /// <summary>
        /// Tests that validate small units of code are functioning 
        /// as intended.  The unit tests should always pass.
        /// </summary>
        public const string Unit = "Unit";

        /// <summary>
        /// Tests that are integration style tests. These tend to
        /// test more than small units of code or other external dependencies.  
        /// Integration tests should let you know if something is not set up 
        /// correctly.
        /// </summary>
        public const string Integration = "Integration";

        /// <summary>
        /// Tests for validating how well Lucene.Net is performing. 
        /// </summary>
        public const string Performance = "Performance";

        /// <summary>
        /// Tests that validate the integrity of dependencies like
        /// file paths, test data, that are required for other tests
        /// to work correctly. 
        /// </summary>
        public const string Infrastructure = "Infrastructure";
    }
}
