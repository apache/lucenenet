using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{

    public class LuceneCategoryAttribute : NUnit.Framework.CategoryAttribute
    {
        public LuceneCategoryAttribute(string name)
            : base(name.Replace(":", " ").Replace(".", " "))
        {

        }
    }

    public class NightlyAttribute : LuceneCategoryAttribute
    {
        public const string KEY = "tests:nightly";

        public NightlyAttribute()
            : base(KEY)
        {
          
        }
    }

    public class AwaitsFixAttribute : LuceneCategoryAttribute
    {
        public const string KEY = "tests:awaitsfix";

        public AwaitsFixAttribute()
            : base(KEY)
        {

        }
    }


    public class WeeklyAttribute : LuceneCategoryAttribute
    {
        public const string KEY = "tests:weekly";

        public WeeklyAttribute()
            : base(KEY)
        {

        }
    }

    public class SlowAttribute : LuceneCategoryAttribute 
    {
        public const string KEY = "tests:slow";

        public static bool Ignore { get; set; }

        public SlowAttribute(bool enabled)
            : base(KEY)
        {
               
        }
    }
}
