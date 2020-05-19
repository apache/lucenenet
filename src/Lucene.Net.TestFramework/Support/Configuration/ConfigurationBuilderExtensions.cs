using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace Lucene.Net.Configuration
{
    public static class ConfigurationBuilderExtensions
    {

        [CLSCompliant(false)]
        public static IConfigurationBuilder AddLuceneDefaultSettings(this IConfigurationBuilder configurationBuilder, string prefix)
        {
            return configurationBuilder.Add(new LuceneDefaultConfigurationSource() { Prefix = prefix });
        }
        [CLSCompliant(false)]
        public static IConfigurationBuilder AddNUnitTestRunSettings(this IConfigurationBuilder configurationBuilder)
        {
            return configurationBuilder.Add(new TestParameterConfigurationSource() { TestParameters = NUnit.Framework.TestContext.Parameters });
        }

        [CLSCompliant(false)]
        public static IConfigurationBuilder AddJsonFilesFromRootDirectoryTo(this IConfigurationBuilder builder, string currentPath, string fileName)
        {
            Stack<string> locations = ScanConfigurationFiles(currentPath, fileName);

            while (locations.Count != 0)
            {
                builder.AddJsonFile(locations.Pop(), optional: true, reloadOnChange: true);
            }
            return builder;
        }

        [CLSCompliant(false)]
        public static IConfigurationBuilder AddXmlFilesFromRootDirectoryTo(this IConfigurationBuilder builder, string currentPath, string fileName)
        {
            Stack<string> locations = ScanConfigurationFiles(currentPath, fileName);

            while (locations.Count != 0)
            {
                builder.AddXmlFile(locations.Pop(), optional: true, reloadOnChange: true);
            }
            return builder;
        }

        private static Stack<string> ScanConfigurationFiles(string currentPath, string fileName)
        {
            Stack<string> locations = new Stack<string>();

            string candidatePath = System.IO.Path.Combine(currentPath, fileName);
            if (File.Exists(candidatePath))
            {
                locations.Push(candidatePath);
            }

            try
            {
                while (new DirectoryInfo(currentPath).Parent != null)
                {
                    candidatePath = System.IO.Path.Combine(new DirectoryInfo(currentPath).Parent.FullName, fileName);
                    if (File.Exists(candidatePath))
                    {
                        locations.Push(candidatePath);
                    }
                    currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                }
            }
            catch (SecurityException)
            {
                // ignore security errors
            }
            return locations;
        }
    }

}
