// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 
//See https://github.com/aspnet/Entropy/blob/dev/LICENSE.txt in the project root for license information.

//Code modified to work with latest version of framework.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Lucene.Net.Support.Configuration
{

    internal enum ConfigurationAction
    {
        Add,
        Remove,
        Clear
    }
  
    public class KeyValueParser : IConfigurationParser
    {
        private readonly string _keyName = "key";
        private readonly string _valueName = "value";
        private readonly string[] _supportedActions = Enum.GetNames(typeof(ConfigurationAction)).Select(x => x.ToLowerInvariant()).ToArray();

        public KeyValueParser()
            : this("key", "value")
        { }

        public KeyValueParser(string key, string value)
        {
            _keyName = key;
            _valueName = value;
         }

        public bool CanParseElement(XElement element)
        {
            var hasKeyAttribute = element.DescendantsAndSelf().Any(x => x.Attribute(_keyName) != null);

            return hasKeyAttribute;
        }

        public void ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results)
        {
            if (!CanParseElement(element))
            {
                return;
            }

            if (!element.Elements().Any())
            {
                AddToDictionary(element, context, results);
            }

            context.Push(element.Name.ToString());

            foreach (var node in element.Elements())
            {
                var hasSupportedAction = node.DescendantsAndSelf().Any(x => _supportedActions.Contains(x.Name.ToString().ToLowerInvariant()));

                if (!hasSupportedAction)
                {
                    continue;
                }

                ParseElement(node, context, results);
            }

            context.Pop();
        }

        private void AddToDictionary(XElement element, Stack<string> context, SortedDictionary<string, string> results)
        {
            ConfigurationAction action;

            if (!Enum.TryParse(element.Name.ToString(), true, out action))
            {
                return;
            }

            var key = element.Attribute(_keyName);
            var value = element.Attribute(_valueName);

            if (key == null)
            {
                return;
            }

            var fullkey = GetKey(context, key.Value);

            switch (action)
            {
                case ConfigurationAction.Add:
                    string valueToAdd = value.Value;

                    if (results.ContainsKey(fullkey))
                    {
                        results[fullkey] = valueToAdd;
                    }
                    else
                    {
                        results.Add(fullkey, valueToAdd);
                    }
                    break;
                case ConfigurationAction.Remove:
                    results.Remove(fullkey);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported action: [{action}]");
            }
        }

        private static string GetKey(Stack<string> context, string name)
        {
            return ConfigurationPath.Combine(context.Reverse().Concat(new[] { name }));
        }
    }
}
