/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

//----------------------------------------------------------------------------------------
//	Copyright � 2007 - 2013 Tangible Software Solutions Inc.
//	this class can be used by anyone provided that the copyright notice remains intact.
//
//	this class provides the logic to simulate Java rectangular arrays, which are jagged
//	arrays with inner arrays of the same length. A size of -1 indicates unknown length.
//----------------------------------------------------------------------------------------

using Lucene.Net.Util;

internal static partial class RectangularArrays
{
    internal static int[][] ReturnRectangularIntArray(int Size1, int Size2)
    {
        int[][] Array;
        if (Size1 > -1)
        {
            Array = new int[Size1][];
            if (Size2 > -1)
            {
                for (int Array1 = 0; Array1 < Size1; Array1++)
                {
                    Array[Array1] = new int[Size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }

    internal static BytesRef[][] ReturnRectangularBytesRefArray(int Size1, int Size2)
    {
        BytesRef[][] Array;
        if (Size1 > -1)
        {
            Array = new BytesRef[Size1][];
            if (Size2 > -1)
            {
                for (int Array1 = 0; Array1 < Size1; Array1++)
                {
                    Array[Array1] = new BytesRef[Size2];
                }
            }
        }
        else
            Array = null;

        return Array;
    }
}
