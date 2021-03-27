using Lucene.Net.Cli.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Cli
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

    public abstract class ConfigurationBase : CommandLineApplication
    {
        private static readonly Assembly thisAssembly = typeof(ConfigurationBase).Assembly;
        protected static string HELP_VALUE_NAME = "help";

        protected ConfigurationBase()
            //: base(throwOnUnexpectedArg: false)
        {
            var help = this.HelpOption("-?|-h|--help");
            help.UniqueId = HELP_VALUE_NAME;
            help.ShowInHelpText = false;

            this.ShortVersionGetter = () => 
            {
                return "Lucene.Net Command Line Utility, Version: " + thisAssembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;
            };

            this.LongVersionGetter = () =>
            {
                return ShortVersionGetter();
            };
        }

        public override void OnExecute(Func<int> invoke)
        {
            base.OnExecute(() =>
            {
                if (this.GetOptionByUniqueId(HELP_VALUE_NAME).HasValue())
                {
                    this.ShowHelp();
                    return ExitCode.Success;
                }
                try
                {
                    return invoke();
                }
                catch (ArgumentException)
                {
                    // Rather than writing to console, the
                    // utilities are now throwing ArgumentException
                    // if the args cannot be parsed.
                    this.ShowHint();
                    this.ShowHelp();
                    return ExitCode.GeneralError;
                }
            });
        }

        public Action<string[]> Main { get; set; }

        public CommandOption GetOptionByUniqueId(string uniqueId)
        {
            return this.Options.FirstOrDefault(o => o.UniqueId == uniqueId);
        }

        public CommandOption GetOption<T>()
        {
            return this.Options.FirstOrDefault(o => typeof(T).IsAssignableFrom(o.GetType()));
        }

        public CommandArgument GetArgument<T>()
        {
            return this.Arguments.FirstOrDefault(o => typeof(T).IsAssignableFrom(o.GetType()));
        }

        /// <summary>
        /// Gets the resource with a specific name. It is automatically
        /// prefixed by the current command name.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        protected string FromResource(string resourceName)
        {
            return Resources.Strings.ResourceManager.GetString(this.GetType().DeclaringType.Name + resourceName);
        }

        public void ShowNotEnoughArguments(int minimum)
        {
            Out.WriteLine(Resources.Strings.NotEnoughArguments, minimum);
        }

        public bool ValidateArguments(int minimum)
        {
            var args = GetNonNullArguments();

            if (args.Length < minimum)
            {
                this.ShowNotEnoughArguments(minimum);
                this.ShowHelp();
                return false;
            }
            return true;
        }

        public string[] GetNonNullArguments()
        {
            return this.Arguments
                .Where(a => !string.IsNullOrWhiteSpace(a.Value))
                .SelectMany(a => a.MultipleValues ? a.Values : new List<string> { a.Value })
                .ToArray();
        }
    }
}
