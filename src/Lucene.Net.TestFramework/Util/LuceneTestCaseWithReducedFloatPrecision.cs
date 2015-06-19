using Lucene.Net.Support;

namespace Lucene.Net.Util
{
    public class LuceneTestCaseWithReducedFloatPrecision :  LuceneTestCase
    {
        public override void SetUp()
        {
            base.SetUp();

            // set precision
            FloatUtils.SetPrecision();
        }
    }
}
