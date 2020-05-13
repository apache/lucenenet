using Microsoft.Extensions.Configuration;
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
        public static void AddJsonFilesFromRootDirectoryTo(this IConfigurationBuilder builder, string currentPath, string fileName)
        {
            Stack<string> locations = builder.ScanConfigurationFiles(currentPath, fileName);

#if NETSTANDARD
            while (locations.Count != 0)
            {
                builder.AddJsonFile(locations.Pop(), optional: true, reloadOnChange: true);
            }
#elif NET45
                // NET45 specific setup for builder
#else
                // Not sure if there is a default case that isnt covered?
#endif
        }

        [CLSCompliant(false)]
        public static void AddXmlFilesFromRootDirectoryTo(this IConfigurationBuilder builder, string currentPath, string fileName)
        {
            Stack<string> locations = builder.ScanConfigurationFiles(currentPath, fileName);

#if NETSTANDARD
            while (locations.Count != 0)
            {
                builder.AddXmlFile(locations.Pop(), optional: true, reloadOnChange: true);
            }
#elif NET45
                // NET45 specific setup for builder
#else
                // Not sure if there is a default case that isnt covered?
#endif
        }

        private static Stack<string> ScanConfigurationFiles(this IConfigurationBuilder builder, string currentPath, string fileName)
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
