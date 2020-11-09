using System;
using System.Diagnostics.CodeAnalysis;

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

    [SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "This class is intended to hold the options that are passed into the app")]
    public class CommandLineOptions
    {
        public static int Parse(string[] args)
        {
            var options = new CommandLineOptions();

            var cmd = new RootCommand.Configuration(options);

            try
            {
                return cmd.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(Resources.Strings.GeneralExceptionMessage + Environment.NewLine + ex.ToString());
                cmd.ShowHint();
                return 1;
            }
        }

        //public static readonly string VERBOSE_OPTION_VALUE_NAME = "verbose";
        //public static readonly string DIRECTORY_TYPE_VALUE_NAME = "directoryType";
        //public static readonly string INDEX_DIRECTORY_ARGUMENT_ID = "indexDirectory";

        //public CommandOption VerboseOption = new CommandOption("-v|--verbose", CommandOptionType.NoValue)
        //{
        //    Description = Resources.Strings.VerboseOptionDescription,
        //    ValueName = VERBOSE_OPTION_VALUE_NAME
        //};
        //public CommandArgument IndexDirectoryArgument = new CommandArgument()
        //{
        //    Name = "[<INDEX-DIRECTORY>]",
        //    Description = Resources.Strings.IndexDirectoryArgumentDescription,
        //    Id = INDEX_DIRECTORY_ARGUMENT_ID
        //};
        //public string IndexDirectory
        //{
        //    get
        //    {
        //        // Return current directory if index directory not supplied.
        //        return string.IsNullOrWhiteSpace(IndexDirectoryArgument.Value) ?
        //            System.AppContext.BaseDirectory :
        //            IndexDirectoryArgument.Value;
        //    }
        //}

        //public CommandOption DirectoryTypeOption = new CommandOption("-dir|-dir-impl|--dir-impl|--directory-type", CommandOptionType.SingleValue)
        //{
        //    Description = Resources.Strings.DirectoryTypeOptionDescription,
        //    ValueName = DIRECTORY_TYPE_VALUE_NAME
        //};
    }
}
