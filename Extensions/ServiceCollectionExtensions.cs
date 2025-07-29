using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Analysis;
using UnityIntelligenceMCP.Core.Analysis.Dependencies;
using UnityIntelligenceMCP.Core.Analysis.Patterns;
using UnityIntelligenceMCP.Core.Analysis.Project;
using UnityIntelligenceMCP.Core.Analysis.Relationships;
using UnityIntelligenceMCP.Core.Data;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Core.RoslynServices;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Resources;

namespace UnityIntelligenceMCP.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUnityAnalysisServices(this IServiceCollection services)
        {
            // Core Infrastructure
            services.AddSingleton<UnityInstallationService>();
            services.AddSingleton<UnityRoslynAnalysisService>();
            services.AddSingleton<UnityDocumentationResource>();

            // Static Code Analysis Services
            services.AddSingleton<IUnityStaticAnalysisService, UnityStaticAnalysisService>();
            services.AddSingleton<UnityProjectAnalyzer>();
            services.AddSingleton<UnityPatternAnalyzer>();
            services.AddSingleton<PatternDetectorRegistry>();
            services.AddSingleton<UnityComponentRelationshipAnalyzer>();
            services.AddSingleton<UnityDependencyGraphAnalyzer>();
            services.AddSingleton<PatternMetricsAnalyzer>();
            services.AddSingleton<IUnityMessageAnalyzer, UnityMessageAnalyzer>();

            // New Semantic Search and Documentation Services
            services.AddSingleton<IDocumentationDatabase, DuckDbApplicationDatabase>();
            services.AddSingleton<IDocumentationRepository, DocumentationRepository>();
            services.AddSingleton<IEmbeddingService, PlaceholderEmbeddingService>(); // Using placeholder for now
            services.AddSingleton<DocumentationOrchestrationService>();

            return services;
        }

        public static async Task InitializeDatabaseServicesAsync(this IServiceProvider serviceProvider)
        {
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDocumentationDatabase>();
            await db.InitializeDatabaseAsync();
        }
    }
}
