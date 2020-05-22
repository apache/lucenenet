using Lucene.Net.Codecs;
using Lucene.Net.Configuration;
using NUnit.Framework;
using System;

[SetUpFixture]
public class LuceneTestFrameworkInitializer
{
    // LUCENENET specific constants to scan the test framework for codecs/docvaluesformats/postingsformats only once
    public static ICodecFactory CodecFactory { get; set; } = new TestCodecFactory();
    public static IDocValuesFormatFactory DocValuesFormatFactory { get; set; } = new TestDocValuesFormatFactory();
    public static IPostingsFormatFactory PostingsFormatFactory { get; set; } = new TestPostingsFormatFactory();

    [CLSCompliant(false)]
    public static IConfigurationRootFactory ConfigurationFactory { get; set; } = new TestConfigurationRootFactory();

    [OneTimeSetUp]
    public void OneTimeSetUpBeforeTests()
    {
        TestFrameworkSetUp();

        try
        {
            // Setup the factories
            ConfigurationSettings.SetConfigurationRootFactory(ConfigurationFactory);
            Codec.SetCodecFactory(CodecFactory);
            DocValuesFormat.SetDocValuesFormatFactory(DocValuesFormatFactory);
            PostingsFormat.SetPostingsFormatFactory(PostingsFormatFactory);
        }
        catch (Exception ex)
        {
            // Write the stack trace so we have something to go on if an error occurs here.
            throw new Exception($"An exception occurred during OneTimeSetUpBeforeTests:\n{ex.ToString()}", ex);
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDownAfterTests()
    {
        try
        {
            TestFrameworkTearDown();
        }
        catch (Exception ex)
        {
            // Write the stack trace so we have something to go on if an error occurs here.
            throw new Exception($"An exception occurred during OneTimeTearDownAfterTests:\n{ex.ToString()}", ex);
        }
    }

    public virtual void TestFrameworkSetUp()
    {
        // Runs only once per test framework run before all tests
    }


    public virtual void TestFrameworkTearDown()
    {
        // Runs only once per test framework run after all tests
    }
}
