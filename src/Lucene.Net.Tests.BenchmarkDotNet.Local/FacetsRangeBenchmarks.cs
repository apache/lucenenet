using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Lucene.Net.Demo.Facet;

namespace Lucene.Net.Tests.BenchmarkDotNet
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

    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class FacetsRangeBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun);
            }
        }

        public static readonly RangeFacetsExample example = new RangeFacetsExample();

        [GlobalCleanup]
        public void GlobalTearDown() => example.Dispose();

        [Benchmark]
        public void Search()
        {
            example.Index();
            example.Search();
        }

        [Benchmark]
        public void DrillDown()
        {
            example.Index();
            example.DrillDown(example.PAST_SIX_HOURS);
        }
    }
}
