using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>Fires an arrow for every ranged attack. Arrows are pooled, since archers shoot all the time.</summary>
    public sealed class ProjectileSpawner : MonoBehaviour
    {
        // Arrows leave from the archer's chest, not its feet.
        private static readonly Vector2 MuzzleOffset = new(0f, 0.5f);

        [SerializeField] private ProjectileView projectilePrefab;

        private readonly CompositeDisposable _subscriptions = new();
        private ObjectPool<ProjectileView> _pool;

        [Inject]
        private void Construct(IEventBus eventBus)
        {
            eventBus.Subscribe<UnitAttackedEvent>(OnUnitAttacked).AddTo(_subscriptions);
        }

        private void Awake()
        {
            _pool = new ObjectPool<ProjectileView>(
                () => Instantiate(projectilePrefab, transform),
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => Destroy(view.gameObject));
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            _pool.Dispose();
        }

        private void OnUnitAttacked(UnitAttackedEvent attackedEvent)
        {
            if (!attackedEvent.Attacker.Definition.IsRanged)
                return;

            _pool.Get().Launch(attackedEvent.Attacker.Position + MuzzleOffset, attackedEvent.AimPoint, _pool.Release);
        }
    }
}
