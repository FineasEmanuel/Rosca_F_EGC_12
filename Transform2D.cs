using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Poziția, rotația și scalarea unui obiect din scenă. Din ele se construiește matricea model,
/// care duce vârfurile obiectului din object space în world space (secțiunea 5).
/// </summary>
public class Transform2D
{
    public Vector2 Position = Vector2.Zero;   // translația, în unități de lume
    public float Rotation = 0.0f;             // unghiul, în GRADE, pozitiv = sens trigonometric
    public Vector2 Scale = Vector2.One;       // factorii de scalare pe x și pe y
    public float Depth = 0.0f;                // coordonata z; contează doar în perspectivă (secțiunea 7.2)

    public Matrix4 ModelMatrix =>
        // T1: scalare -> rotație -> translație (în C# se citește de la stânga la dreapta).
        // T1: dacă translația ar fi aplicată ÎNAINTEA rotației, obiectul ar fi mutat întâi în Position,
        // iar rotația (făcută față de originea lumii) l-ar plimba pe un arc de cerc în jurul
        // originii, în loc să-l rotească pe loc în jurul propriului centru.
        Matrix4.CreateScale(Scale.X, Scale.Y, 1.0f)
        * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation))
        * Matrix4.CreateTranslation(Position.X, Position.Y, Depth);

    /// <summary>Scalare uniformă cu un factor, cu scara fiecărei axe limitată între min și max.</summary>
    public void ScaleBy(float factor, float min = 0.1f, float max = 10.0f)
    {
        Scale = new Vector2(
            Math.Clamp(Scale.X * factor, min, max),
            Math.Clamp(Scale.Y * factor, min, max));
    }

    /// <summary>
    /// T4: rotește poziția obiectului în jurul unui pivot din lume și crește unghiul propriu
    /// cu aceeași valoare (obiectul "privește" mereu spre pivot la fel, ca Luna față de Pământ).
    /// Pozitiv = sens trigonometric.
    /// </summary>
    public void RotateAround(Vector2 pivot, float degrees)
    {
        float a = MathHelper.DegreesToRadians(degrees);
        float cos = MathF.Cos(a), sin = MathF.Sin(a);

        Vector2 d = Position - pivot;
        Position = pivot + new Vector2(d.X * cos - d.Y * sin,
                                       d.X * sin + d.Y * cos);
        Rotation += degrees;
    }

    /// <summary>O copie independentă (folosită pentru readucerea scenei în starea inițială).</summary>
    public Transform2D Clone() => (Transform2D)MemberwiseClone();
}
