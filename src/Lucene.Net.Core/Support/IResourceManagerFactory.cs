using System;
using System.Resources;

namespace Lucene.Net.Support
{
    /// <summary>
    /// LUCENENET specific interface used to inject instances of
    /// <see cref="ResourceManager"/>. This
    /// extension point can be used to override the default behavior
    /// to, for example, retrieve resources from a persistent data store,
    /// rather than getting them from resource files.
    /// </summary>
    public interface IResourceManagerFactory
    {
        ResourceManager Create(Type resourceSource);

        void Release(ResourceManager manager);
    }
}
