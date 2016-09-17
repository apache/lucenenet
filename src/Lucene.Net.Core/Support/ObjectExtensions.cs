using System.Collections;

namespace Lucene.Net.Support
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Compares the current value against the other value to determine if
        /// the values are equal. If the values implement IEnumerable (and are 
        /// not strings), then it will enumerate over the values and compare them
        /// in the same order.
        /// 
        /// This differs from most SetEquals implementations in that they usually
        /// don't check the order of the elements in the IEnumerable, but this one does.
        /// It also does the check in a safe manner in case the IEnumerable type is nullable,
        /// so that null == null.
        /// 
        /// The values that are provided don't necessarily have to implement IEnumerable 
        /// to check their values for equality.
        /// 
        /// This method is most useful for assertions and testing, but it may 
        /// also be useful in other scenarios. Do note the IEnumerable values are cast to
        /// object before comparing, so it may not be ideal for production scenarios 
        /// if the values are not reference types.
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="a">This object</param>
        /// <param name="b">The object that this object will be compared against</param>
        /// <returns><c>true</c> if the values are equal; otherwise <c>false</c></returns>
        public static bool ValueEquals<T>(this T a, T b)
        {
            if (a is IEnumerable && b is IEnumerable)
            {
                var iter = (b as IEnumerable).GetEnumerator();
                foreach (object value in a as IEnumerable)
                {
                    iter.MoveNext();
                    if (!object.Equals(value, iter.Current))
                    {
                        return false;
                    }
                }
                return true;
            }

            return a.Equals(b);
        }

        // Special case: strings are IEnumerable, but the default Equals works fine
        // for testing value equality
        public static bool ValueEquals(this string a, string b)
        {
            return a.Equals(b);
        }
    }
}
