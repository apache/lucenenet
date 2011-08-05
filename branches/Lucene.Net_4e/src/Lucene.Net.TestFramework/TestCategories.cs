// -----------------------------------------------------------------------
// <copyright company="Apache" file="TestCategories.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The categories for test fixtures. 
    /// </summary>
    public class TestCategories
    {
        /// <summary>
        ///  Tests that only validate the functionality of a unit of code. These
        ///  should generally run fast and never depend on other tests functionality
        ///  or running in any particular order.
        /// </summary>
        public const string Unit = "Unit";

        /// <summary>
        ///  Tests that validate integration against remote or internal dependencies.
        ///  These tests will generally require external resources like files or data 
        ///  bases.
        /// </summary>
        public const string Integration = "Integration";

        /// <summary>
        /// Tests that validate specific overall stories or functionality that the
        /// libraries or applications must perform. Think acceptance testing or 
        /// blackbox testing.  
        /// </summary>
        public const string Functional = "Functional";

        /// <summary>
        /// Tests that validate performance requirements.  
        /// </summary>
        public const string Performance = "Performance";

        /// <summary>
        /// Tests that are going to take a long period of time to execute. This
        /// is a meta category for excluding long running tests while trying
        /// to develop and run unit tests often.
        /// </summary>
        public const string LongRunning = "LongRunning";
    }
}
