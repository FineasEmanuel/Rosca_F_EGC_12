using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// TEMA: camera orbită. Privește mereu spre o țintă (Target) și se mișcă pe o sferă în jurul ei.
/// Poziția pe sferă este dată de distanță (raza) și de două unghiuri:
/// azimutul (în jurul axei Y, ca yaw) și elevația (deasupra planului orizontal, ca pitch).
///   poziție = țintă + distanță · (cos el · cos az, sin el, cos el · sin az)
/// Comenzi: clic stânga ținut + mouse = rotire; săgețile = rotire de la tastatură;
/// rotița = distanța; W A S D = mută ținta în planul orizontal (bonus).
/// </summary>
public class OrbitCamera : Camera
{
    // --- Starea camerei ---------------------------------------------------------

    /// <summary>Punctul spre care privește camera (centrul sferei pe care se mișcă).</summary>
    public Vector3 Target { get; private set; }

    /// <summary>Distanța de la cameră la țintă (raza sferei).</summary>
    public float Distance { get; private set; }

    /// <summary>Azimutul, în grade: unghiul în jurul axei Y (ca yaw). 90° = camera stă pe +Z și privește spre -Z.</summary>
    public float Azimuth { get; private set; }

    /// <summary>Elevația, în grade: unghiul deasupra planului orizontal (ca pitch). Limitată la ±MaxElevation.</summary>
    public float Elevation { get; private set; }

    // --- Parametri ----------------------------------------------------------------

    public const float MaxElevation = 89.0f;       // grade; același motiv ca la pitch (secțiunea 5.3)
    public const float MinDistance = 2.0f;         // limitele distanței, alese de noi
    public const float MaxDistance = 40.0f;

    public float MouseSensitivity = 0.3f;          // grade pe pixel (clic stânga ținut)
    public float KeyRotateSpeed = 90.0f;           // grade pe secundă (săgeți)
    public float ZoomFactor = 0.9f;                // la un pas al rotiței distanța se înmulțește cu 0,9 (sau cu 1/0,9)
    public float MoveSpeed = 4.0f;                 // unități pe secundă (W A S D mută ținta)

    private static readonly Vector3 WorldUp = Vector3.UnitY;
    private static readonly Vector3 StartTarget = new(0.0f, 1.0f, -4.0f);
    private const float StartDistance = 12.0f;
    private const float StartAzimuth = 90.0f;      // camera pe +Z, privește spre -Z (ca în camera FPS)
    private const float StartElevation = 25.0f;

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "Orbită";

    /// <summary>Vectorul unitate de la țintă spre cameră (aceeași structură ca formula lui front).</summary>
    private Vector3 DirectionToCamera
    {
        get
        {
            float az = MathHelper.DegreesToRadians(Azimuth);
            float el = MathHelper.DegreesToRadians(Elevation);
            return new Vector3(
                MathF.Cos(el) * MathF.Cos(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Sin(az));
        }
    }

    public override Vector3 Position => Target + Distance * DirectionToCamera;

    /// <summary>Camera privește spre țintă, deci direcția privirii este opusul direcției țintă -> cameră.</summary>
    public override Vector3 Front => -DirectionToCamera;

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, WorldUp);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // 1. Rotirea cu mouse-ul: clic stânga ținut + mișcare.
        //    Tragerea spre dreapta / în jos "rotește scena" în acel sens, deci camera se mută invers pe sferă.
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation += mouseDelta.Y * MouseSensitivity;
        }

        // 2. Rotirea de la tastatură: săgețile (viteză în grade pe secundă, înmulțită cu dt).
        float rotate = KeyRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth += rotate;
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth -= rotate;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += rotate;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= rotate;

        // 3. Rotița schimbă distanța (nu FOV-ul): rotița în sus apropie camera de țintă.
        if (mouse.ScrollDelta.Y != 0.0f)
        {
            Distance *= MathF.Pow(ZoomFactor, mouse.ScrollDelta.Y);
        }

        // 4. Limitele: elevația ±89° (evită răsturnarea imaginii), distanța între Min și Max.
        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);
        Distance = Math.Clamp(Distance, MinDistance, MaxDistance);
        Azimuth %= 360.0f;   // doar ca numărul să nu crească la nesfârșit

        // 5. Bonus: W A S D mută ținta în planul orizontal (față de direcția privirii).
        Vector3 forward = new(Front.X, 0.0f, Front.Z);
        if (forward.LengthSquared > 1e-8f)
        {
            forward = forward.Normalized();
            Vector3 right = Vector3.Cross(forward, WorldUp);

            Vector3 move = Vector3.Zero;
            if (keyboard.IsKeyDown(Keys.W)) move += forward;
            if (keyboard.IsKeyDown(Keys.S)) move -= forward;
            if (keyboard.IsKeyDown(Keys.D)) move += right;
            if (keyboard.IsKeyDown(Keys.A)) move -= right;

            if (move.LengthSquared > 1e-8f)   // W+S sau A+D apăsate împreună se anulează: nu normalizăm vectorul nul
            {
                Target += move.Normalized() * MoveSpeed * dt;
            }
        }
    }

    /// <summary>
    /// Bonus (comutare fără "salt" FPS -> orbită): camera orbită este așezată astfel încât să privească
    /// exact ca o cameră aflată în <paramref name="position"/>, cu direcția <paramref name="front"/>.
    /// Ținta = punctul aflat la <paramref name="distance"/> în fața camerei; unghiurile se calculează
    /// din vectorul unitate țintă -> cameră, o = -front: elevația = asin(o.y), azimutul = atan2(o.z, o.x).
    /// </summary>
    public void SetFromView(Vector3 position, Vector3 front, float distance)
    {
        front = front.Normalized();
        Vector3 o = -front;

        Distance = Math.Clamp(distance, MinDistance, MaxDistance);
        Target = position + front * Distance;
        Elevation = Math.Clamp(MathHelper.RadiansToDegrees(MathF.Asin(Math.Clamp(o.Y, -1.0f, 1.0f))),
                               -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(o.Z, o.X));
    }

    public override void Reset()
    {
        Target = StartTarget;
        Distance = StartDistance;
        Azimuth = StartAzimuth;
        Elevation = StartElevation;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 p = Position;
        yield return $"Țintă          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}   (limite {MinDistance}–{MaxDistance})";
        yield return $"Azimut / elev. : {Azimuth,7:F1}° / {Elevation,6:F1}°";
        yield return $"Poziție        : ({p.X,6:F2}, {p.Y,6:F2}, {p.Z,6:F2})";
    }
}