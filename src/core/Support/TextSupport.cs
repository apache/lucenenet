namespace Lucene.Net.Support
{
    public class TextSupport
    {
        /// <summary>
        /// Copies an array of chars obtained from a String into a specified array of chars
        /// </summary>
        /// <param name="sourceString">The String to get the chars from</param>
        /// <param name="sourceStart">Position of the String to start getting the chars</param>
        /// <param name="sourceEnd">Position of the String to end getting the chars</param>
        /// <param name="destinationArray">Array to return the chars</param>
        /// <param name="destinationStart">Position of the destination array of chars to start storing the chars</param>
        /// <returns>An array of chars</returns>
        public static void GetCharsFromString(string sourceString, int sourceStart, int sourceEnd, char[] destinationArray, int destinationStart)
        {
            int sourceCounter;
            int destinationCounter;
            sourceCounter = sourceStart;
            destinationCounter = destinationStart;
            while (sourceCounter < sourceEnd)
            {
                destinationArray[destinationCounter] = (char)sourceString[sourceCounter];
                sourceCounter++;
                destinationCounter++;
            }
        }
    }
}