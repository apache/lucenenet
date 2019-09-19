using Xunit;
using Xunit.Sdk;

namespace Lucene.Net.TestFramework
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
    /// A skippable test.
    /// </summary>
    // LUCENENET: Grabbed example from https://github.com/xunit/samples.xunit/tree/5334ee9cf4a81f40dcb4cafabfeb098a555efb3d/DynamicSkipExample
    [XunitTestCaseDiscoverer("Lucene.Net.TestFramework.XunitExtensions.SkippableFactDiscoverer", "Lucene.Net.TestFramework.xUnit")]
    public class SkippableFactAttribute : FactAttribute
    {
    }
}
