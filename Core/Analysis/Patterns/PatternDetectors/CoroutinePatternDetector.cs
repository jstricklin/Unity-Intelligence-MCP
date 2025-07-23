using UnityIntelligenceMCP.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors
{
    public class CoroutinePatternDetector : IUnityPatternDetector
    {
        public string PatternName => "Coroutine";
        public float Confidence => 0.93f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool isCoroutineUser = script.ClassName.Contains("Spawner") || 
                                  script.ClassName.Contains("Animator");
            return Task.FromResult(isCoroutineUser);
        }
    }
}
