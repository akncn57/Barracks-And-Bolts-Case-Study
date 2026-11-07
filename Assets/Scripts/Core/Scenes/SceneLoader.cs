using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BarracksAndBolts.Core.Scenes
{
    public sealed class SceneLoader
    {
        public bool IsLoading { get; private set; }

        public async UniTask LoadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            // Ignore repeated requests, e.g. a button clicked twice while the first load is running.
            if (IsLoading)
            {
                Debug.LogWarning($"Ignored loading '{sceneName}': another scene is already loading.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                throw new InvalidOperationException($"Scene '{sceneName}' is not in the build profile.");

            IsLoading = true;
            try
            {
                await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: cancellationToken);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
