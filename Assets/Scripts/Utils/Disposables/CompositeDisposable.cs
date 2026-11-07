using System;
using System.Collections.Generic;

namespace BarracksAndBolts.Utils.Disposables
{
    /// <summary>
    /// Collects disposables (e.g. event subscriptions) so they can be released together.
    /// </summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _items = new();
        private bool _isDisposed;

        public void Add(IDisposable item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Adding to an already disposed container releases the item right away instead of leaking it.
            if (_isDisposed)
            {
                item.Dispose();
                return;
            }

            _items.Add(item);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            foreach (IDisposable item in _items)
                item.Dispose();

            _items.Clear();
        }
    }
}
