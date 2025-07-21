using UnityCodeIntelligence.Analysis;
using UnityCodeIntelligence.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityCodeIntelligence.Analysis.PatternDetectors
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
