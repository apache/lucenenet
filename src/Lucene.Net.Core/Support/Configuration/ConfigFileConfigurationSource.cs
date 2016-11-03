using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Lucene.Net.Support.Configuration
{
    public class ConfigFileConfigurationSource : IConfigurationSource
    {
        public string Configuration { get; set; }
        public bool LoadFromFile { get; set; }
        public bool Optional { get; set; }
        public IEnumerable<IConfigurationParser> Parsers { get; set; }

        public ConfigFileConfigurationSource(string configuration, bool loadFromFile, bool optional, params IConfigurationParser[] parsers)
        { 
            LoadFromFile = loadFromFile;
            Configuration = configuration;
            Optional = optional;

            var parsersToUse = new List<IConfigurationParser> {
                new KeyValueParser(),
                new KeyValueParser("name", "connectionString")
            };

            parsersToUse.AddRange(parsers);

            Parsers = parsersToUse.ToArray();
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ConfigFileConfigurationProvider(Configuration, LoadFromFile, Optional, Parsers);
        }
    }
}
