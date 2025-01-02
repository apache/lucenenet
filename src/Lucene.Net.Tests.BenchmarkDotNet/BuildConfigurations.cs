using System.Collections.Generic;

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

    public static class BuildConfigurations
    {
        public static IList<BuildConfiguration> Configs = new List<BuildConfiguration>
        {
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00005" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00006" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00007" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00008" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00009" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00010" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00011" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00012" },
            //new BuildConfiguration { PackageVersion = "4.8.0-beta00013" },
            new BuildConfiguration { PackageVersion = "4.8.0-beta00014" },
            new BuildConfiguration { PackageVersion = "4.8.0-beta00015" },
            new BuildConfiguration { PackageVersion = "4.8.0-beta00016" },
            new BuildConfiguration { PackageVersion = "4.8.0-beta00017" },
            //new BuildConfiguration { CustomConfigurationName = "LocalBuild", Id = "LocalBuild" }, // NOTE: This functions, but for some reason is less performant than testing a NuGet package
        };
    }

    public class BuildConfiguration
    {
        private string id;

        /// <summary>
        /// NuGet package version. May be on a NuGet feed or a local directory configured as a feed.
        /// </summary>
        public string PackageVersion { get; set; }

        public string CustomConfigurationName { get; set; }

        public string Id
        {
            get
            {
                if (id is null)
                    return PackageVersion;
                return id;
            }
            set => id = value;
        }
    }
}
