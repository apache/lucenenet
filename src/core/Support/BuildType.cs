namespace Lucene.Net.Support
{
    public class BuildType
    {
#if DEBUG
        public static bool Debug = true;
#else
        public static bool Debug = false;
#endif
    }
}