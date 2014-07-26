using System.Text;

namespace Lucene.Net.Support
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder Reverse(this StringBuilder text)
        {
            if (text.Length > 1)
            {
                int pivotPos = text.Length / 2;
                for (int i = 0; i < pivotPos; i++)
                {
                    int iRight = text.Length - (i + 1);
                    char rightChar = text[i];
                    char leftChar = text[iRight];
                    text[i] = leftChar;
                    text[iRight] = rightChar;
                }
            }

            return text;
        }
    }
}