using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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

    public class TestConfig : LuceneTestCase
    {
        [Test]
        public void TestAbsolutePathNamesWindows()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            props["work.dir1"] = "c:\\temp";
            props["work.dir2"] = "c:/temp";
            Config conf = new Config(props);
            assertEquals("c:\\temp", conf.Get("work.dir1", ""));
            assertEquals("c:/temp", conf.Get("work.dir2", ""));
        }
    }
}
