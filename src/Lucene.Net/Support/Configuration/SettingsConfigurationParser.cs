using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lucene.Net.Support.Configuration
{
    public class SettingsConfigurationParser : IConfigurationParser
    {
        public const string SettingsElement = "Settings";
        private const string SettingElement = "Setting";
        private const string Name = "Name";
        private const string Value = "Value";
        private const string Profile = "Profile";

        public bool CanParseElement(XElement element)
        {
            var ns = element.GetDefaultNamespace() ?? XNamespace.None;
            var matching = element.DescendantsAndSelf(ns.GetName(SettingsElement)).ToArray();
            return matching.Any();
        }

        public void ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results)
        {
            var ns = element.GetDefaultNamespace() ?? XNamespace.None;

            if (!CanParseElement(element))
            {
                return;
            }

            context.Push(SettingsElement);

            XName settingElement = ns.GetName(SettingElement);
            XName valueAttribute = ns.GetName(Value);

            var allSettings = element.DescendantsAndSelf(settingElement).ToArray();

            foreach (var setting in allSettings)
            {
                var nameElement = setting.Attribute(Name);

                context.Push(nameElement.Value);

                foreach (var valueElement in setting.Descendants(valueAttribute))
                {
                    var profileName = valueElement.Attribute(Profile).Value;
                    results.Add(GetKey(context, profileName), valueElement.Value);
                }

                context.Pop();
            }

            context.Pop();
        }

        private static string GetKey(Stack<string> context, string name)
        {
            return ConfigurationPath.Combine(context.Reverse().Concat(new[] { name }));
        }
    }
}
