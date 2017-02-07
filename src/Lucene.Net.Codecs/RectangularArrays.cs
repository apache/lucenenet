//----------------------------------------------------------------------------------------
//	Copyright © 2007 - 2014 Tangible Software Solutions Inc.
//	This class can be used by anyone provided that the copyright notice remains intact.
//
//	This class provides the logic to simulate Java rectangular arrays, which are jagged
//	arrays with inner arrays of the same length. A size of -1 indicates unknown length.
//----------------------------------------------------------------------------------------
internal static partial class RectangularArrays // LUCENENET TODO: Move to Support ?
{
    internal static long[][] ReturnRectangularInt64Array(int size1, int size2)
    {
        long[][] Array;
        if (size1 > -1)
        {
            Array = new long[size1][];
            if (size2 > -1)
            {
                for (int Array1 = 0; Array1 < size1; Array1++)
                {
                    Array[Array1] = new long[size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }
}