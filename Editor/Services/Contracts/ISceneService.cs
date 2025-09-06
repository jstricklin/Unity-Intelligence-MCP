namespace UnityIntelligenceMCP.Editor.Services.Contracts
{
    public interface ISceneService
    {
        string FindScenePathByName(string sceneName);
        bool OpenScene(string scenePath, bool additive);
        bool CloseScene(string sceneName);
        void SaveCurrentSceneIfDirty();
    }
}
