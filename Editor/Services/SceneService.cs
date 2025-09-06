using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityIntelligenceMCP.Editor.Services.Contracts;

namespace UnityIntelligenceMCP.Editor.Services
{
    public class SceneService : ISceneService
    {
        public string FindScenePathByName(string sceneName)
        {
            var sceneGuids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            if (sceneGuids.Length == 0)
            {
                return null;
            }
            return AssetDatabase.GUIDToAssetPath(sceneGuids.First());
        }

        public bool OpenScene(string scenePath, bool additive)
        {
            var openMode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(scenePath, openMode);
            return scene.IsValid();
        }

        public bool CloseScene(string sceneName, bool saveChanges)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                // To make it idempotent, we can return true if the scene is already closed.
                return true; 
            }

            if (scene.isDirty && saveChanges)
            {
                EditorSceneManager.SaveScene(scene);
            }
            
            return EditorSceneManager.CloseScene(scene, true);
        }

        public void SaveCurrentSceneIfDirty()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }
        }
    }
}
