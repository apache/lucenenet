using NUnit.Framework;

namespace Lucene.Net.Attributes
{
    /// <summary>
    /// This test runs long and should be skipped in the 1st run.
    /// </summary>
    public class LongRunningTestAttribute : CategoryAttribute
    {
        public LongRunningTestAttribute() : base("LongRunningTest")
        {
            // nothing to do here but invoke the base contsructor.
        }
    }
}