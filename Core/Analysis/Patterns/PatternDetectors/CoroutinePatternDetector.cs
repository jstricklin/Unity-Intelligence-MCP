using UnityIntelligenceMCP.Models;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;

namespace UnityIntelligenceMCP.Core.Analysis.Patterns.PatternDetectors
{
    public class CoroutinePatternDetector : IUnityPatternDetector
    {
        public string PatternName => "Coroutine";
        public float Confidence => 0.93f;
        
        public Task<bool> DetectAsync(ScriptInfo script, CancellationToken cancellationToken)
        {
            if (script.ClassDeclaration is null || script.SemanticModel is null)
            {
                return Task.FromResult(false);
            }

            var ienumeratorSymbol = script.SemanticModel.Compilation.GetTypeByMetadataName(typeof(IEnumerator).FullName);
            if (ienumeratorSymbol is null)
            {
                return Task.FromResult(false);
            }

            var hasCoroutine = script.ClassDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Any(method =>
                {
                    var methodSymbol = script.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
                    return methodSymbol is not null &&
                           SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, ienumeratorSymbol);
                });

            return Task.FromResult(hasCoroutine);
        }
    }
}
