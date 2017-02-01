//----------------------------------------------------------------------------------------
//	Copyright © 2007 - 2013 Tangible Software Solutions Inc.
//	this class can be used by anyone provided that the copyright notice remains intact.
//
//	this class provides the logic to simulate Java rectangular arrays, which are jagged
//	arrays with inner arrays of the same length. A size of -1 indicates unknown length.
//----------------------------------------------------------------------------------------

using Lucene.Net.Util;

internal static partial class RectangularArrays
{
    internal static int[][] ReturnRectangularIntArray(int size1, int size2)
    {
        int[][] Array;
        if (size1 > -1)
        {
            Array = new int[size1][];
            if (size2 > -1)
            {
                for (int Array1 = 0; Array1 < size1; Array1++)
                {
                    Array[Array1] = new int[size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }

    internal static BytesRef[][] ReturnRectangularBytesRefArray(int size1, int size2)
    {
        BytesRef[][] Array;
        if (size1 > -1)
        {
            Array = new BytesRef[size1][];
            if (size2 > -1)
            {
                for (int Array1 = 0; Array1 < size1; Array1++)
                {
                    Array[Array1] = new BytesRef[size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }
}
