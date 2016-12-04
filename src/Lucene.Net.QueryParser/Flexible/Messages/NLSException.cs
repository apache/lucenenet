namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    /// <summary>
    /// Interface that exceptions should implement to support lazy loading of messages.
    /// 
    /// For Native Language Support (NLS), system of software internationalization.
    /// 
    /// This Interface should be implemented by all exceptions that require
    /// translation
    /// </summary>
    public interface INLSException
    {
        /// <summary>
        /// an instance of a class that implements the Message interface
        /// </summary>
        IMessage MessageObject { get; }
    }
}
