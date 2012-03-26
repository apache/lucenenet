using System;

namespace Lucene.Net.Index.Memory
{

    /// <summary>
    /// I think this approximation can be replaced by MashalBy.SizeOf(obj);
    /// </summary>
    internal class VM
    {
        public static int PTR =  Environment.Is64BitProcess ? 8 : 4;    

        // bytes occupied by primitive data types
        public static int BOOLEAN = 1;
        public static int BYTE = 1;
        public static int CHAR = 2;
        public static int SHORT = 2;
        public static int INT = 4;
        public static int LONG = 8;
        public static int FLOAT = 4;
        public static int DOUBLE = 8;
    
        private static readonly int LOG_PTR = (int) Math.Round(log2(PTR));
    
        /**
        * Object header of any heap allocated Java object. 
        * ptr to class, info for monitor, gc, hash, etc.
        */
        private static readonly int OBJECT_HEADER = 2*PTR; 

        private VM() {} // not instantiable

        //  assumes n > 0
        //  64 bit VM:
        //    0     --> 0*PTR
        //    1..8  --> 1*PTR
        //    9..16 --> 2*PTR
        private static int SizeOf(int n) 
        {
            return (((n-1) >> LOG_PTR) + 1) << LOG_PTR;
        }
    
        public static int SizeOfObject(int n)
        {
            return SizeOf(OBJECT_HEADER + n);        
        }
    
        public static int SizeOfObjectArray(int len) 
        {
            return SizeOfObject(INT + PTR*len);        
        }
    
        public static int SizeOfCharArray(int len) 
        {
            return SizeOfObject(INT + CHAR*len);        
        }
    
        public static int SizeOfIntArray(int len) 
        {
            return SizeOfObject(INT + INT*len);        
        }
    
        public static int SizeOfString(int len) 
        {
            return SizeOfObject(3*INT + PTR) + SizeOfCharArray(len);
        }
    
        public static int SizeOfHashMap(int len) 
        {
            return SizeOfObject(4*PTR + 4*INT) + SizeOfObjectArray(len) 
                    + len * SizeOfObject(3*PTR + INT); // entries
        }
    
        // Note: does not include referenced objects
        public static int SizeOfArrayList(int len) 
        {
            return SizeOfObject(PTR + 2*INT) + SizeOfObjectArray(len); 
        }
    
        public static int sizeOfArrayIntList(int len) 
        {
            return SizeOfObject(PTR + INT) + SizeOfIntArray(len);
        }
    
        /** logarithm to the base 2. Example: log2(4) == 2, log2(8) == 3 */
        private static double log2(double value) {
            return Math.Log(value) / Math.Log(2);
        }
        
    }
}
