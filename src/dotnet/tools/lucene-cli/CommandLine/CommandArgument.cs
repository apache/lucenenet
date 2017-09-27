// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Cli.CommandLine
{
    public class CommandArgument
    {
        public CommandArgument()
        {
            Values = new List<string>();
        }

        //public string Id { get; set; } // used to identify a command in the list
        public virtual string Name { get; set; }
        public bool ShowInHelpText { get; set; } = true;
        public virtual string Description { get; set; }
        public List<string> Values { get; private set; }
        public bool MultipleValues { get; set; }
        public virtual string Value
        {
            get
            {
                return Values.FirstOrDefault();
            }
        }
    }
}
