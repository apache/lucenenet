---
uid: Lucene.Net.QueryParsers.Flexible.Messages
summary: *content
---

<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->


For Native Language Support (NLS), system of software internationalization.

## NLS message API

This utility API, adds support for NLS messages in the apache code. It is currently used by the lucene "New Flexible Query PArser". 

Features:

1. Message reference in the code, using static Strings
2. Message resource validation at class load time, for easier debugging
3. Allows for message IDs to be re-factored using code re-factor tools
4. Allows for reference count on messages, just like code
5. Lazy loading of Message Strings
6. Normal loading Message Strings 


Prerequisite for these examples: Add a resource file named `MessagesTestBundle.resx` and add messages for each of the public static string fields except for `Q0005E_Message_Not_In_Bundle`.

Lazy loading of Message Strings

```cs
public class MessagesTest : NLS
{
    private static readonly string BundleName = typeof(MessagesTest).FullName;

    private MessagesTest()
    {
        // should never be instantiated
    }

    static MessagesTest()
    {
        InitializeMessages(BundleName, typeof(MessagesTest));
    }

    // static string must match the strings in the property files.
    public static string Q0001E_Invalid_Syntax;
    public static string Q0004E_Invalid_Syntax_Escape_Unicode_Truncation;

    // this message is missing from the properties file
    public static string Q0005E_Message_Not_In_Bundle;
}

// Create a message reference
IMessage invalidSyntax = new Message(MessagesTest.Q0001E_Invalid_Syntax, "XXX");

// Do other stuff in the code...
// when is time to display the message to the user or log the message on a file
// the message is loaded from the correct bundle

string message1 = invalidSyntax.GetLocalizedMessage();
string message2 = invalidSyntax.GetLocalizedMessage(new CultureInfo("ja"));
```

Normal loading of Message Strings

```cs
string message1 = NLS.GetLocalizedMessage(MessagesTest.Q0004E_Invalid_Syntax_Escape_Unicode_Truncation);
string message2 = NLS.GetLocalizedMessage(MessagesTest.Q0004E_Invalid_Syntax_Escape_Unicode_Truncation, new CultureInfo("ja"));
```

The `Lucene.Net.QueryParsers.Flexible.Messages.TestNLS` NUnit test contains several other examples. The TestNLS C# code is available from the Apache Lucene.NET code repository. 