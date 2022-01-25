using Lucene.Net.Attributes;
using Lucene.Net.Cli;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Tests.Cli
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
    /// Installs lucene-cli as a local tool in a temp directory from a local NuGet package file and runs commands on it.
    /// </summary>
    [Slow]
    public class InstallationTest : LuceneTestCase
    {
        private const string LuceneCliToolName = "lucene-cli";
        // The relative path from the AppDomain.CurrentDomain.BaseDirectory where we can find the tool project
        private static readonly string RelativeLuceneCliPath = $"../../../../{LuceneCliToolName}";

        private static DirectoryInfo tempWork;
        private static string packageVersion;

        public override void BeforeClass()
        {
            base.BeforeClass();
            tempWork = CreateTempDir();

            FileInfo packageFile;
            // NOTE: If using CI other than Azure DevOps, an environment variable named SYSTEM_DEFAULTWORKINGDIRECTORY can
            // be added to identify the directory that pre-built lucene-cli tool can be found in. Note that all subdirectories
            // are checked and since we only target one framework we don't care about target framework.
            string defaultWorkingDirectory = Environment.GetEnvironmentVariable("SYSTEM_DEFAULTWORKINGDIRECTORY");
            if (defaultWorkingDirectory is null)
            {
                Console.Write($"WARNING: System.DefaultWorkingDirectory environment variable not detected. The test will proceed by attempting to build the {LuceneCliToolName} project locally.");

                DirectoryInfo tempPackages = CreateTempDir();

                // For a local test, build our dotnet tool on the command line using the .csproj file
                DotNetPackLuceneCli(tempPackages, out packageFile);
            }
            else
            {
                // For a test on Azure DevOps, we scan for our lucene-cli NuGet package that is already packed.
                // We know it is somewhere below defaultWorkingDirectory and it is named like "<LuceneCliToolName>.<PackageVersion>.nupkg".
                var directory = new DirectoryInfo(defaultWorkingDirectory);
                packageFile = directory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                    .Where(f => f.Name.StartsWith($"{LuceneCliToolName}.", StringComparison.Ordinal)).FirstOrDefault();
                Assert.IsNotNull(packageFile, $"lucene-cli NuGet package not found in {defaultWorkingDirectory}");
            }

            packageVersion = GetPackageVersion(packageFile.Name);
            var targetFramework = GetTargetFramework();

            // Prepare our temp directory with a tool manifest so it can have local tools (we don't install globally to avoid conflicts with tools on dev machines).
            AssertCommandExitCode(ExitCode.Success, "dotnet", $"new tool-manifest --output \"{tempWork.FullName}\"");

            // Now install our tool and verify that the command succeeded.
            AssertCommandExitCode(ExitCode.Success, "dotnet", $"tool install {LuceneCliToolName} --version {packageVersion} --framework {targetFramework} --add-source \"{packageFile.DirectoryName}\" --tool-path  \"{tempWork.FullName}\"");
        }

        public override void AfterClass()
        {
            // Uninstall our tool - we are done with it.
            AssertCommandExitCode(ExitCode.Success, "dotnet", $"tool uninstall  {LuceneCliToolName} --tool-path  \"{tempWork.FullName}\"");
            base.AfterClass();
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestWithoutCommmand()
        {
            // Try running without any command. We should get the usage on the stdOut.
            // This is just a smoke test to make sure we can run the tool after it is installed.
            AssertLuceneCommandStdOutTextStartsWith("Lucene.Net Command Line Utility, Version:", "");
        }


        /// <summary>
        /// Makes a temporary build from source versioned specifically <see cref="Constants.LUCENE_VERSION"/> that we can test.
        /// </summary>
        private void DotNetPackLuceneCli(DirectoryInfo outputDirectory, out FileInfo packageFile)
        {
            string packageVersion = Constants.LUCENE_VERSION;
            string relativeLuceneCliProjectFile = NormalizeSlashes(Path.Combine(RelativeLuceneCliPath, $"{LuceneCliToolName}.csproj"));
            string absoluteLuceneCliProjectFile = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeLuceneCliProjectFile));
            Assert.IsTrue(File.Exists(absoluteLuceneCliProjectFile), $"{absoluteLuceneCliProjectFile} doesn't exist.");

            packageFile = new FileInfo(Path.Combine(outputDirectory.FullName, $"{LuceneCliToolName}.{packageVersion}.nupkg"));
            AssertCommandExitCode(ExitCode.Success, "dotnet", $"pack \"{absoluteLuceneCliProjectFile}\" --configuration Release --output \"{outputDirectory.FullName}\" -p:PackageVersion={packageVersion}");
        }

        private string NormalizeSlashes(string input)
        {
            return input.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        private string GetPackageVersion(string packageFile)
        {
            return packageFile.Replace($"{LuceneCliToolName}.", string.Empty, StringComparison.Ordinal).Replace(".nupkg", string.Empty, StringComparison.Ordinal);
        }

        private string GetTargetFramework()
        {
            var targetFrameworkAttribute = GetType().Assembly.GetAttributes<System.Reflection.AssemblyMetadataAttribute>(inherit: false).Where(a => a.Key == "TargetFramework").FirstOrDefault();
            if (targetFrameworkAttribute is null)
                Assert.Fail("TargetFramework metadata not found in this assembly.");
            return targetFrameworkAttribute.Value;
        }

        private string AppendCommandOutput(string message, string stdOut, string stdErr, int exitCode)
        {
            return $"{message}\n\nStdOut:\n{stdOut}\n\nStdErr:\n{stdErr}\n\nExit Code:\n{exitCode}";
        }

        private void AssertCommandExitCode(int expectedExitCode, string command, string arguments)
        {
            int exitCode = RunCommand(command, arguments, out string stdOut, out string stdErr);
            Assert.AreEqual(expectedExitCode, exitCode, AppendCommandOutput($"{command} {arguments} failed", stdOut, stdErr, exitCode));
        }

        private void AssertLuceneCommandStdOutTextStartsWith(string expectedStdOutStart, string arguments)
        {
            // Make sure to supply the entire path to the command in the temp directory that we installed locally so we don't
            // execute any globally installed lucene-cli tool.
            int exitCode = RunCommand(Path.Combine(tempWork.FullName, "lucene"), arguments, out string stdOut, out string stdErr);
            Assert.IsTrue(stdOut.TrimStart().StartsWith(expectedStdOutStart, StringComparison.Ordinal), AppendCommandOutput($"Expected stdOut to start with {expectedStdOutStart}", stdOut, stdErr, exitCode));
        }

        // returns exit code
        private int RunCommand(string executable, string arguments, out string stdOut, out string stdErr)
        {
            using Process p = new Process();

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = executable;
            p.StartInfo.Arguments = arguments;
            p.Start();

            stdOut = p.StandardOutput.ReadToEnd();
            stdErr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            return p.ExitCode;
        }
    }
}
