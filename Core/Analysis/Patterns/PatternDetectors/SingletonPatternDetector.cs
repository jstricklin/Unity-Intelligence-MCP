using UnityCodeIntelligence.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityCodeIntelligence.Analysis.PatternDetectors
{
    public class SingletonPatternDetector : IUnityPatternDetector
    {
        public string PatternName => "Singleton";
        public float Confidence => 0.95f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool isSingleton = script.ClassName.Contains("Manager") || 
                              script.ClassName.Contains("Singleton");
            return Task.FromResult(isSingleton);
        }
    }
}
