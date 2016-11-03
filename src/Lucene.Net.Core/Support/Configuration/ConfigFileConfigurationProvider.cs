// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 
//See https://github.com/aspnet/Entropy/blob/dev/LICENSE.txt in the project root for license information.

//Code modified to work with latest version of framework.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Lucene.Net.Support.Configuration
{
    public class ConfigFileConfigurationProvider : ConfigurationProvider
    {
        private readonly string _configuration;
        private readonly bool _loadFromFile;
        private readonly bool _isOptional;

        private readonly IEnumerable<IConfigurationParser> _parsers;

        public ConfigFileConfigurationProvider(string configuration, bool loadFromFile, bool optional,IEnumerable<IConfigurationParser> parsers)
        {
            _loadFromFile = loadFromFile;
            _configuration = configuration;
            _isOptional = optional;
            _parsers = parsers;
        }

        public override void Load()
        {
            if (_loadFromFile && !_isOptional && !File.Exists(_configuration))
            {
                throw new FileNotFoundException("Could not find configuration file to load.", _configuration);
            }

            var document = _loadFromFile ? XDocument.Load(_configuration) : XDocument.Parse(_configuration);

            var context = new Stack<string>();
            var dictionary = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var child in document.Root.Elements())
            {
                ParseElement(child, context, dictionary);
            }

            Data = dictionary;
        }

        /// <summary>
        /// Given an XElement tries to parse that element using any of the KeyValueParsers
        /// and adds it to the results dictionary
        /// </summary>
        private void ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results)
        {
            foreach (var parser in _parsers)
            {
                if (parser.CanParseElement(element))
                {
                    parser.ParseElement(element, context, results);
                    break;
                }
            }
        }
    }
}
