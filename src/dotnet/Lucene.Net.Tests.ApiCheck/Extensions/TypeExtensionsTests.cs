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

using Lucene.Net.ApiCheck.Extensions;

namespace Lucene.Net.Tests.ApiCheck.Extensions;

public class TypeExtensionsTests
{
    // BCL primitives (and System.Void) should render as their C# keyword equivalents
    // so the report shows 'void' / 'int' rather than 'System.Void' / 'System.Int32'.
    [InlineData(typeof(void), "void")]
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(byte), "byte")]
    [InlineData(typeof(sbyte), "sbyte")]
    [InlineData(typeof(char), "char")]
    [InlineData(typeof(short), "short")]
    [InlineData(typeof(ushort), "ushort")]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(uint), "uint")]
    [InlineData(typeof(long), "long")]
    [InlineData(typeof(ulong), "ulong")]
    [InlineData(typeof(float), "float")]
    [InlineData(typeof(double), "double")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(object), "object")]
    [Theory]
    public void FormatDisplayName_BclTypesUseCSharpKeyword(Type type, string expected)
    {
        Assert.Equal(expected, type.FormatDisplayName());
    }

    [Fact]
    public void FormatDisplayName_NonBclTypeFallsBackToFullName()
    {
        Assert.Equal("System.IO.TextReader", typeof(System.IO.TextReader).FormatDisplayName());
    }

    // Array, by-ref, and pointer wrappers should propagate the keyword formatting
    // through to the inner element type.
    [InlineData(typeof(int[]), "int[]")]
    [InlineData(typeof(string[]), "string[]")]
    [InlineData(typeof(int[][]), "int[][]")]
    [InlineData(typeof(System.IO.TextReader[]), "System.IO.TextReader[]")]
    [Theory]
    public void FormatDisplayName_ArrayTypes(Type type, string expected)
    {
        Assert.Equal(expected, type.FormatDisplayName());
    }

    [Fact]
    public void FormatDisplayName_ByRefType()
    {
        Assert.Equal("string&", typeof(string).MakeByRefType().FormatDisplayName());
    }

    [Fact]
    public void FormatDisplayName_PointerType()
    {
        Assert.Equal("int*", typeof(int).MakePointerType().FormatDisplayName());
    }

    // Generic arguments should also use C# keyword formatting (e.g. List<int>, not List<Int32>).
    [Fact]
    public void FormatDisplayName_GenericTypeArgumentsUseKeywords()
    {
        Assert.Equal("System.Collections.Generic.List<int>", typeof(List<int>).FormatDisplayName());
    }

    [Fact]
    public void FormatDisplayName_GenericTypeArgumentsMultiple()
    {
        Assert.Equal(
            "System.Collections.Generic.Dictionary<string, int>",
            typeof(Dictionary<string, int>).FormatDisplayName());
    }
}
