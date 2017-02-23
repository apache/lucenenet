using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{
    /// <summary>
    /// LUCENENET specific contract that provides support for <see cref="Codecs.Codec.AvailableCodecs()"/>, 
    /// <see cref="Codecs.DocValuesFormat.AvailableDocValuesFormats()"/>, 
    /// and <see cref="Codecs.PostingsFormat.AvailablePostingsFormats()"/>. Implement this
    /// interface in addition to <see cref="Codecs.ICodecFactory"/>, <see cref="Codecs.IDocValuesFormatFactory"/>,
    /// or <see cref="Codecs.IPostingsFormatFactory"/> to provide optional support for the above
    /// methods when providing a custom implementation. If this interface is not supported by
    /// the corresponding factory, a <see cref="NotSupportedException"/> will be thrown from the above methods.
    /// </summary>
    public interface IServiceListable
    {
        ICollection<string> AvailableServices();
    }
}
