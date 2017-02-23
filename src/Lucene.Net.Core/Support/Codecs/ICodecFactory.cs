namespace Lucene.Net.Codecs
{
    /// <summary>
    /// LUCENENET specific contract for extending the functionality of <see cref="Codec"/> implementations so
    /// they can be injected with dependencies.
    /// <para/>
    /// To set the <see cref="ICodecFactory"/>, call <see cref="Codec.SetCodecFactory(ICodecFactory)"/>.
    /// </summary>
    /// <seealso cref="DefaultCodecFactory"/>
    public interface ICodecFactory
    {
        /// <summary>
        /// Gets the <see cref="Codec"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Codec"/> instance to retrieve.</param>
        /// <returns>The <see cref="Codec"/> instance.</returns>
        Codec GetCodec(string name);
    }
}
