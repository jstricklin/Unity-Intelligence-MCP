using UnityEditor;
using System;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Editor.Core
{
    [DEPRECATED]
    public static class UnityThreadDispatcher
    {
        public static T Execute<T>(Func<T> action)
        {
            T result = default;
            if (TaskScheduler.Current != TaskScheduler.Default)
            {
                // Already on main thread
                return action();
            }
            // FIXME: DEADLOCK CAUSED BY TASK RESULT BEFORE TASK COMPLETION
            // Schedule to run on main thread
            var tcs = new TaskCompletionSource<T>();
            EditorApplication.delayCall += () =>
            {
                try
                {
                    result = action();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            return tcs.Task.Result;
        }
    }
}
