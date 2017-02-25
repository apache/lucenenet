using Lucene.Net.Util;
using System;

namespace Lucene.Net.Codecs
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class IgnoreCodecAttribute : IgnoreServiceAttribute
    {
    }
}
