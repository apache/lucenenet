
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Configuration
{
    //class TestParameterConfigurationSource
    //    : CommandLineConfigurationSource, IConfigurationSource

    //{
    //    public TestParameterConfigurationSource(TestParameters testParameters)
    //    {
    //        List<string> args1 = new List<string>();
    //        foreach (string x in testParameters.Names)
    //        {
    //            args1.Add(string.Format("{0}={1}", x, testParameters[x]));
    //        }
    //        if (args1 != null)
    //            Args = args1.ToArray();
    //    }
    //}
    public class TestParameterConfigurationProvider : IConfigurationProvider
    {
        private CommandLineConfigurationProvider _instance;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="testParameters">The Test Parameter args.</param>
        public TestParameterConfigurationProvider(TestParameters testParameters)
        {
            List<string> args1 = new List<string>();
            foreach (string x in testParameters.Names)
            {
                args1.Add(string.Format("{0}={1}", x, testParameters[x]));
            }

            _instance = new CommandLineConfigurationProvider(args1, null);
        }

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            return _instance.GetChildKeys(earlierKeys, parentPath);
        }
        [CLSCompliant(false)]
        public IChangeToken GetReloadToken()
        {
            return _instance.GetReloadToken();
        }

        public void Load()
        {
            _instance.Load();
        }

        public void Set(string key, string value)
        {
            _instance.Set(key, value);
        }

        public bool TryGet(string key, out string value)
        {
            return _instance.TryGet(key, out value);
        }
    }
}
