using Microsoft.Extensions.DependencyInjection;
using UnityIntelligenceMCP.Core.Analysis;
using UnityIntelligenceMCP.Core.Analysis.Dependencies;
using UnityIntelligenceMCP.Core.Analysis.Patterns;
using UnityIntelligenceMCP.Core.Analysis.Project;
using UnityIntelligenceMCP.Core.Analysis.Runtime;
using UnityIntelligenceMCP.Core.Analysis.Relationships;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Core.RoslynServices;
using UnityIntelligenceMCP.Resources;

namespace UnityIntelligenceMCP.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUnityAnalysisServices(this IServiceCollection services)
        {
            // TODO: Refactor these registrations into more logical groupings (e.g., Analysis, IO, Resources).
            return services
                .AddSingleton<UnityInstallationService>()
                .AddSingleton<UnityRoslynAnalysisService>()
                .AddSingleton<PatternDetectorRegistry>()
                .AddSingleton<UnityComponentRelationshipAnalyzer>()
                .AddSingleton<UnityDependencyGraphAnalyzer>()
                .AddSingleton<UnityProjectAnalyzer>()
                .AddSingleton<UnityPatternAnalyzer>()
                .AddSingleton<PatternMetricsAnalyzer>()
                .AddSingleton<UnityDocumentationResource>()
                .AddSingleton<IUnityMessageAnalyzer, UnityMessageAnalyzer>()
                // A concrete implementation would handle communication with a live Unity instance.
                .AddSingleton<IUnityRuntimeAnalysisService, MockUnityRuntimeService>()
                // Register the facade for all static analysis services.
                .AddSingleton<IUnityStaticAnalysisService, UnityStaticAnalysisService>();
        }
    }
}
