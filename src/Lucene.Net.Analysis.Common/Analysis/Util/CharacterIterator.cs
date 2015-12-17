namespace Lucene.Net.Analysis.Util
{
    public abstract class CharacterIterator : ICharacterIterator
    {
        public static readonly char DONE = '\uFFFF';

        public abstract char First();
        public abstract char Last();
        public abstract char Current();
        public abstract char Next();
        public abstract char Previous();
        public abstract char SetIndex(int position);
        public abstract int GetBeginIndex();
        public abstract int GetEndIndex();
        public abstract int GetIndex();

        public abstract object Clone();
    }
}