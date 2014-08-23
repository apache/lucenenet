using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Java.Util
{
    public class SortTestClass : TestClass
    {

        protected void AssertSort<T>(IList<T> list, int start = 0, int count = -1) where T : IComparable<T>
        {
            if(count == -1)
                count = list.Count;

            count.Times(i =>
            {
                if (i < start)
                   return;
                

                var current = list[i];

                if (i > start)
                {
                    var previous = list[i - 1];
                    Ok(previous.CompareTo(current) <= 0, "previous value, {0}, should be less than or equal to {1} at index {2}", previous, current, i);
                }

                if (i < (count - 2))
                {
                    var next = list[i + 1];
                    Ok(next.CompareTo(current) >= 0, "next value, {0}, should be greater than or equal to {1} at index {2}", next, current, i);
                }
            });
        }
    }
}
