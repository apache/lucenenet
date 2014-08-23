
namespace Lucene.Net.Java.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Java.Util;

    public class InsertionSortTests : SortTestClass
    {

        [Test]
        public void TestSort()
        {
            var length = Random.Next(20);
            var array = new int[length];
            Arrays.Fill(array, () => Random.Next());

            InsertionSort.Sort(array);
            
            this.AssertSort(array);

            Throws<ArgumentNullException>(() =>
            {
                int[] test = null;
                
                // ReSharper disable ExpressionIsAlwaysNull
                InsertionSort.Sort(test);
            });
        }

        [Test]
        public void TestSortWithSlice()
        {
            var offset = Random.Next(5, 10);
            var length = Random.Next(30, 35);
            var list = new char[length];

            Ok(list.Length > offset);
            Arrays.Fill(list, () => (char) Random.Next(97, 122));
           
            var copy = list.ToList();

            InsertionSort.Sort(list, offset, length - offset - 5);
 
            this.AssertSort(list, offset, length - offset - 5);
            
            // ensure what was outside of the slice was not sorted
            for (var i = 0; i < offset; i++)
            {
                Equal(list[i], copy[i]);
            }

            for (var i = length - 5; i < length; i++)
            {
                Equal(list[i], copy[i]);
            }

            var listB = new char[10];

            // verify exceptions
            Throws<ArgumentNullException>(() => InsertionSort.Sort((IList<int>)null, -1, 10));
            Throws<ArgumentOutOfRangeException>(() => InsertionSort.Sort(listB, -1, 10));
            Throws<ArgumentOutOfRangeException>(() => InsertionSort.Sort(listB, 0, -10));
            Throws<ArgumentOutOfRangeException>(() => InsertionSort.Sort(listB, 10, 10));
            Throws<ArgumentOutOfRangeException>(() => InsertionSort.Sort(listB, 0, 11));
            Throws<ArgumentOutOfRangeException>(() => InsertionSort.Sort(listB, -9, 5));

        }
    }
}