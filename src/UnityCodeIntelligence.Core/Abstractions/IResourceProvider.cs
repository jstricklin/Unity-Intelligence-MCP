using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Models;

namespace UnityCodeIntelligence.Core.Abstractions;

public interface IResourceProvider
{
    Task<ResourceContent> GetResource(string uri, CancellationToken cancellationToken);
}
