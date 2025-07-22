using UnityCodeIntelligence.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityCodeIntelligence.Core.Analysis.Patterns.PatternDetectors
{
    public class ObjectPoolPatternDetector : IUnityPatternDetector
    {
        public string PatternName => "ObjectPool";
        public float Confidence => 0.92f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool isObjectPool = script.ClassName.Contains("Pool") && 
                               (script.ClassName.Contains("Manager") || 
                                script.ClassName.Contains("System"));
            return Task.FromResult(isObjectPool);
        }
    }
}
