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
    public class FacetsDistanceBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.MediumRun;

                for (int i = 0; i < BuildConfigurations.Configs.Count; i++)
                {
                    var config = BuildConfigurations.Configs[i];
                    if (string.IsNullOrEmpty(config.CustomConfigurationName))
                    {
                        AddJob(baseJob.WithNuGet("Lucene.Net.Analysis.Common", config.PackageVersion)
                                      .WithNuGet("Lucene.Net.Expressions", config.PackageVersion)
                                      .WithNuGet("Lucene.Net.Facet", config.PackageVersion)
                                      .WithId($"{i:000}-{config.Id}"));
                    }
                    else
                    {
                        AddJob(baseJob.WithCustomBuildConfiguration(config.CustomConfigurationName)
                                      .WithId($"{i:000}-{config.Id}"));
                    }
                }
            }
        }

        public static readonly DistanceFacetsExample example = new DistanceFacetsExample();

        [GlobalSetup]
        public void GlobalSetup() => example.Index();

        [GlobalCleanup]
        public void GlobalTearDown() => example.Dispose();

        [Benchmark]
        public void Search() => example.Search();

        [Benchmark]
        public void DrillDown() => example.DrillDown(DistanceFacetsExample.TWO_KM);
    }
}
