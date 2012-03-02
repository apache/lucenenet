namespace Lucene.Net
{
    /// <summary>
    /// Support for junit.framework.TestCase.getName().
    /// {{Lucene.Net-2.9.1}} Move to another location after LUCENENET-266
    /// </summary>
    public class TestCase
    {
        public static string GetName()
        {
            return GetTestCaseName(false);
        }

        public static string GetFullName()
        {
            return GetTestCaseName(true);
        }

        static string GetTestCaseName(bool fullName)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                System.Reflection.MethodBase method = stackTrace.GetFrame(i).GetMethod();
                object[] testAttrs = method.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false);
                if (testAttrs != null && testAttrs.Length > 0)
                    if (fullName) return method.DeclaringType.FullName + "." + method.Name;
                    else return method.Name;
            }
            return "GetTestCaseName[UnknownTestMethod]";
        }
    }
}