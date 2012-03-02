namespace Lucene.Net.Support
{
    class CollisionTester
    {
        int id;
        int hashCode;

        public CollisionTester(int id, int hashCode)
        {
            this.id = id;
            this.hashCode = hashCode;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is CollisionTester)
            {
                return this.id == ((CollisionTester)obj).id;
            }
            else
                return base.Equals(obj);
        }
    }
}