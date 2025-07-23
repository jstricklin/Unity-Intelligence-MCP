using UnityIntelligenceMCP.Models;
using System.Threading.Tasks;
using System.Threading;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors
{
    public class StateMachinePatternDetector : IUnityPatternDetector
    {
        public string PatternName => "StateMachine";
        public float Confidence => 0.88f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            bool isStateMachine = script.ClassName.Contains("State") && 
                                 (script.ClassName.Contains("Machine") ||
                                  script.ClassName.Contains("Controller"));
            return Task.FromResult(isStateMachine);
        }
    }
}
