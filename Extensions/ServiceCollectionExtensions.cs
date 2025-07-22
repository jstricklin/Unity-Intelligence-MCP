using Microsoft.Extensions.DependencyInjection;
using UnityCodeIntelligence.Core.RoslynServices;
using UnityCodeIntelligence.Core.Analysis.Patterns;
using UnityCodeIntelligence.Core.Analysis.Project;
using UnityCodeIntelligence.Core.Analysis.Relationships;

namespace UnityCodeIntelligence.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUnityAnalysisServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<UnityRoslynAnalysisService>()
                .AddSingleton<PatternDetectorRegistry>()
                .AddSingleton<UnityComponentRelationshipAnalyzer>()
                .AddSingleton<UnityProjectAnalyzer>()
                .AddSingleton<UnityPatternAnalyzer>()
                .AddSingleton<PatternMetricsAnalyzer>();
        }
    }
}
