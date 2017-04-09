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
        public const string SETTINGS_ELEMENT = "Settings";
        private const string SETTING_ELEMENT = "Setting";
        private const string NAME = "Name";
        private const string VALUE = "Value";
        private const string PROFILE = "Profile";

        public bool CanParseElement(XElement element)
        {
            var ns = element.GetDefaultNamespace() ?? XNamespace.None;
            var matching = element.DescendantsAndSelf(ns.GetName(SETTINGS_ELEMENT)).ToArray();
            return matching.Any();
        }

        public void ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results)
        {
            var ns = element.GetDefaultNamespace() ?? XNamespace.None;

            if (!CanParseElement(element))
            {
                return;
            }

            context.Push(SETTINGS_ELEMENT);

            XName settingElement = ns.GetName(SETTING_ELEMENT);
            XName valueAttribute = ns.GetName(VALUE);

            var allSettings = element.DescendantsAndSelf(settingElement).ToArray();

            foreach (var setting in allSettings)
            {
                var nameElement = setting.Attribute(NAME);

                context.Push(nameElement.Value);

                foreach (var valueElement in setting.Descendants(valueAttribute))
                {
                    var profileName = valueElement.Attribute(PROFILE).Value;
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
