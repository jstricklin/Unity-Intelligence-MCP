using UnityIntelligenceMCP.Models;
using System.Threading.Tasks;
using System.Threading;
using UnityIntelligenceMCP.Models.Analysis;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors
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
