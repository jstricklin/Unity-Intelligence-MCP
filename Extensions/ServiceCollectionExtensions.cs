using Microsoft.Extensions.DependencyInjection;
using UnityCodeIntelligence.Core.Analysis.Patterns;
using UnityCodeIntelligence.Core.Analysis.Project;
using UnityCodeIntelligence.Core.Analysis.Relationships;
using UnityCodeIntelligence.Core.IO;
using UnityCodeIntelligence.Core.RoslynServices;
using UnityCodeIntelligence.Resources;

namespace UnityCodeIntelligence.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUnityAnalysisServices(this IServiceCollection services)
        {
            // TODO: Refactor these registrations into more logical groupings (e.g., Analysis, IO, Resources).
            return services
                .AddSingleton<UnityRoslynAnalysisService>()
                .AddSingleton<UnityInstallationService>()
                .AddSingleton<PatternDetectorRegistry>()
                .AddSingleton<UnityComponentRelationshipAnalyzer>()
                .AddSingleton<UnityProjectAnalyzer>()
                .AddSingleton<UnityPatternAnalyzer>()
                .AddSingleton<PatternMetricsAnalyzer>()
                .AddSingleton<UnityDocumentationResource>();
        }
    }
}
