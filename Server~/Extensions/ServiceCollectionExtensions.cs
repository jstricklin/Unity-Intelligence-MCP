using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Analysis;
using UnityIntelligenceMCP.Core.Analysis.Dependencies;
using UnityIntelligenceMCP.Core.Analysis.Patterns;
using UnityIntelligenceMCP.Core.Analysis.Project;
using UnityIntelligenceMCP.Core.Analysis.Relationships;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Core.Data.Infrastructure;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Core.RoslynServices;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Resources;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Utilities;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Core.Data.Services;
using UnityIntelligenceMCP.Core.Services;
using System.Data.Common;
using UnityIntelligenceMCP.Core.Services.Contracts;

namespace UnityIntelligenceMCP.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreUnityServices(this IServiceCollection services)
        {
            services.AddSingleton<IToolUsageLogger, DuckDbToolUsageLogger>();
            services.AddSingleton<UnityInstallationService>();
            services.AddSingleton<UnityDocumentationResource>();

            return services;
        }

        public static IServiceCollection AddUnityAnalysisServices(this IServiceCollection services)
        {
            // Core Infrastructure
            services.AddSingleton<UnityRoslynAnalysisService>();

            // Static Code Analysis Services
            services.AddSingleton<IUnityStaticAnalysisService, UnityStaticAnalysisService>();
            services.AddSingleton<UnityProjectAnalyzer>();
            services.AddSingleton<UnityPatternAnalyzer>();
            services.AddSingleton<PatternDetectorRegistry>();
            services.AddSingleton<UnityComponentRelationshipAnalyzer>();
            services.AddSingleton<UnityDependencyGraphAnalyzer>();
            services.AddSingleton<PatternMetricsAnalyzer>();
            services.AddSingleton<IUnityMessageAnalyzer, UnityMessageAnalyzer>();

            return services;
        }
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
        {
            services.AddSingleton<IApplicationDatabase, DuckDbApplicationDatabase>();
            services.AddSingleton<IDuckDbConnectionFactory, DuckDbConnectionFactory>();
            services.AddSingleton<IDbWorkQueue, DbWorkQueue>();
            services.AddHostedService<QueuedDbWriterService>();
            services.AddSingleton<IDocumentationRepository, DocumentationRepository>();
            return services;
        }
        public static IServiceCollection AddUnityDocumentationServices(this IServiceCollection services)
        {

            // New Semantic Search and Documentation Services
            // services.AddSingleton<IEmbeddingService, PlaceholderEmbeddingService>(); // Using placeholder for now
            services.AddSingleton<IEmbeddingService>(sp => new AllMiniLMEmbeddingService(Environment.ProcessorCount));
            services.AddSingleton<ISemanticSearchService, SemanticSearchService>();
            services.AddSingleton<DocumentationOrchestrationService>();
            services.AddSingleton<UnityDocumentationParser>();
            services.AddSingleton<IDocumentChunker, UnityDocumentChunker>();
            services.AddSingleton<DocumentationIndexingService>();

            return services;
        }
        public static IServiceCollection AddEditorBridgeServices(this IServiceCollection services)
        {
            services.AddHostedService<EditorBridgeClientService>();
            services.AddSingleton<IMessageHandler, MessageHandler>();
            return services;
        }

        public static async Task InitializeServicesAsync(this IServiceProvider serviceProvider)
        {
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var configService = provider.GetRequiredService<ConfigurationService>();
            var unityService = provider.GetRequiredService<UnityInstallationService>();
            var unityVersion = unityService.GetEditorVersion() ?? "unknown";

            // Initialize database
            var db = provider.GetRequiredService<IApplicationDatabase>();
            await db.InitializeDatabaseAsync(unityVersion);
            
            var indexingService = provider.GetService<DocumentationIndexingService>();
            if (indexingService != null)
            {
                var forceReindex = configService.UnitySettings.FORCE_REINDEX;
                await indexingService.IndexDocumentationIfRequiredAsync(forceReindex);
            } else {
                return;
            }
        }
    }
}
