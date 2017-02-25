namespace Lucene.Net.Codecs
{
    /// <summary>
    /// LUCENENET specific contract for extending the functionality of <see cref="PostingsFormat"/> implementations so
    /// they can be injected with dependencies.
    /// <para/>
    /// To set the <see cref="IPostingsFormatFactory"/>, call <see cref="PostingsFormat.SetPostingsFormatFactory(IPostingsFormatFactory)"/>.
    /// </summary>
    /// <seealso cref="DefaultPostingsFormatFactory"/>
    public interface IPostingsFormatFactory
    {
        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="PostingsFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        PostingsFormat GetPostingsFormat(string name);
    }
}
