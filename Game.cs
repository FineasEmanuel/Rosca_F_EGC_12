using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)

    // T4: pivotul rotației (null = nu a fost ales niciunul)
    private Vector2? _pivot = null;

    // T5: satelitul (nu este în _scene, deci Tab nu îl selectează) și unghiul orbitei, în grade
    private SceneObject _satellite = null!;
    private float _orbitAngle = 0.0f;

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;   // swap-ul bufferelor așteaptă reîmprospătarea monitorului
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        // Scena: aceleași forme din laboratorul 3, acum plasate în lume prin Transform2D.
        // Depth (z) contează doar în perspectivă (tasta O, secțiunea 7.2).
        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });

        // T5: satelitul. Transformarea lui se calculează la desenare, din obiectul selectat.
        _satellite = new SceneObject("satelit", Shapes.Triangle(), new Transform2D())
        {
            Tint = new Color4(1.0f, 0.85f, 0.3f, 1.0f)
        };
    }

    /// <summary>Adaugă un obiect în scenă și reține transformarea lui inițială (pentru tasta R).</summary>
    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);      // proiecția se adaptează la noul raport de aspect
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        float dt = (float)args.Time;
        SceneObject sel = _scene[_selected];

        // T2: Shift ținut apăsat => toate mișcările obiectului (săgeți, rotație, scalare) de 3 ori mai rapide
        float k = KeyboardState.IsKeyDown(Keys.LeftShift) ? 3.0f : 1.0f;

        // translația (săgeți)
        float move = 2.0f * k * dt;   // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.Left)) sel.Transform.Position.X -= move;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += move;
        if (KeyboardState.IsKeyDown(Keys.Down)) sel.Transform.Position.Y -= move;
        if (KeyboardState.IsKeyDown(Keys.Up)) sel.Transform.Position.Y += move;

        // T2: rotația, 90 de grade pe secundă. Q = sens trigonometric (+), E = sens orar (-)
        float rot = 90.0f * k * dt;
        if (KeyboardState.IsKeyDown(Keys.Q)) sel.Transform.Rotation += rot;
        if (KeyboardState.IsKeyDown(Keys.E)) sel.Transform.Rotation -= rot;

        // T2: scalarea uniformă, factor 1,5 pe secundă. Z = micșorează, X = mărește.
        // T2: scalarea se face prin ÎNMULȚIRE, nu prin adunare: o scădere prin adunare ar ajunge la 0
        // și apoi la valori negative, care oglindesc forma. Prin înmulțire scara se apropie de 0 fără
        // să-l atingă, iar mărirea și micșorarea sunt simetrice (Pow(f, dt) = f pe secundă, indiferent de FPS).
        float f = MathF.Pow(1.5f, k * dt);
        if (KeyboardState.IsKeyDown(Keys.Z)) sel.Transform.ScaleBy(1.0f / f);
        if (KeyboardState.IsKeyDown(Keys.X)) sel.Transform.ScaleBy(f);

        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O)) _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F)) _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R)) ResetScene();

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        // Obiectul selectat poate să se fi schimbat prin Tab sau R în același cadru.
        sel = _scene[_selected];

        // T3: clic stânga mută obiectul selectat în punctul din lume aflat sub cursor (Depth nu se schimbă)
        if (MouseState.IsButtonPressed(MouseButton.Left))
            sel.Transform.Position = ScreenToWorld(MouseState.Position);

        // T4: clic dreapta stabilește pivotul în punctul din lume aflat sub cursor
        if (MouseState.IsButtonPressed(MouseButton.Right))
            _pivot = ScreenToWorld(MouseState.Position);

        // T4: cât timp P este ținută apăsată, obiectul se rotește în jurul pivotului, 90 de grade pe secundă
        if (KeyboardState.IsKeyDown(Keys.P) && _pivot.HasValue)
            sel.Transform.RotateAround(_pivot.Value, 90.0f * dt);

        // T5: o rotație completă a orbitei la 4 secunde = 90 de grade pe secundă
        _orbitAngle = (_orbitAngle + 90.0f * dt) % 360.0f;

        // --- Camera: tastatura și rotița (secțiunea 6.2) -----------------------------

        float camSpeed = 3.0f / _camera.Zoom;                    // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);   // un pas al rotiței = ±10 %
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        // --- Titlul ferestrei: obiectul selectat, proiecția, cadre pe secundă --------

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche din back buffer.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare și programul shader.
        SetWireframe(_wireframe);
        _shader.Use();

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // T5: satelitul obiectului selectat. Transformarea locală (scalare 0,3, translație (1,2; 0),
        // apoi rotație cu unghiul orbitei) se înmulțește cu matricea model a obiectului selectat
        // (în C#: copil * părinte), deci satelitul îl urmează când acesta este mutat, rotit sau scalat.
        // T5: când obiectul selectat este scalat, orbita se scalează odată cu el (raza 1,2 devine
        // 1,2 * scara obiectului, iar satelitul se mărește proporțional), pentru că matricea satelitului
        // include matricea model a părintelui, deci moștenește și scalarea lui, nu doar poziția și rotația.
        Matrix4 local = Matrix4.CreateScale(0.3f, 0.3f, 1.0f)
                      * Matrix4.CreateTranslation(1.2f, 0.0f, 0.0f)
                      * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(_orbitAngle));
        Matrix4 satelliteModel = local * _scene[_selected].Transform.ModelMatrix;

        _shader.SetMatrix4("uModel", satelliteModel);
        _shader.SetColor("uTint", _satellite.Tint);
        _satellite.Mesh.Draw();

        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _satellite.Dispose();
        _shader.Dispose();

        base.OnUnload();
    }

    /// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        // T3, pasul 1: pixel -> NDC.
        // T3: axa Y a mouse-ului are originea sus-stânga și crește în jos, iar în NDC și în lume Y crește
        // în sus; de aceea y_ndc = 1 - 2 * y / H (semnul minus întoarce axa).
        float xNdc = 2.0f * mousePixels.X / ClientSize.X - 1.0f;
        float yNdc = 1.0f - 2.0f * mousePixels.Y / ClientSize.Y;

        // T3, pasul 2: NDC -> lume, cu inversa lui view * projection (recalculată la fiecare apel,
        // ca să țină cont de mutarea camerei, de zoom și de redimensionarea ferestrei).
        Matrix4 inverse = (_camera.ViewMatrix * _camera.ProjectionMatrix).Inverted();
        Vector4 world = new Vector4(xNdc, yNdc, 0.0f, 1.0f) * inverse;   // OpenTK: vector linie, în stânga
        return new Vector2(world.X, world.Y);
    }

    /// <summary>Tasta R: obiectele revin la transformările inițiale, camera la starea inițială.</summary>
    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
        _pivot = null;
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}
