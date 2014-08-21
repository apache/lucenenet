

namespace Lucene.Net.Support
{
    using System;
    using System.Runtime.InteropServices.ComTypes;

    public static class ArrayExtensionMethods
    {

        public static T[] Copy<T>(this T[] array, int length = -1)
        {
            if (length == -1)
                length = array.Length;

            var result = new T[length];
            Array.Copy(array, result, length);
            return result;
        }
    }
}
