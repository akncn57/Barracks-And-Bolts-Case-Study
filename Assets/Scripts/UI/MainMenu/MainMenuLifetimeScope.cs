using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.UI.MainMenu
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MainMenuView>();
        }
    }
}
