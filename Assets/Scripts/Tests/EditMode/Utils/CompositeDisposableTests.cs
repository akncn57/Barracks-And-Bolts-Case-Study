using System;
using BarracksAndBolts.Utils.Disposables;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Utils
{
    public class CompositeDisposableTests
    {
        private sealed class CountingDisposable : IDisposable
        {
            public int DisposeCount { get; private set; }
            public void Dispose() => DisposeCount++;
        }

        [Test]
        public void Dispose_DisposesAllItemsOnce()
        {
            var composite = new CompositeDisposable();
            var first = new CountingDisposable();
            var second = new CountingDisposable();
            composite.Add(first);
            composite.Add(second);

            composite.Dispose();
            composite.Dispose();

            Assert.AreEqual(1, first.DisposeCount);
            Assert.AreEqual(1, second.DisposeCount);
        }

        [Test]
        public void Add_AfterDispose_DisposesItemImmediately()
        {
            var composite = new CompositeDisposable();
            composite.Dispose();
            var item = new CountingDisposable();

            composite.Add(item);

            Assert.AreEqual(1, item.DisposeCount);
        }

        [Test]
        public void Add_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CompositeDisposable().Add(null));
        }
    }
}
