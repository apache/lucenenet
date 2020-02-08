//----------------------------------------------------------------------------------------
//	Copyright � 2007 - 2013 Tangible Software Solutions Inc.
//	this class can be used by anyone provided that the copyright notice remains intact.
//
//	this class provides the logic to simulate Java rectangular arrays, which are jagged
//	arrays with inner arrays of the same length. A size of -1 indicates unknown length.
//----------------------------------------------------------------------------------------

namespace Lucene.Net.Support
{
    internal static class RectangularArrays
    {
        public static T[][] ReturnRectangularArray<T>(int size1, int size2)
        {
            T[][] array;
            if (size1 > -1)
            {
                array = new T[size1][];
                if (size2 > -1)
                {
                    for (int array1 = 0; array1 < size1; array1++)
                    {
                        array[array1] = new T[size2];
                    }
                }
            }
            else
                array = null;

            return array;
        }
    }
}