using Microsoft.Extensions.Configuration;
using System;

namespace Lucene.Net.Configuration
{
    public interface IConfigurationSettings
    {
        IConfigurationRoot CurrentConfiguration { get; }
    }

    public class MockConfigurationSettings : IConfigurationSettings
    {
        private readonly IConfigurationRoot configurationRoot;

        public MockConfigurationSettings(IConfigurationRoot configurationRoot)
        {
            this.configurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
        }

        public IConfigurationRoot CurrentConfiguration => configurationRoot;
    }
}