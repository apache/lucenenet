using Lucene.Net.Util;
using Lucene.Net.Analysis.Cn.Smart.Hhmm;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;


[TestFixture]
[LuceneNetSpecific]
public class DictionaryTests : LuceneTestCase
{
    private const string BigramResourceName = "Lucene.Net.Tests.Analysis.SmartCn.Resources.bigramdict.dct";

    [Test, Category("Dictionary")]
    public void TestBigramDictionary()
    {
        using var resourceStream = GetResourceStream(BigramResourceName);

        FileInfo _tempFile = CreateTempFile("bigramdict", ".dct");
        CopyStreamToFile(resourceStream, _tempFile);

        Assert.IsTrue(_tempFile.Length > 0, "Temp file is empty.");

        BigramDictionary bigramDict = BigramDictionary.GetInstance();
        bigramDict.LoadFromFile(_tempFile.FullName);

        Assert.AreEqual(10, bigramDict.GetFrequency("啊hello".AsSpan()), "Frequency for '啊hello' is incorrect.");
        Assert.AreEqual(20, bigramDict.GetFrequency("阿world".AsSpan()), "Frequency for '阿world' is incorrect.");
    }

    [Test, Category("Dictionary")]
    public void TestWordDictionaryGetInstance()
    {
        WordDictionary wordDict = WordDictionary.GetInstance();

        Assert.NotNull(wordDict, "WordDictionary.GetInstance() returned null.");

    }

    private Stream GetResourceStream(string resourceName)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        Assert.NotNull(stream, $"Resource '{resourceName}' not found!");
        Assert.IsTrue(stream.Length > 0, "Resource Stream is empty");
        return stream;
    }

    private void CopyStreamToFile(Stream stream, FileInfo file)
    {
        try
        {
            stream.Position = 0;
            using var outputStream = File.Create(file.FullName);
            stream.CopyTo(outputStream);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to copy stream to file: {ex.Message}");
        }
    }

    private new FileInfo CreateTempFile(string prefix, string extension)
    {
        string tempFileName = Path.Combine(
            Path.GetTempPath(),
            $"{prefix}_{Guid.NewGuid():N}{extension}"
        );
        return new FileInfo(tempFileName);
    }
}
