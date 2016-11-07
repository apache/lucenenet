using NUnit.Framework;

namespace Lucene.Net.Attributes
{
    /// <summary>
    /// Indicates that this test requires XML DTD Processing. Currently is
    /// not supported in .NET Standard but will come back in .NET Standard 2.0.
    /// https://github.com/dotnet/corefx/issues/4376.
    /// This feature is compiled out with preprocessor FEATURE_DTD_PROCESSING in:
    /// <see cref="Lucene.Net.Analysis.Compound.Hyphenation.HyphenationTree.LoadPatterns(System.IO.Stream, System.Text.Encoding)"/>
    /// <see cref="Lucene.Net.Analysis.Compound.Hyphenation.PatternParser.GetXmlReaderSettings()"/>
    /// </summary>
    public class DtdProcessingTestAttribute : CategoryAttribute
    {
        public DtdProcessingTestAttribute() : base("DtdProcessingTest")
        {
        }
    }
}
