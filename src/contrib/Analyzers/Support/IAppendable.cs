using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Support
{
    public interface IAppendable
    {
        IAppendable Append(char c);

        IAppendable Append(ICharSequence csq);

        IAppendable Append(ICharSequence csq, int start, int end);
    }
}