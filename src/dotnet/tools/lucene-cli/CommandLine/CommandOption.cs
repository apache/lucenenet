// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Cli.CommandLine
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    public class CommandOption
    {
        public CommandOption(string template, CommandOptionType optionType)
        {
            Template = template;
            OptionType = optionType;
            Values = new List<string>();

            foreach (var part in Template.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("--", StringComparison.Ordinal))
                {
                    LongName = part.Substring(2);
                }
                else if (part.StartsWith("-", StringComparison.Ordinal))
                {
                    var optName = part.Substring(1);

                    // If there is only one char and it is not an English letter, it is a symbol option (e.g. "-?")
                    if (optName.Length == 1 && !IsEnglishLetter(optName[0]))
                    {
                        SymbolName = optName;
                    }
                    else
                    {
                        ShortName = optName;
                    }
                }
                else if (part.StartsWith("<", StringComparison.Ordinal) && part.EndsWith(">", StringComparison.Ordinal))
                {
                    ValueName = part.Substring(1, part.Length - 2);
                }
                else
                {
                    throw new ArgumentException($"Invalid template pattern '{template}'", nameof(template));
                }
            }

            if (string.IsNullOrEmpty(LongName) && string.IsNullOrEmpty(ShortName) && string.IsNullOrEmpty(SymbolName))
            {
                throw new ArgumentException($"Invalid template pattern '{template}'", nameof(template));
            }
        }

        /// <summary>
        /// An ID that can be used to locate this option.
        /// </summary>
        public string UniqueId { get; set; }
        public string Template { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string SymbolName { get; set; }
        public string ValueName { get; set; }
        public string Description { get; set; }
        public List<string> Values { get; private set; }
        public CommandOptionType OptionType { get; private set; }
        public bool ShowInHelpText { get; set; } = true;
        public bool Inherited { get; set; }

        public bool TryParse(string value)
        {
            switch (OptionType)
            {
                case CommandOptionType.MultipleValue:
                    Values.Add(value);
                    break;
                case CommandOptionType.SingleValue:
                    if (Values.Any())
                    {
                        return false;
                    }
                    Values.Add(value);
                    break;
                case CommandOptionType.NoValue:
                    if (value != null)
                    {
                        return false;
                    }
                    // Add a value to indicate that this option was specified
                    Values.Add("on");
                    break;
                default:
                    break;
            }
            return true;
        }

        public bool HasValue()
        {
            return Values.Any();
        }

        public string Value()
        {
            return HasValue() ? Values[0] : null;
        }

        private static bool IsEnglishLetter(char c) // LUCENENET: CA1822: Mark members as static
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
    }
}
