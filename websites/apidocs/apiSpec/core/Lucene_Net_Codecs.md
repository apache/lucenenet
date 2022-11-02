---
uid: Lucene.Net.Codecs
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

Codecs API: API for customization of the encoding and structure of the index.

 The Codec API allows you to customize the way the following pieces of index information are stored:

* Postings lists - see <xref:Lucene.Net.Codecs.PostingsFormat>
* DocValues - see <xref:Lucene.Net.Codecs.DocValuesFormat>
* Stored fields - see <xref:Lucene.Net.Codecs.StoredFieldsFormat>
* Term vectors - see <xref:Lucene.Net.Codecs.TermVectorsFormat>
* FieldInfos - see <xref:Lucene.Net.Codecs.FieldInfosFormat>
* SegmentInfo - see <xref:Lucene.Net.Codecs.SegmentInfoFormat>
* Norms - see <xref:Lucene.Net.Codecs.NormsFormat>
* Live documents - see <xref:Lucene.Net.Codecs.LiveDocsFormat> 

For some concrete implementations beyond Lucene's official index format, see the [Codecs module](../codecs/overview.html).

Codecs are identified by name through the <xref:Lucene.Net.Codecs.ICodecFactory> implementation, which by default is the <xref:Lucene.Net.Codecs.DefaultCodecFactory>. To create your own codec, extend <xref:Lucene.Net.Codecs.Codec>. By default, the name of the class (minus the suffix "Codec") will be used as the codec's name.

```cs
// By default, the name will be "My" because the "Codec" suffix is removed
public class MyCodec : Codec 
{
}
```

> [!NOTE]
> There is a built-in <xref:Lucene.Net.Codecs.FilterCodec> type that can be used to easily extend an existing codec type.

To override the default codec name, decorate the custom codec with the <xref:Lucene.Net.Codecs.CodecNameAttribute>.

The <xref:Lucene.Net.Codecs.CodecNameAttribute> can be used to set the name to that of a built-in codec to override its registration in the <xref:Lucene.Net.Codecs.DefaultCodecFactory>.  

```cs
[CodecName("MyCodec")] // Sets the codec name explicitly
public class MyCodec : Codec
{
}
```

 Register the Codec class so Lucene.NET can find it either by providing it to the <xref:Lucene.Net.Codecs.DefaultCodecFactory> at application start up or by using a dependency injection container.

## Using Microsoft.Extensions.DependencyInjection to Register a Custom Codec

 First, create an <xref:Lucene.Net.Codecs.ICodecFactory> implementation to return the type based on a string name. Here is a generic implementation, that can be used with almost any dependency injection container.

```cs
public class NamedCodecFactory : ICodecFactory, IServiceListable
{
    private readonly IDictionary<string, Codec> codecs;

    public NamedCodecFactory(IEnumerable<Codec> codecs)
    {
        this.codecs = codecs.ToDictionary(n => n.Name);
    }

    public ICollection<string> AvailableServices => codecs.Keys;

    public Codec GetCodec(string name)
    {
        if (codecs.TryGetValue(name, out Codec value))
            return value;

        throw new ArgumentException($"The codec {name} is not registered.", nameof(name));
    }
}
```

> [!NOTE]
> Implementing <xref:Lucene.Net.Util.IServiceListable> is optional. This allows for logging scenarios (such as those built into the test framework) to list the codecs that are registered.

Next, register all of the codecs that your Lucene.NET implementation will use and the `NamedCodecFactory` with dependency injection using singleton lifetime.

```cs
IServiceProvider services = new ServiceCollection()
    .AddSingleton<Codec, Lucene.Net.Codecs.Lucene46.Lucene46Codec>()
    .AddSingleton<Codec, MyCodec>()
    .AddSingleton<ICodecFactory, NamedCodecFactory>()
    .BuildServiceProvider();
```

Finally, set the <xref:Lucene.Net.Codecs.ICodecFactory> implementation Lucene.NET will use with the static [Codec.SetCodecFactory(ICodecFactory)](xref:Lucene.Net.Codecs.Codec) method. This must be done one time at application start up.

```cs
Codec.SetCodecFactory(services.GetService<ICodecFactory>());
```

## Using <xref:Lucene.Net.Codecs.DefaultCodecFactory> to Register a Custom Codec

If your application is not using dependency injection, you can register a custom codec by adding your codec at start up.

```cs
Codec.SetCodecFactory(new DefaultCodecFactory { 
    CustomCodecTypes = new Type[] { typeof(MyCodec) }
});
```

> [!NOTE]
> <xref:Lucene.Net.Codecs.DefaultCodecFactory> also registers all built-in codec types automatically.

## Custom Postings Formats

If you just want to customize the <xref:Lucene.Net.Codecs.PostingsFormat>, or use different postings formats for different fields.

```cs
[PostingsFormatName("MyPostingsFormat")]
public class MyPostingsFormat : PostingsFormat
{
    private readonly string field;

    public MyPostingsFormat(string field)
    {
        this.field = field ?? throw new ArgumentNullException(nameof(field));
    }

    public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
    {
        // Returns fields consumer...
    }

    public override FieldsProducer FieldsProducer(SegmentReadState state)
    {
        // Returns fields producer...
    }
}
```

Extend the the default <xref:Lucene.Net.Codecs.Lucene46.Lucene46Codec>, and override [GetPostingsFormatForField(string)](xref:Lucene.Net.Codecs.Lucene46.Lucene46Codec) to return your custom postings format.

```cs
[CodecName("MyCodec")]
public class MyCodec : Lucene46Codec
{
    public override PostingsFormat GetPostingsFormatForField(string field)
    {
        return new MyPostingsFormat(field);
    }
}
```

Registration of a custom postings format is similar to registering custom codecs, implement <xref:Lucene.Net.Codecs.IPostingsFormatFactory> and then call <xref:Lucene.Net.Codecs.PostingsFormat.SetPostingsFormatFactory> at application start up.

```cs
PostingsFormat.SetPostingsFormatFactory(new DefaultPostingsFormatFactory {
    CustomPostingsFormatTypes = new Type[] { typeof(MyPostingsFormat) }
});
```

## Custom DocValues Formats

Similarly, if you just want to customize the <xref:Lucene.Net.Codecs.DocValuesFormat> per-field, have a look at [GetDocValuesFormatForField(string)](xref:Lucene.Net.Codecs.Lucene46.Lucene46Codec). Custom implementations can be provided by implementing <xref:Lucene.Net.Codecs.IDocValuesFormatFactory> and registering  the factory using <xref:Lucene.Net.Codecs.DocValuesFormat.SetDocValuesFormatFactory>.

## Testing Custom Codecs

The <xref:Lucene.Net.TestFramework> library contains specialized classes to minimize the amount of code required to thoroughly test extensions to Lucene.NET. Create a new class library project targeting an executable framework your consumers will be using and add the following NuGet package reference. The test framework uses NUnit as the test runner.

> [!NOTE]
> See [Unit testing C# with NUnit and .NET Core](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-nunit) for detailed instructions on how to set up a class library to use with NUnit.

> [!NOTE]
> .NET Standard is not an executable target. Tests will not run unless you target a framework such as `net6.0` or `net48`.

Here is an example project file for .NET 5 for testing a project named `MyCodecs.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
    <PackageReference Include="nunit" Version="3.13.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="3.17.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.11.0" />
    <PackageReference Include="Lucene.Net.TestFramework" Version="4.8.0-beta00016" />
    <PackageReference Include="System.Net.Primitives" Version="4.3.0"/>
    </ItemGroup>

    <ItemGroup>
    <ProjectReference Include="..\MyCodecs\MyCodecs.csproj" />
    </ItemGroup>

</Project>
```

> [!NOTE]
> This example outlines testing a custom <xref:Lucene.Net.Codecs.PostingsFormat>, but testing other codec dependencies is a similar procedure.

To extend an existing codec with a new <xref:Lucene.Net.Codecs.PostingsFormat>, the <xref:Lucene.Net.Codecs.FilterCodec> class can be subclassed and the codec to be extended supplied to the <xref:Lucene.Net.Codecs.FilterCodec> constructor. A <xref:Lucene.Net.Codecs.PostingsFormat> should be supplied to an existing codec to run the tests against it.

This example is for testing a custom postings format named `MyPostingsFormat`. Creating a postings format is a bit involved, but an overview of the process is in [Building a new Lucene postings format ](http://blog.mikemccandless.com/2012/07/building-new-lucene-postings-format.html).

```cs
public class MyCodec : FilterCodec
{
    private readonly PostingsFormat myPostingsFormat;

    public MyCodec()
        : base(new Lucene.Net.Codecs.Lucene46.Lucene46Codec())
    {
        myPostingsFormat = new MyPostingsFormat();
    }
}
```

Next, add a class to the test project and decorate it with the `TestFixtureAttribute` from NUnit. To test a postings format, subclass <xref:Lucene.Net.Index.BasePostingsFormatTestCase>, override the `GetCodec()` method, and return the codec under test. The codec can be cached in a member variable to improve the performance of the tests.

```cs
namespace ExampleLuceneNetTestFramework
{
    [TestFixture]
    public class TestMyPostingsFormat : BasePostingsFormatTestCase
    {
        private readonly Codec codec = new MyCodec();

        protected override Codec GetCodec()
        {
            return codec;
        }
    }
}
```

The <xref:Lucene.Net.Index.BasePostingsFormatTestCase> class includes a barrage of 8 tests that can now be run using your favorite test runner, such as Visual Studio Test Explorer. 

 - TestDocsAndFreqs
 - TestDocsAndFreqsAndPositions
 - TestDocsAndFreqsAndPositionsAndOffsets
 - TestDocsAndFreqsAndPositionsAndOffsetsAndPayloads
 - TestDocsAndFreqsAndPositionsAndPayloads
 - TestDocsOnly
 - TestMergeStability
 - TestRandom

The goal of the <xref:Lucene.Net.Index.BasePostingsFormatTestCase> is that if all of these tests pass, then the  <xref:Lucene.Net.Codecs.PostingsFormat> will always be compatible with Lucene.NET.

## Registering Codecs with the Test Framework

Codecs, postings formats and doc values formats can be injected into the test framework to integration test them against other Lucene.NET components. This is an advanced scenario that assumes integration tests for Lucene.NET components exist in your test project.

In your test project, add a new file to the root of the project named `Startup.cs` that inherits <xref:Lucene.Net.Util.LuceneTestFrameworkInitializer>. The file may exist in any namespace. Override the `Initialize()` method to set your custom `CodecFactory`.

> [!NOTE]
> There may only be one `LuceneTestFrameworkInitializer` subclass per assembly.


```cs
public class Startup : LuceneTestFrameworkInitializer
{
    /// <summary>
    /// Runs before all tests in the current assembly
    /// </summary>
    protected override void Initialize()
    {
        CodecFactory = new TestCodecFactory {
            CustomCodecTypes = new Codec[] { typeof(MyCodec) }
        };
    }
}
```

> [!IMPORTANT]
> In Lucene.NET 4.8.0-beta00015 and prior, the `CodecFactory` should be set in the `TestFrameworkSetUp()` method, however all later versions must use the `Initialize()` method to set the factory properties, or an `InvalidOperationException` will be thrown.

## Setting the Default Codec for use in Tests

The above block will register a new codec named `MyCodec` with the test framework. However, the test framework will not select the codec for use in tests on its own. To override the default behavior of selecting a random codec, the configuration parameter `tests:codec` must be set explicitly.

> [!NOTE]
> A codec name is derived from either the name of the class (minus the "Codec" suffix) or the <xref:Lucene.Net.Codecs.CodecName.Name> property.

#### Setting the Default Codec using an Environment Variable

Set an environment variable named `lucene:tests:codec` to the name of the codec.

```
$env:lucene:tests:codec = "MyCodec"; # Powershell example
```

#### Setting the Default Codec using a Configuration File

Add a file to the test project (or a parent directory of the test project) named `lucene.testsettings.json` with a value named `tests:codec`.

```json
{
    "tests": {
    "codec": "MyCodec"
    }
}
```

## Setting the Default Postings Format or Doc Values Format for use in Tests

Similarly to codecs, the default postings format or doc values format can be set via environment variable or configuration file.

#### Environment Variables

Set environment variables named `lucene:tests:postingsformat` to the name of the postings format and/or `lucene:tests:docvaluesformat` to the name of the doc values format.

```
$env:lucene:tests:postingsformat = "MyPostingsFormat"; # Powershell example
$env:lucene:tests:docvaluesformat = "MyDocValuesFormat"; # Powershell example
```

#### Configuration File

Add a file to the test project (or a parent directory of the test project) named `lucene.testsettings.json` with a value named `tests:postingsformat` and/or `tests:docvaluesformat`.

```json
{
    "tests": {
    "postingsformat": "MyPostingsFormat",
    "docvaluesformat": "MyDocValuesFormat"
    }
}
```

## Default Codec Configuration

For reference, the default configuration of codecs, postings formats, and doc values are as follows.

 #### Codecs

These are the types registered by the <xref:Lucene.Net.Codecs.DefaultCodecFactory> by default.

 | Name | Type | Assembly |
 | ---- | ---- | -------- |
 | `Lucene46` | <xref:Lucene.Net.Codecs.Lucene46.Lucene46Codec> | Lucene.Net.dll |
 | `Lucene3x` | <xref:Lucene.Net.Codecs.Lucene3x.Lucene3xCodec> | Lucene.Net.dll |
 | `Lucene45` | <xref:Lucene.Net.Codecs.Lucene45.Lucene45Codec> | Lucene.Net.dll |
 | `Lucene42` | <xref:Lucene.Net.Codecs.Lucene42.Lucene42Codec> | Lucene.Net.dll |
 | `Lucene41` | <xref:Lucene.Net.Codecs.Lucene41.Lucene41Codec> | Lucene.Net.dll |
 | `Lucene40` | <xref:Lucene.Net.Codecs.Lucene40.Lucene40Codec> | Lucene.Net.dll |
 | `Appending` | <xref:Lucene.Net.Codecs.Appending.AppendingCodec> | Lucene.Net.Codecs.dll |
 | `SimpleText` | <xref:Lucene.Net.Codecs.SimpleText.SimpleTextCodec> | Lucene.Net.Codecs.dll |

> [!NOTE]
> The codecs in Lucene.Net.Codecs.dll are only loaded if referenced in the calling project.

#### Postings Formats

These are the types registered by the <xref:Lucene.Net.Codecs.DefaultPostingsFormatFactory> by default.

 | Name | Type | Assembly |
 | ---- | ---- | -------- |
 | `Lucene41` | <xref:Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat> | Lucene.Net.dll |
 | `Lucene40` | <xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat> | Lucene.Net.dll |
 | `SimpleText` | <xref:Lucene.Net.Codecs.SimpleText.SimpleTextPostingsFormat> | Lucene.Net.Codecs.dll |
 | `Pulsing41` | <xref:Lucene.Net.Codecs.Pulsing.Pulsing41PostingsFormat> | Lucene.Net.Codecs.dll |
 | `Direct` | <xref:Lucene.Net.Codecs.Memory.DirectPostingsFormat> | Lucene.Net.Codecs.dll |
 | `FSTOrd41` | <xref:Lucene.Net.Codecs.Memory.FSTOrdPostingsFormat> | Lucene.Net.Codecs.dll |
 | `FSTOrdPulsing41` | <xref:Lucene.Net.Codecs.Memory.FSTOrdPulsing41PostingsFormat> | Lucene.Net.Codecs.dll |
 | `FST41` | <xref:Lucene.Net.Codecs.Memory.FSTPostingsFormat> | Lucene.Net.Codecs.dll |
 | `FSTPulsing41` | <xref:Lucene.Net.Codecs.Memory.FSTPulsing41PostingsFormat> | Lucene.Net.Codecs.dll |
 | `Memory` | <xref:Lucene.Net.Codecs.Memory.MemoryPostingsFormat> | Lucene.Net.Codecs.dll |
 | `BloomFilter` | <xref:Lucene.Net.Codecs.Bloom.BloomFilteringPostingsFormat> | Lucene.Net.Codecs.dll |

> [!NOTE]
> The postings formats in Lucene.Net.Codecs.dll are only loaded if referenced in the calling project.

#### Doc Values Formats

These are the types registered by the <xref:Lucene.Net.Codecs.DefaultDocValuesFormatFactory> by default.

 | Name | Type | Assembly |
 | ---- | ---- | -------- |
 | `Lucene45` | <xref:Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat> | Lucene.Net.dll |
 | `Lucene42` | <xref:Lucene.Net.Codecs.Lucene42.Lucene42DocValuesFormat> | Lucene.Net.dll |
 | `Lucene40` | <xref:Lucene.Net.Codecs.Lucene40.Lucene40DocValuesFormat> | Lucene.Net.dll |
 | `SimpleText` | <xref:Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesFormat> | Lucene.Net.Codecs.dll |
 | `Direct` | <xref:Lucene.Net.Codecs.Memory.DirectDocValuesFormat> | Lucene.Net.Codecs.dll |
 | `Memory` | <xref:Lucene.Net.Codecs.Memory.MemoryDocValuesFormat> | Lucene.Net.Codecs.dll |
 | `Disk` | <xref:Lucene.Net.Codecs.DiskDV.DiskDocValuesFormat> | Lucene.Net.Codecs.dll |

> [!NOTE]
> The doc values formats in Lucene.Net.Codecs.dll are only loaded if referenced in the calling project.