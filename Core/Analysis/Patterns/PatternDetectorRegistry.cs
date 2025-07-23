using UnityIntelligenceMCP.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns
{
    public class PatternDetectorRegistry
    {
        public IEnumerable<IUnityPatternDetector> GetAllDetectors() => new List<IUnityPatternDetector>
        {
            new SingletonPatternDetector(),
            new ObjectPoolPatternDetector(),
            new CoroutinePatternDetector(),
            new ScriptableObjectPatternDetector(),
            new UnityEventPatternDetector(),
            new GenericEventPatternDetector(),
            new StateMachinePatternDetector(),
            new ServiceLocatorPatternDetector()
        };
    }

    public interface IUnityPatternDetector
    {
        string PatternName { get; }
        float Confidence { get; }
        Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken);
    }
}
