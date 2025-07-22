using UnityCodeIntelligence.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityCodeIntelligence.Core.Analysis.Patterns.PatternDetectors
{
    public class ScriptableObjectPatternDetector : IUnityPatternDetector
    {
        public string PatternName => "ScriptableObject";
        public float Confidence => 0.96f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool isScriptableObject = script.ClassName.EndsWith("SO") || 
                                     script.ClassName.Contains("Scriptable");
            return Task.FromResult(isScriptableObject);
        }
    }
}
