using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Effects
{
    /// <summary>
    /// One-shot effects: a puff of dust where a unit dies, an explosion and a dust cloud where a building falls.
    /// They play on the game clock, so a paused game freezes them too. Renderers are pooled.
    /// </summary>
    public sealed class EffectSpawner : MonoBehaviour
    {
        // How big the effects are next to the building, per footprint cell.
        private const float ExplosionScalePerCell = 0.45f;
        private const float BuildingDustScalePerCell = 0.9f;

        // Clearing a full sandbox map would start hundreds at once. Past this many, new ones are skipped.
        private const int MaxPlaying = 48;

        [Tooltip("Hidden template copied for each effect.")]
        [SerializeField] private SpriteRenderer effectTemplate;
        [SerializeField, Min(1f)] private float framesPerSecond = 14f;

        [Header("Frames")]
        [SerializeField] private Sprite[] unitDeathFrames;
        [SerializeField] private Sprite[] buildingExplosionFrames;
        [SerializeField] private Sprite[] buildingDustFrames;

        [Header("Draw order")]
        [Tooltip("Above units and roofs, like arrows.")]
        [SerializeField] private int explosionSortingOrder = 145;

        private sealed class PlayingEffect
        {
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public float StartedAt;
        }

        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<PlayingEffect> _playing = new();
        private readonly Stack<SpriteRenderer> _free = new();
        private GameClock _clock;
        private GridMap _gridMap;

        [Inject]
        private void Construct(IEventBus eventBus, GameClock clock, GridMap gridMap)
        {
            _clock = clock;
            _gridMap = gridMap;
            eventBus.Subscribe<UnitDiedEvent>(e => OnUnitDied(e.Unit)).AddTo(_subscriptions);
            eventBus.Subscribe<BuildingDestroyedEvent>(e => OnBuildingDestroyed(e.Building)).AddTo(_subscriptions);
            effectTemplate.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnUnitDied(UnitModel unit)
        {
            // Around the body, a little above the feet.
            Play(unitDeathFrames, unit.Position + Vector2.up * 0.35f, 1f, effectTemplate.sortingOrder);
        }

        private void OnBuildingDestroyed(BuildingModel building)
        {
            var area = building.Area;
            var bottomCenter = _gridMap.AreaToWorldBottomCenter(area);
            var size = Mathf.Max(area.width, area.height);
            Play(buildingDustFrames, bottomCenter + Vector2.up * 0.5f, size * BuildingDustScalePerCell, effectTemplate.sortingOrder);
            Play(buildingExplosionFrames, bottomCenter + Vector2.up * (area.height * 0.6f), size * ExplosionScalePerCell, explosionSortingOrder);
        }

        private void Play(Sprite[] frames, Vector2 position, float scale, int sortingOrder)
        {
            if (frames == null || frames.Length == 0 || _playing.Count >= MaxPlaying)
                return;

            var effectRenderer = _free.Count > 0 ? _free.Pop() : Instantiate(effectTemplate, transform);
            effectRenderer.gameObject.SetActive(true);
            effectRenderer.transform.position = position;
            effectRenderer.transform.localScale = Vector3.one * scale;
            effectRenderer.sortingOrder = sortingOrder;
            effectRenderer.sprite = frames[0];
            _playing.Add(new PlayingEffect { Renderer = effectRenderer, Frames = frames, StartedAt = _clock.ElapsedTime });
        }

        private void Update()
        {
            // Backwards, so finished effects can go back to the pool on the way.
            for (var i = _playing.Count - 1; i >= 0; i--)
            {
                var effect = _playing[i];
                var frame = Mathf.FloorToInt((_clock.ElapsedTime - effect.StartedAt) * framesPerSecond);
                if (frame < effect.Frames.Length)
                {
                    effect.Renderer.sprite = effect.Frames[frame];
                    continue;
                }

                effect.Renderer.gameObject.SetActive(false);
                _free.Push(effect.Renderer);
                _playing.RemoveAt(i);
            }
        }
    }
}
