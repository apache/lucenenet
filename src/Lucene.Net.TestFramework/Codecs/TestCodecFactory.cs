using System;
using System.Reflection;

namespace Lucene.Net.Codecs
{
    /// <summary>
    /// LUCENENET specific class used to add the codecs from the test framework
    /// </summary>
    public class TestCodecFactory : DefaultCodecFactory
    {
        public TestCodecFactory()
        {
            base.ScanForCodecs(this.GetType().GetTypeInfo().Assembly);
        }

        protected override bool IsServiceType(Type type)
        {
            return base.IsServiceType(type) &&
                !type.Name.Equals("RandomCodec", StringComparison.Ordinal);
        }
    }
}
