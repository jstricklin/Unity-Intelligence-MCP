using UnityCodeIntelligence.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityCodeIntelligence.Analysis.PatternDetectors
{
    public class UnityEventPatternDetector : IUnityPatternDetector
    {
        public string PatternName => "UnityEvent";
        public float Confidence => 0.90f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool usesUnityEvents = script.ClassName.Contains("Event") || 
                                  script.ClassName.Contains("Handler") ||
                                  script.ClassName.Contains("Listener");
            return Task.FromResult(usesUnityEvents);
        }
    }
}
