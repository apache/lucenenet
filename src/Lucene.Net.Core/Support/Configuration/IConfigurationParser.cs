// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 
//See https://github.com/aspnet/Entropy/blob/dev/LICENSE.txt in the project root for license information.

//Code modified to work with latest version of framework.

using System.Collections.Generic;
using System.Xml.Linq;

namespace Lucene.Net.Support.Configuration
{
    public interface IConfigurationParser
    {
      
        bool CanParseElement(XElement element);

    
        void ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results);
    }
}
