using BarracksAndBolts.Popup;
using BarracksAndBolts.UI.Common;
using BarracksAndBolts.UI.Settings;
using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.UI.MainMenu
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MainMenuView>();
            builder.RegisterComponentInHierarchy<UiScaleApplier>();
            builder.RegisterComponentInHierarchy<PopupSystem>();
            builder.RegisterComponentInHierarchy<SettingsButtonView>();
            builder.Register<MainMenuSettingsContext>(Lifetime.Singleton).As<ISettingsContext>();
        }
    }
}
