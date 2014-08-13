/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    /// <summary>
    /// Summary description for TestAttribute
    /// </summary>
    public class TestAttribute : Xunit.FactAttribute
    {

        public string JavaMethodName { get; set; }

        public TestAttribute(string displayName, string javaMethodName = null, string skip = null)
        {
            this.DisplayName = displayName;
            this.Skip = skip;
            this.JavaMethodName = javaMethodName;
        }

	    public TestAttribute()
	    {
	    }
    }

    [TraitDiscoverer("TicketDiscoverer", "TraitExtensibility")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TicketAttribute : System.Attribute, Xunit.Sdk.ITraitAttribute
    {
        public string Ticket { get; private set; }

        public string Description { get; private set; }

         public TicketAttribute(string ticket, string description)
         {
             this.Ticket = ticket;
             this.Description = description;
         }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PerfAttribute : CategoryAttribute
    {

        public PerfAttribute()
            : base("Performance")
        {

        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NightlyAttribute : CategoryAttribute
    {

        public NightlyAttribute()
            : base("Nightly")
        {

        }
    }

    [TraitDiscoverer("CategoryDiscoverer", "TraitExtensibility")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CategoryAttribute : System.Attribute, Xunit.Sdk.ITraitAttribute
    {
        public string Name { get; private set; }

        public CategoryAttribute(string category)
        {
            this.Name = category;
        }
    }

    /// <summary>
    /// This class discovers all of the tests and test classes that have
    /// applied the Category attribute
    /// </summary>
    public class TicketDiscoverer : ITraitDiscoverer
    {
        /// <summary>
        /// Gets the trait values from the Category attribute.
        /// </summary>
        /// <param name="traitAttribute">The trait attribute containing the trait values.</param>
        /// <returns>The trait values.</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            var ctorArgs = traitAttribute.GetConstructorArguments().ToList();
            var message = "";

            if(ctorArgs.Count > 0)
                message = ctorArgs[1].ToString();
            
              
            yield return new KeyValuePair<string, string>("Ticket", ctorArgs[0].ToString());
            yield return new KeyValuePair<string, string>("Ticket Description", message);
        }
    }

    /// <summary>
    /// This class discovers all of the tests and test classes that have
    /// applied the Category attribute
    /// </summary>
    public class CategoryDiscoverer : ITraitDiscoverer
    {
        /// <summary>
        /// Gets the trait values from the Category attribute.
        /// </summary>
        /// <param name="traitAttribute">The trait attribute containing the trait values.</param>
        /// <returns>The trait values.</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            var ctorArgs = traitAttribute.GetConstructorArguments().ToList();
            yield return new KeyValuePair<string, string>("Category", ctorArgs[0].ToString());
        }
    }
}