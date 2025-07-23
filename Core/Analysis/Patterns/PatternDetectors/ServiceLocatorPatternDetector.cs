using UnityIntelligenceMCP.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors
{
    public class ServiceLocatorPatternDetector : IUnityPatternDetector
    {
        public string PatternName => "ServiceLocator";
        public float Confidence => 0.87f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool isServiceLocator = script.ClassName.Contains("Service") && 
                                   (script.ClassName.Contains("Locator") ||
                                    script.ClassName.Contains("Provider"));
            return Task.FromResult(isServiceLocator);
        }
    }
}
