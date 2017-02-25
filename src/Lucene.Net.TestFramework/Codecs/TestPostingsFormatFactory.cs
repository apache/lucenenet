using System.Reflection;

namespace Lucene.Net.Codecs
{
    /// <summary>
    /// LUCENENET specific class used to add the PostingsFormats from the test framework
    /// </summary>
    public class TestPostingsFormatFactory : DefaultPostingsFormatFactory
    {
        public TestPostingsFormatFactory()
        {
            base.ScanForPostingsFormats(this.GetType().GetTypeInfo().Assembly);
        }
    }
}
