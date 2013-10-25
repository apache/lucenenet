using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class FieldCacheSource : ValueSource
    {
        protected readonly String field;
        protected readonly IFieldCache cache = Lucene.Net.Search.FieldCache.DEFAULT;

        public FieldCacheSource(String field)
        {
            this.field = field;
        }

        public IFieldCache FieldCache
        {
            get { return cache; }
        }

        public string Field
        {
            get { return field; }
        }

        public override string Description
        {
            get { return field; }
        }

        public override bool Equals(object o)
        {
            if (!(o is FieldCacheSource)) return false;
            FieldCacheSource other = (FieldCacheSource)o;
            return this.field.Equals(other.field)
                   && this.cache == other.cache;
        }

        public override int GetHashCode()
        {
            return cache.GetHashCode() + field.GetHashCode();
        }
    }
}
