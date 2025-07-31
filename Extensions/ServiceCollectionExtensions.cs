using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
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
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Utilities;
using UnityIntelligenceMCP.Models;

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
            services.AddSingleton<IApplicationDatabase, DuckDbApplicationDatabase>();
            services.AddSingleton<IDocumentationRepository, DocumentationRepository>();
            services.AddSingleton<IDbWorkQueue, DbWorkQueue>();
            services.AddHostedService<QueuedDbWriterService>();
            services.AddSingleton<IEmbeddingService, PlaceholderEmbeddingService>(); // Using placeholder for now
            services.AddSingleton<DocumentationOrchestrationService>();
            services.AddSingleton<UnityDocumentationParser>();
            services.AddSingleton<IDocumentChunker, UnityDocumentChunker>();
            services.AddSingleton<DocumentationIndexingService>();

            services.AddSingleton<IVectorRepository, ChromaDbRepository>();
            services.AddHttpClient<ChromaDbRepository>();
            services.AddSingleton<SemanticSearchService>();

            return services;
        }

        public static async Task InitializeServicesAsync(this IServiceProvider serviceProvider)
        {
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;

            // Initialize databases
            var db = provider.GetRequiredService<IApplicationDatabase>();
            await db.InitializeDatabaseAsync();
            var vectorDb = provider.GetRequiredService<IVectorRepository>();
            await vectorDb.InitializeChromaDbAsync();
            
            // Index documentation if it's not already present for the current version
            var configService = provider.GetRequiredService<ConfigurationService>();
            var indexingService = provider.GetRequiredService<DocumentationIndexingService>();
            var forceReindex = configService.UnitySettings.ForceDocumentationReindex;

            if (forceReindex == true)
            {
                var unityVersion = provider.GetRequiredService<UnityInstallationService>().GetProjectVersion(configService.GetConfiguredProjectPath());
                if (!string.IsNullOrEmpty(unityVersion))
                {
                    // Also delete from vector DB
                    await vectorDb.DeleteByVersionAsync(unityVersion);
                }
            }

            await indexingService.IndexDocumentationIfRequiredAsync(configService.GetConfiguredProjectPath(), forceReindex);
        }
    }
}
