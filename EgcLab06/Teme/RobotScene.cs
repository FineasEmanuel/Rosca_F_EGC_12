// Varianta A: bratul robotic (inclusiv bonusul de prindere si mutare).
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class RobotScene : Scene
{
    private const string Left = "robot: baza stanga", Right = "robot: baza dreapta";
    private const string Up = "robot: umar sus", Down = "robot: umar jos";
    private const string Bend = "robot: cot indoire", Extend = "robot: cot intindere";
    private const string Grip = "robot: cleste", Save = "robot: salveaza poza";
    private const string Play = "robot: reda pozele", EaseMode = "robot: schimba easing", Clear = "robot: sterge pozele";
    private SceneObject _base = null!, _shoulder = null!, _elbow = null!, _hand = null!, _fingerL = null!, _fingerR = null!, _cargo = null!;
    private Vector3 _angles = new(0, -35, -45);
    private readonly KeyframeTrack _poses = new(1.5f) { Ease = Easing.EaseInOut };
    private readonly Tween _grip = new(new Vector3(0.48f,0,0), new Vector3(0.48f,0,0), 0.35f, Easing.EaseInOut);
    private bool _closed, _playing, _held;
    private int _ease = 3;
    private float _time;
    public override string Name => "Tema A - Brat robotic";
    public override string Status => $"Baza {_angles.X:F0} | Umar {_angles.Y:F0} | Cot {_angles.Z:F0} | Poze {_poses.Count} | {(_playing ? "redare" : "manual")} | {(_held ? "obiect prins" : _closed ? "cleste inchis" : "cleste deschis")}";
    public override IEnumerable<string> ActionNames => new[] { Left, Right, Up, Down, Bend, Extend, Grip, Save, Play, EaseMode, Clear };
    public override void Load(Input input)
    {
        CameraPosition = new Vector3(9, 7, 12);
        CameraTarget = new Vector3(0, 2.5f, 0);
        input.Bind(Left, Keys.A, Keys.Left); input.Bind(Right, Keys.D, Keys.Right);
        input.Bind(Up, Keys.W, Keys.Up); input.Bind(Down, Keys.S, Keys.Down);
        input.Bind(Bend, Keys.Z); input.Bind(Extend, Keys.X);
        input.Bind(Grip, Keys.Space); input.Bind(Save, Keys.K); input.Bind(Play, Keys.Enter);
        input.Bind(EaseMode, Keys.E); input.Bind(Clear, Keys.Backspace);
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh cylinder = AddMesh(Shapes.ShadedCylinder(0.5f, 1f, 32));
        Mesh sphere = AddMesh(Shapes.ShadedSphere(1f, 24, 12));
        AddObject("podea", AddMesh(Shapes.Grid(8, 1f, new Color4(0.3f,0.35f,0.4f,1f))));
        var steel = new Color4(0.35f,0.45f,0.55f,1f);
        var orange = new Color4(1f,0.55f,0.12f,1f);
        var dark = new Color4(0.15f,0.2f,0.25f,1f);
        Part("soclu", cylinder, null, new Vector3(0,0.25f,0), new Vector3(2.5f,0.5f,2.5f), dark);
        _base = Pivot("baza", null, new Vector3(0,0.5f,0));
        Part("platforma rotativa", cylinder, _base, new Vector3(0,0.15f,0), new Vector3(1.8f,0.3f,1.8f), steel);
        _shoulder = Pivot("umar", _base, new Vector3(0,0.55f,0));
        Part("articulatie umar", sphere, _shoulder, Vector3.Zero, new Vector3(0.5f), steel);
        Part("segment superior", cube, _shoulder, new Vector3(0,1.25f,0), new Vector3(0.65f,2.5f,0.65f), orange);
        _elbow = Pivot("cot", _shoulder, new Vector3(0,2.5f,0));
        Part("articulatie cot", sphere, _elbow, Vector3.Zero, new Vector3(0.43f), steel);
        Part("antebrat", cube, _elbow, new Vector3(0,1f,0), new Vector3(0.48f,2f,0.48f), orange);
        _hand = Pivot("cleste", _elbow, new Vector3(0,2f,0));
        Part("palma", cube, _hand, Vector3.Zero, new Vector3(1.15f,0.28f,0.5f), steel);
        _fingerL = Part("deget stang", cube, _hand, new Vector3(-0.48f,0.46f,0), new Vector3(0.16f,0.75f,0.45f), dark);
        _fingerR = Part("deget drept", cube, _hand, new Vector3(0.48f,0.46f,0), new Vector3(0.16f,0.75f,0.45f), dark);
        _cargo = Part("obiect transportabil", cube, null, new Vector3(4,0.25f,0), new Vector3(0.5f), new Color4(0.2f,0.85f,0.5f,1f));
        ApplyPose();
    }
    private SceneObject Pivot(string name, SceneObject? parent, Vector3 position)
    {
        var p = AddObject(name, null, parent); p.Transform.Position = position; return p;
    }
    private SceneObject Part(string name, Mesh mesh, SceneObject? parent, Vector3 pos, Vector3 scale, Color4 color)
    {
        var p = AddObject(name, mesh, parent); p.Transform.Position = pos; p.Transform.Scale = scale; p.Tint = color; return p;
    }
    private void ApplyPose()
    {
        _base.Transform.Rotation = new Vector3(0,_angles.X,0);
        _shoulder.Transform.Rotation = new Vector3(0,0,_angles.Y);
        _elbow.Transform.Rotation = new Vector3(0,0,_angles.Z);
    }
    public override void Update(float dt, Input input)
    {
        if (input.WasPressed(Save)) _poses.Add(_angles);
        if (input.WasPressed(Clear)) { _poses.Clear(); _playing = false; _time = 0; }
        if (input.WasPressed(Play) && _poses.Count > 0) { _playing = !_playing; _time = 0; }
        if (input.WasPressed(EaseMode)) { _ease = (_ease + 1) % Easing.All.Length; _poses.Ease = Easing.All[_ease].Function; }
        if (input.WasPressed(Grip))
        {
            _closed = !_closed;
            _grip.GoTo(new Vector3(_closed ? 0.32f : 0.48f,0,0), 0.35f);
        }
        if (dt <= 0) return;
        if (_playing) { _time += dt; _angles = _poses.Sample(_time); }
        else
        {
            _angles.X += input.Axis(Left, Right) * 75f * dt;
            _angles.Y = Math.Clamp(_angles.Y + input.Axis(Down, Up) * 65f * dt, -110f, 80f);
            _angles.Z = Math.Clamp(_angles.Z + input.Axis(Bend, Extend) * 75f * dt, -150f, 150f);
        }
        ApplyPose();
        _grip.Update(dt);
        float gap = _grip.Value.X;
        _fingerL.Transform.Position = new Vector3(-gap,0.46f,0);
        _fingerR.Transform.Position = new Vector3(gap,0.46f,0);
        Vector3 gripCenter = (Matrix4.CreateTranslation(0,0.46f,0) * _hand.WorldMatrix).Row3.Xyz;
        if (_closed && _grip.IsFinished && !_held && (_cargo.WorldPosition - gripCenter).Length < 0.65f)
        {
            _cargo.Parent = _hand; _cargo.Transform.Position = new Vector3(0,0.46f,0); _held = true;
        }
        if (!_closed && _held && _grip.IsFinished)
        {
            Vector3 pos = _cargo.WorldPosition; _cargo.Parent = null; _cargo.Transform.Position = pos; _held = false;
        }
        if (!_held && _cargo.Transform.Position.Y > 0.25f)
        {
            Vector3 p = _cargo.Transform.Position; p.Y = MathF.Max(0.25f,p.Y - 3f * dt); _cargo.Transform.Position = p;
        }
    }
    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return Status;
        yield return $"Easing: {Easing.All[_ease].Name}; interval chei: {_poses.SecondsPerKey:F1} s";
        yield return "Bonus: apropiati clestele de cubul verde, inchideti cu Space, mutati bratul si deschideti.";
    }
}
