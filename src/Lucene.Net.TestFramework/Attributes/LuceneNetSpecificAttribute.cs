using NUnit.Framework;

namespace Lucene.Net.Attributes
{
    /// <summary>
    /// This test was added during the port to .NET to test
    /// additional factors that apply specifically to the port.
    /// In other words, apply this attribute to the test if it
    /// did not exist in Java Lucene.
    /// </summary>
    public class LuceneNetSpecificAttribute : CategoryAttribute
    {
        public LuceneNetSpecificAttribute()
            : base("LUCENENET")
        {
            // nothing to do here but invoke the base contsructor.
        }
    }
}
