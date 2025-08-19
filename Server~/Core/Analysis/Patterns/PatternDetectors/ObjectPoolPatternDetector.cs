using UnityIntelligenceMCP.Models;
using System.Threading.Tasks;
using System.Threading;
using UnityIntelligenceMCP.Models.Analysis;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors
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
