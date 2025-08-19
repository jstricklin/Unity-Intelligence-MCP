using UnityIntelligenceMCP.Models;
using System.Threading.Tasks;
using System.Threading;
using UnityIntelligenceMCP.Models.Analysis;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors
{
    public class GenericEventPatternDetector : IUnityPatternDetector
    {
        public string PatternName => "GenericEvent";
        public float Confidence => 0.85f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool usesGenericEvents = script.ClassName.Contains("Observable") || 
                                    script.ClassName.Contains("Publisher") ||
                                    script.ClassName.Contains("Subscriber");
            return Task.FromResult(usesGenericEvents);
        }
    }
}
