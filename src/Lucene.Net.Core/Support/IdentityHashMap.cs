namespace Lucene.Net.Support
{
    public class IdentityHashMap<TKey, TValue> : HashMap<TKey, TValue>
    {
        public IdentityHashMap()
            : base(new IdentityComparer<TKey>())
        {
        }
    }
}