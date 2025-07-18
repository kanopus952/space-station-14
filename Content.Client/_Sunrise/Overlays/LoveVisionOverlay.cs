using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Aphrodesiac;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.StatusEffect;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.ContentPack;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Random;
using Robust.Shared.Input;
using Content.Client.Guidebook.Richtext;
using Robust.Client.Input;

namespace Content.Client._Sunrise.LoveVision;

public sealed class LoveVisionOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _input = default!;

    private readonly SpriteSystem _sprite;

    public override bool RequestScreenTexture => true;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _loveVisionShader;
    private readonly ShaderInstance _gradient;

    private readonly Robust.Client.Graphics.Texture _heartTexture;

    private readonly List<HeartData> _hearts = [];

    private struct HeartData
    {
        public Vector2 Position;
        public TimeSpan SpawnTime;
    }
    private float _strength = 0.0f;
    private float _timeTicker = 0.0f;
    private Vector2 _position;

    private TimeSpan _nextHeartTime;

    private readonly float _maxStrength = 15f;
    private readonly float _minStrength = 0f;

    // private float EffectScale => Math.Clamp((_strength - _minStrength) / _maxStrength, 0.0f, 1.0f);

    public LoveVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();
        _loveVisionShader = _prototypeManager.Index<ShaderPrototype>("LoveVision").InstanceUnique();
        _gradient = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        _heartTexture = _sprite.Frame0(new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Nano/lock.svg.192dpi.png")));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.HasComponent<LoveVisionComponent>(playerEntity)
            || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
            return;

        var statusSys = _sysMan.GetEntitySystem<StatusEffectsSystem>();
        if (!statusSys.TryGetTime(playerEntity.Value, LoveVisionSystem.LoveVisionKey, out var time, status))
            return;

        var duration = (float)(time.Value.Item2 - time.Value.Item1).TotalSeconds;
        var elapsedTime = _timeTicker;
        _timeTicker += args.DeltaSeconds;

        var halfDuration = duration / 2f;
        var normalizedTime = MathF.Abs(elapsedTime - halfDuration) / halfDuration; // 0 at the middle, 1 at the ends

        // Invert the normalized time to make it peak in the middle
        var peakFactor = 1f - normalizedTime;

        // Adjust strength based on peakFactor
        _strength += (peakFactor * args.DeltaSeconds);

        // Optional: Clamp _strength to a reasonable range if needed, e.g., between 0 and 1
        _strength = Math.Clamp(_strength, 0f, 1f);

        var curTime = _timing.CurTime;

        if (curTime >= _nextHeartTime)
        {
            var viewportSize = _eyeManager.GetWorldViewport();
            var random = new Random();
            var x = (float)(random.NextDouble() * viewportSize.Size.X);
            var y = (float)(random.NextDouble() * viewportSize.Size.Y);

            _hearts.Add(new HeartData
            {
                Position = new Vector2(x, y),
                SpawnTime = _timing.CurTime
            });

            var delay = _random.NextFloat() * 2.0 + 1.0; // от 1.0 до 3.0 сек
            _nextHeartTime = curTime + TimeSpan.FromSeconds(delay);
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComponent))
            return false;

        if (args.Viewport.Eye != eyeComponent.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_config.GetCVar(CCVars.ReducedMotion))
            return;

        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        var time = (float)_timing.RealTime.TotalSeconds;
        var distance = args.ViewportBounds.Width;
        _loveVisionShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _loveVisionShader?.SetParameter("effectStrength", _strength);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_loveVisionShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);

        if (_strength > 0f) // Условие, если надо
        {
            var level = _strength / 2f;
            var pulseRate = 6f;
            var adjustedTime = time * pulseRate;

            var outerMaxLevel = 2.0f * distance;
            var outerMinLevel = 0.8f * distance;
            var innerMaxLevel = 0.6f * distance;
            var innerMinLevel = 0.2f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            var pulse = MathF.Max(0f, MathF.Sin(adjustedTime));

            _gradient.SetParameter("time", pulse);
            _gradient.SetParameter("color", new Robust.Shared.Maths.Vector3(1.0f, 0.3f, 0.7f)); // Розово-фиолетовый
            _gradient.SetParameter("darknessAlphaOuter", 2f);
            _gradient.SetParameter("ratioMultiplier", 1f);

            _gradient.SetParameter("outerCircleRadius", outerRadius);
            _gradient.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            _gradient.SetParameter("innerCircleRadius", innerRadius);
            _gradient.SetParameter("innerCircleMaxRadius", innerRadius + 0.02f * distance);

            worldHandle.UseShader(_gradient);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }

        var curTime = _timing.CurTime;
        var screen = args.ScreenHandle;

        for (var i = _hearts.Count - 1; i >= 0; i--)
        {
            var heart = _hearts[i];
            var timeElapsed = curTime - heart.SpawnTime;

            var lifetime = TimeSpan.FromSeconds(1.5f);

            if (timeElapsed > lifetime)
            {
                _hearts.RemoveAt(i);
                continue;
            }

            var pos = heart.Position - new Vector2(0, 30 * timeElapsed.Seconds);

            var alpha = 1.0f - timeElapsed / lifetime;
            alpha = Math.Clamp(alpha, 0f, 1f);

            var modulate = new Color(255, 100, 180, (byte)(alpha * 255));

            screen.DrawTexture(_heartTexture, pos, modulate);
        }
    }
}
