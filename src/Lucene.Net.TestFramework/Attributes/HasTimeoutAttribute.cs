using NUnit.Framework;

namespace Lucene.Net.Attributes
{
    /// <summary>
    /// Specifies that this test sometimes runs for a long time and may not end.
    /// For running tests in .NET Core because NUnit does not support [Timeout],
    /// so we can have tests in the .NET Core build that run forever.
    /// </summary>
    public class HasTimeoutAttribute : CategoryAttribute
    {
        public HasTimeoutAttribute() : base("HasTimeout")
        {
            // nothing to do here but invoke the base contsructor.
        }
    }
}
