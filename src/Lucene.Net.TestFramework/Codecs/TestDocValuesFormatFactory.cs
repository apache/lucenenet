using System.Reflection;

namespace Lucene.Net.Codecs
{
    /// <summary>
    /// LUCENENET specific class used to add the DocValuesFormats from the test framework
    /// </summary>
    public class TestDocValuesFormatFactory : DefaultDocValuesFormatFactory
    {
        public TestDocValuesFormatFactory()
        {
            base.ScanForDocValuesFormats(this.GetType().GetTypeInfo().Assembly);
        }
    }
}
