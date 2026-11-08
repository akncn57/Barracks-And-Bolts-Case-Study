using BarracksAndBolts.UI.Information;
using BarracksAndBolts.UI.Production;
using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.UI
{
    /// <summary>
    /// Child of GameplayLifetimeScope for the gameplay canvas. Lives in the UI assembly because Gameplay can't see UI types.
    /// Set Parent to GameplayLifetimeScope in the inspector; services like PlacementService come from there.
    /// Every UI view that needs injection must be registered here, or its [Inject] method never runs.
    /// </summary>
    public class GameplayUiLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<ProductionMenuView>();
            builder.RegisterComponentInHierarchy<InformationPanelView>();
        }
    }
}
