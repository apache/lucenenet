using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public static class HelperExtensions 
    {

        public static void Times(this int times, Action action)
        {
            var i = 0;
            for (; i < times; i++)
                action();
        }
    }
}
