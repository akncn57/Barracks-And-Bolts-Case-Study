using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.Core.Bootstrap
{
    public sealed class BootstrapLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootstrapFlow>();
        }
    }
}
