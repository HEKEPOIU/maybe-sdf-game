
using Godot;
using GodotTool;

namespace MSG;

class MSGPolygon(Vector2 position, Vector2[] shape, Color[] color)
{
    public Color[] Color = color;
    public Vector2 Position = position;
    public Vector2 Scale = Vector2.One;
    public float Rotation = 0;
    public Transform2D Transform
    {
        get => new(Rotation, Scale, 0, Position);
    }

    public Vector2[] TransformedShape
    {
        get => Transform * shape;
    }


    public float CountSize()
    {
        return ShoelaceArea(TransformedShape);
    }

    public static float ShoelaceArea(Vector2[] polygon)
    {
        var size = polygon.Length;
        float area = 0;
        for (var i = 0; i < size; i++)
        {
            var j = (i + 1) % size;
            area += polygon[i].X * polygon[j].Y;
            area -= polygon[j].X * polygon[i].Y;
        }
        return Mathf.Abs(area / 2);
    }

    public static Vector2[] GetCricle(int side)
    {
        Debug.Assert(side >= 2);

        var result = new Vector2[side];
        var startAngle = -Mathf.Pi / 2;

        for (int i = 0; i < side; i++)
        {
            var currentAngle = startAngle + 2 * Mathf.Pi / side * i;
            result[i] = new Vector2(0 + 1 * Mathf.Cos(currentAngle), 0 + 1 * Mathf.Sin(currentAngle));
        }

        return result;
    }
}
