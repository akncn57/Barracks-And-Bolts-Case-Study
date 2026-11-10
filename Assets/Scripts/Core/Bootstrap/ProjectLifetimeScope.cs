using BarracksAndBolts.Core.Scenes;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Core.Settings;
using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.Core.Bootstrap
{
    /// <summary>
    /// Root scope for services that live for the whole application.
    /// Set as Root Lifetime Scope in VContainerSettings, so it also exists when a scene is played directly.
    /// </summary>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<GameSessionSettings>(Lifetime.Singleton);
            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<PlayerPrefsSettingsStorage>(Lifetime.Singleton).As<ISettingsStorage>();
            builder.Register<GameSettings>(Lifetime.Singleton);
            builder.RegisterEntryPoint<DisplaySettingsApplier>();
        }
    }
}
