

namespace Lucene.Net
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

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

            if (ctorArgs.Count > 0)
                message = ctorArgs[1].ToString();


            yield return new KeyValuePair<string, string>("Ticket", ctorArgs[0].ToString());
            yield return new KeyValuePair<string, string>("Ticket Description", message);
        }
    }

}
