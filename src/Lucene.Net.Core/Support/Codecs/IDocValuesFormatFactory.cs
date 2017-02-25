namespace Lucene.Net.Codecs
{
    /// <summary>
    /// LUCENENET specific contract for extending the functionality of <see cref="DocValuesFormat"/> implementations so
    /// they can be injected with dependencies.
    /// <para/>
    /// To set the <see cref="IDocValuesFormatFactory"/>, call <see cref="DocValuesFormat.SetDocValuesFormatFactory(IDocValuesFormatFactory)"/>.
    /// </summary>
    /// <seealso cref="DefaultDocValuesFormatFactory"/>
    public interface IDocValuesFormatFactory
    {
        /// <summary>
        /// Gets the <see cref="DocValuesFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="DocValuesFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="DocValuesFormat"/> instance.</returns>
        DocValuesFormat GetDocValuesFormat(string name);
    }
}
