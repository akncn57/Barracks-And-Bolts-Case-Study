using BarracksAndBolts.Core.Scenes;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace BarracksAndBolts.Core.Bootstrap
{
    /// <summary>
    /// First thing that runs in a build. Project services are ready at this point, so it opens the main menu.
    /// </summary>
    public sealed class BootstrapFlow : IStartable
    {
        private readonly SceneLoader _sceneLoader;

        public BootstrapFlow(SceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            _sceneLoader.LoadAsync(SceneNames.MainMenu).Forget();
        }
    }
}
