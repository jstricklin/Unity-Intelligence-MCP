using System.Collections.Concurrent;
using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models.Analysis
{
    /// <summary>
    /// Represents the structural dependency graph of a Unity project,
    /// mapping assets to the other assets they depend on.
    /// </summary>
    public class DependencyGraph
    {
        /// <summary>
        /// An adjacency list where the key is the file path of an asset (e.g., a script or a scene)
        /// and the value is a set of file paths of assets it has a direct dependency on.
        /// This is thread-safe to allow for parallel graph construction.
        /// </summary>
        public ConcurrentDictionary<string, HashSet<string>> AdjacencyList { get; } = new();

        /// <summary>
        /// Adds a directed edge to the dependency graph in a thread-safe manner.
        /// </summary>
        /// <param name="sourcePath">The file path of the asset that has the dependency.</param>
        /// <param name="dependencyPath">The file path of the asset being depended upon.</param>
        public void AddDependency(string sourcePath, string dependencyPath)
        {
            if (sourcePath == dependencyPath) return;

            var dependencies = AdjacencyList.GetOrAdd(sourcePath, _ => new HashSet<string>());
            lock (dependencies)
            {
                dependencies.Add(dependencyPath);
            }
        }
    }
}
