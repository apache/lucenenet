using System.Globalization;

namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    /// <summary>
    /// Message Interface for a lazy loading.
    /// For Native Language Support (NLS), system of software internationalization.
    /// </summary>
    public interface IMessage
    {
        string Key { get; }
        object[] GetArguments();
        string GetLocalizedMessage();
        string GetLocalizedMessage(CultureInfo locale);
    }
}
