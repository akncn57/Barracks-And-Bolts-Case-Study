using BarracksAndBolts.UI.Common;
using BarracksAndBolts.UI.Feedback;
using BarracksAndBolts.UI.Hud;
using BarracksAndBolts.UI.Information;
using BarracksAndBolts.UI.Production;
using BarracksAndBolts.UI.Sandbox;
using BarracksAndBolts.UI.Tooltips;
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
            builder.RegisterComponentInHierarchy<UnitSelectionCardView>();
            builder.RegisterComponentInHierarchy<SandboxFactionToggleView>();
            builder.RegisterComponentInHierarchy<ResourceBarView>();
            builder.RegisterComponentInHierarchy<CalendarLabelView>();
            builder.RegisterComponentInHierarchy<DayNightOverlayView>();
            builder.RegisterComponentInHierarchy<NoticeBannerView>();
            builder.RegisterComponentInHierarchy<GameEndView>();
            builder.RegisterComponentInHierarchy<FloatingTextSpawner>();
            builder.RegisterComponentInHierarchy<HoverLabelView>();
            builder.RegisterComponentInHierarchy<TooltipView>();
            builder.RegisterComponentInHierarchy<SlideOutPanel>().As<ICloseablePanel>();
            builder.RegisterEntryPoint<EscapeInputHandler>();
        }
    }
}
