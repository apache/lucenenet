namespace Lucene.Net.Support
{
    internal class BigObject
    {
        public int i = 0;
        public byte[] buf = null;

        public BigObject(int i)
        {
            this.i = i;
            buf = new byte[1024 * 1024]; //1MB
        }
    }
}