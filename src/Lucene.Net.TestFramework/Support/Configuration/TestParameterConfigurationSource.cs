
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Configuration
{
    class TestParameterConfigurationSource
        : CommandLineConfigurationSource, IConfigurationSource

    {
        public TestParameterConfigurationSource(TestParameters testParameters)
        {
            List<string> args1 = new List<string>();
            foreach (string x in testParameters.Names)
            {
                args1.Add(string.Format("{0}={1}", x, testParameters[x]));
            }
            if (args1 != null)
                Args = args1.ToArray();
        }
    }
    
}
