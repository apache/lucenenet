using Lucene.Net.Util;

namespace Lucene.Net.JavaCompatibility
{
    /// <summary>
    /// LUCENENET specific for mimicking the JUnit rule functionality.
    /// We simplify things by just running the rules inside LuceneTestCase.
    /// </summary>
    public abstract class AbstractBeforeAfterRule
    {
        public virtual void Before(LuceneTestCase testInstance)
        {
        }

        public virtual void After(LuceneTestCase testInstance)
        {
        }
    }
}
