// lucene version compatibility level: 4.8.1
using System;
using System.IO;
using System.Security;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Cn.Smart
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
    /// Manages analysis data configuration for <see cref="SmartChineseAnalyzer"/>
    /// <para/>
    /// <see cref="SmartChineseAnalyzer"/> has a built-in dictionary and stopword list out-of-box.
    /// <para/>
    /// NOTE: To use an alternate dicationary than the built-in one, put the "bigramdict.dct" and
    /// "coredict.dct" files in a subdirectory of your application named "analysis-data". This subdirectory
    /// can be placed in any directory up to and including the root directory (if the OS permission allows).
    /// To place the files in an alternate location, set an environment variable named "analysis.data.dir"
    /// with the name of the directory the "bigramdict.dct" and "coredict.dct" files can be located within.
    /// <para/>
    /// The default "bigramdict.dct" and "coredict.dct" files can be found at: 
    /// <a href="https://issues.apache.org/jira/browse/LUCENE-1629">https://issues.apache.org/jira/browse/LUCENE-1629</a>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class AnalyzerProfile
    {
        /// <summary>
        /// Global indicating the configured analysis data directory
        /// </summary>
        public static string ANALYSIS_DATA_DIR = "";

        static AnalyzerProfile()
        {
            Init();
        }

        // LUCENENET specific - changed the logic here to leave the 
        // ANALYSIS_DATA_DIR an empty string if it is not found. This
        // allows us to skip loading files from disk if there are no files
        // to load (and fixes LUCENE-1817 that prevents the on-disk files
        // from ever being loaded).
        private static void Init()
        {
#if NETSTANDARD
            // Support for GB2312 encoding. See: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
            var encodingProvider = System.Text.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(encodingProvider);
#endif

            string dirName = "analysis-data";
            //string propName = "analysis.properties";

            // Try the system property：-Danalysis.data.dir=/path/to/analysis-data
            //ANALYSIS_DATA_DIR = System.getProperty("analysis.data.dir", "");
            ANALYSIS_DATA_DIR = SystemProperties.GetProperty("analysis.data.dir", "");
            if (ANALYSIS_DATA_DIR.Length != 0)
                return;

#if NETSTANDARD1_5
            string currentPath = System.AppContext.BaseDirectory;
#else
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
#endif

            //FileInfo[] cadidateFiles = new FileInfo[] { new FileInfo(currentPath + "/" + dirName),
            //    new FileInfo(currentPath + "/bin/" + dirName)/*, new FileInfo("./" + propName),
            //    new FileInfo("./lib/" + propName)*/ };
            //for (int i = 0; i < cadidateFiles.Length; i++)
            //{
            //    FileInfo file = cadidateFiles[i];
            //    if (file.Exists)
            //    {
            //        ANALYSIS_DATA_DIR = file.FullName;

            //        //if (file.isDirectory())
            //        //{
            //        //    ANALYSIS_DATA_DIR = file.getAbsolutePath();
            //        //}
            //        //else if (file.isFile() && GetAnalysisDataDir(file).Length != 0)
            //        //{
            //        //    ANALYSIS_DATA_DIR = GetAnalysisDataDir(file);
            //        //}
            //        break;
            //    }
            //}

            string candidatePath = System.IO.Path.Combine(currentPath, dirName);
            if (Directory.Exists(candidatePath))
            {
                ANALYSIS_DATA_DIR = candidatePath;
                return;
            }
            

            try
            {
                while (new DirectoryInfo(currentPath).Parent != null)
                {
                    candidatePath = System.IO.Path.Combine(new DirectoryInfo(currentPath).Parent.FullName, dirName);
                    if (Directory.Exists(candidatePath))
                    {
                        ANALYSIS_DATA_DIR = candidatePath;
                        return;
                    }
                    currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                }
            }
            catch (SecurityException)
            {
                // ignore security errors
            }


            //for (int i = 0; i < cadidateDirectories.Count; i++)
            //{
            //    DirectoryInfo dir = cadidateDirectories[i];
            //    if (dir.Exists)
            //    {
            //        ANALYSIS_DATA_DIR = dir.FullName;
            //        break;
            //    }
            //}

            //if (ANALYSIS_DATA_DIR.Length == 0)
            //{
            //    // Dictionary directory cannot be found.
            //    throw new Exception("WARNING: Can not find lexical dictionary directory!"
            //     + " This will cause unpredictable exceptions in your application!"
            //     + " Please refer to the manual to download the dictionaries.");
            //}

        }

        //private static string GetAnalysisDataDir(FileInfo propFile)
        //{
        //    Properties prop = new Properties();
        //    try
        //    {
        //        string dir;
        //        using (FileStream input = new FileStream(propFile.FullName, FileMode.Open, FileAccess.Read))
        //        {
        //            prop.load(new StreamReader(input, Encoding.UTF8));
        //            dir = prop.getProperty("analysis.data.dir", "");
        //        }
        //        return dir;
        //    }
        //    catch (IOException e)
        //    {
        //        return "";
        //    }
        //}
    }
}
