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

namespace Lucene.Net.Util
{
    using System;
    using System.Text.RegularExpressions;
    using Lucene.Net.Support;


    [System.CLSCompliant(false)]
    public class TestConstants : LuceneTestCase
    {

        private string VersionDetails
        {
            get
            {
                return " (LUCENE_MAIN_VERSION=" + Constants.LUCENE_MAIN_VERSION + ", LUCENE_MAIN_VERSION(without alpha/beta)=" + Constants.MainVersionWithoutAlphaBeta + ", LUCENE_VERSION=" + Constants.LUCENE_VERSION + ")";
            }
        }

        [Test]
        public void AssertValuesExist()
        {
            Ok(Constants.OS_NAME != null);
            Ok(Constants.OS_VERSION != null);
            Ok(Constants.OS_ARCH != null);
            Ok(Constants.LUCENE_VERSION != null);

#if!PORTABLE
            Console.WriteLine(" ");
            Console.WriteLine("Constants");
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("OS:              {0}", Constants.OS_NAME);
            Console.WriteLine("OS Version:      {0}", Constants.OS_VERSION);
            Console.WriteLine("OS Arch:         {0}", Constants.OS_ARCH);
            Console.WriteLine("Lucene Version:  {0}", Constants.LUCENE_VERSION);
            Console.WriteLine(" ");
#endif
        }

        [Test]
        public virtual void TestLuceneMainVersionConstant()
        {
            Ok(Regex.IsMatch(Constants.LUCENE_MAIN_VERSION, "\\d+\\.\\d+(|\\.0\\.\\d+)", RegexOptions.IgnoreCase), "LUCENE_MAIN_VERSION does not follow pattern: 'x.y' (stable release) or 'x.y.0.z' (alpha/beta version)" + VersionDetails);
            Ok(Constants.LUCENE_VERSION.StartsWith(Constants.MainVersionWithoutAlphaBeta), "LUCENE_VERSION does not start with LUCENE_MAIN_VERSION (without alpha/beta marker)" + VersionDetails);
        }

        [Test]
        public virtual void TestBuildSetup()
        {
            // common-build.xml sets lucene.version, if not, we skip this test!
            const string defaultVersion = "5.0";
            var version = SystemProps.Get("lucene.version", defaultVersion);
            Ok(version != null);

            if (version != "5.0")
            {
                

                // remove anything after a "-" from the version string:
                version = Regex.Replace(version, "-.*$", "");
                string versionConstant = Regex.Replace(Constants.LUCENE_VERSION, "-.*$", "");
                Ok(versionConstant.StartsWith(version) || version.StartsWith(versionConstant), "LUCENE_VERSION should share the same prefix with lucene.version test property ('" + version + "')." + VersionDetails);
            }
        
        }

    }

}