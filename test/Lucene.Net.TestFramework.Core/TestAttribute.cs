using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net
{
    /// <summary>
    /// Summary description for TestAttribute
    /// </summary>
    public class TestAttribute : Xunit.FactAttribute
    {

        public string JavaMethodName { get; set; }

        public TestAttribute(string displayName, string javaMethodName = null, string skip = null)
        {
            
            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            this.DisplayName = displayName;
            this.Skip = skip;
            this.JavaMethodName = javaMethodName;
        }

        public TestAttribute()
        {
        }
    }
}
