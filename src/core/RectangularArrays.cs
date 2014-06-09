//----------------------------------------------------------------------------------------
//	Copyright © 2007 - 2013 Tangible Software Solutions Inc.
//	this class can be used by anyone provided that the copyright notice remains intact.
//
//	this class provides the logic to simulate Java rectangular arrays, which are jagged
//	arrays with inner arrays of the same length. A size of -1 indicates unknown length.
//----------------------------------------------------------------------------------------
using System.Collections;
internal static partial class RectangularArrays
{
    internal static ArrayList[][] ReturnRectangularArrayListArray(int Size1, int Size2)
    {
        ArrayList[][] Array;
        if (Size1 > -1)
        {
            Array = new ArrayList[Size1][];
            if (Size2 > -1)
            {
                for (int Array1 = 0; Array1 < Size1; Array1++)
                {
                    Array[Array1] = new ArrayList[Size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }

    internal static StateList[][] ReturnRectangularStateListArray(int Size1, int Size2)
    {
        StateList[][] Array;
        if (Size1 > -1)
        {
            Array = new StateList[Size1][];
            if (Size2 > -1)
            {
                for (int Array1 = 0; Array1 < Size1; Array1++)
                {
                    Array[Array1] = new StateList[Size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }

    internal static StateListNode[][] ReturnRectangularStateListNodeArray(int Size1, int Size2)
    {
        StateListNode[][] Array;
        if (Size1 > -1)
        {
            Array = new StateListNode[Size1][];
            if (Size2 > -1)
            {
                for (int Array1 = 0; Array1 < Size1; Array1++)
                {
                    Array[Array1] = new StateListNode[Size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }
}