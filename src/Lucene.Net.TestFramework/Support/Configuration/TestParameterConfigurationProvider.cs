
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Configuration
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
    public class TestParameterConfigurationProvider : IConfigurationProvider
    {
        private CommandLineConfigurationProvider _instance;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="testParameters">The Test Parameter args.</param>
        public TestParameterConfigurationProvider(TestParameters testParameters)
        {
            List<string> args1 = new List<string>();
            foreach (string x in testParameters.Names)
            {
                args1.Add(string.Format("{0}={1}", x, testParameters[x]));
            }

            _instance = new CommandLineConfigurationProvider(args1, null);
        }

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            return _instance.GetChildKeys(earlierKeys, parentPath);
        }
        [CLSCompliant(false)]
        public IChangeToken GetReloadToken()
        {
            return _instance.GetReloadToken();
        }

        public void Load()
        {
            _instance.Load();
        }

        public void Set(string key, string value)
        {
            _instance.Set(key, value);
        }

        public bool TryGet(string key, out string value)
        {
            return _instance.TryGet(key, out value);
        }
    }
}
