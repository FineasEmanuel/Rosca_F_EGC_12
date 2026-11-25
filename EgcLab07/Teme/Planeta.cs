// Varianta C - planeta procedurală (tema pentru acasă).
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>Varianta C - planeta procedurală: relief 3D, calote polare, nori și seed animat.</summary>
public sealed class Planeta : Scene
{
    private const string NewPlanet = "Planeta nouă";
    private const string Clouds = "Norii";

    private Material _surface = null!;
    private Material _cloudMaterial = null!;
    private SceneObject _planet = null!;
    private SceneObject _clouds = null!;
    private Tween _seedTween = null!;
    private Tween _cloudTween = null!;
    private Vector3 _seed = new(2.7f, 8.1f, 1.3f);
    private bool _cloudsVisible = true;
    private readonly Random _random = new();

    public override string Name => "Tema C - Planeta";
    public override string Status => $"N = planetă nouă | Space = nori {(_cloudsVisible ? "ON" : "OFF")}";
    public override IEnumerable<string> ActionNames => new[] { "N - planetă nouă", "Space - arată/ascunde norii" };

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 0.15f, 4.6f);
        CameraTarget = Vector3.Zero;

        input.Bind(NewPlanet, Keys.N);
        input.Bind(Clouds, Keys.Space);

        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 128, 96));

        _surface = AddMaterial("planetă", "procedural.vert", "planet.frag");
        _surface.Param("uSeaLevel", 0.25f, 0.58f, 0.43f)
                .Param("uTerrainScale", 1.0f, 7.0f, 3.15f)
                .Param("uIceLatitude", 0.55f, 0.95f, 0.76f);
        _surface.Uniforms.Set("uSeed", _seed);
        _surface.Uniforms.Set("uBeachWidth", 0.045f);

        _cloudMaterial = AddMaterial("nori", "procedural.vert", "planet_clouds.frag");
        _cloudMaterial.Transparent = true;
        _cloudMaterial.Param("uCloudScale", 2.0f, 9.0f, 5.2f)
                       .Param("uCloudOpacity", 0.0f, 1.0f, 0.78f);
        _cloudMaterial.Uniforms.Set("uCloudSeed", new Vector3(13.1f, 3.7f, 9.4f));
        _cloudMaterial.Uniforms.Set("uCloudFade", 1.0f);

        _planet = AddObject("Planeta", sphere, _surface);
        _planet.Transform.Scale = new Vector3(1.62f);
        _planet.Spin = new Vector3(0.0f, 8.0f, 0.0f);

        _clouds = AddObject("Stratul de nori", sphere, _cloudMaterial);
        _clouds.Transform.Scale = new Vector3(1.655f);
        _clouds.Spin = new Vector3(0.0f, 13.0f, 0.0f);

        _seedTween = new Tween(_seed, _seed, 1.8f, Easing.EaseInOut);
        _cloudTween = Tween.Float(1.0f, 1.0f, 0.35f, Easing.EaseInOut);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);
        _seedTween.Update(dt);
        _cloudTween.Update(dt);

        _seed = _seedTween.Value;
        _surface.Uniforms.Set("uSeed", _seed);
        _cloudMaterial.Uniforms.Set("uCloudFade", _cloudTween.Scalar);

        if (input.WasPressed(NewPlanet))
        {
            Vector3 next = new(
                (float)(_random.NextDouble() * 40.0 + 1.0),
                (float)(_random.NextDouble() * 40.0 + 1.0),
                (float)(_random.NextDouble() * 40.0 + 1.0));
            _seedTween.GoTo(next, 2.2f);
            ConsolePanel.PrintMessage("Se generează o planetă nouă...");
        }

        if (input.WasPressed(Clouds))
        {
            _cloudsVisible = !_cloudsVisible;
            _cloudTween.GoTo(_cloudsVisible ? 1.0f : 0.0f, 0.45f);
            ConsolePanel.PrintMessage(_cloudsVisible ? "Norii apar" : "Norii dispar");
        }
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Seed procedural: {_seed.X:F2}, {_seed.Y:F2}, {_seed.Z:F2}";
        yield return $"Norii: {(_cloudsVisible ? "vizibili" : "ascunși")}";
        yield return "Parametri: uSeaLevel, uTerrainScale, uIceLatitude, uCloudScale, uCloudOpacity";
    }
}
