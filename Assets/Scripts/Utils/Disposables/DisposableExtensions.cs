using System;

namespace BarracksAndBolts.Utils.Disposables
{
    public static class DisposableExtensions
    {
        public static void AddTo(this IDisposable disposable, CompositeDisposable container)
        {
            container.Add(disposable);
        }
    }
}
