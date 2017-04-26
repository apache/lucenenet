/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using NUnit.Framework;

namespace Lucene.Net.Attributes
{
    /// <summary>
    /// Specifies that this test sometimes runs for a long time and may not end.
    /// For running tests in .NET Core because NUnit does not support [Timeout],
    /// so we can have tests in the .NET Core build that run forever.
    /// </summary>
    public class HasTimeoutAttribute : CategoryAttribute
    {
        public HasTimeoutAttribute() : base("HasTimeout")
        {
            // nothing to do here but invoke the base contsructor.
        }
    }
}
