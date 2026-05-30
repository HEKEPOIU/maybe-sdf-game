using Godot;
using GodotTool;

namespace MSG;

public partial class Boolean : Node2D
{
    UnstableFixedArray<MSGPolygon> polygons = new(3);

    public override void _Ready()
    {
        polygons.Add(new(new Vector2(300, 300), MSGPolygon.GetCricle(50), [new Color("478cbf")]));
        polygons.Add(new(new Vector2(350, 300), MSGPolygon.GetCricle(50), [new Color(1, 1, 1, 1)]));
        polygons.Add(new(new Vector2(400, 300), MSGPolygon.GetCricle(50), [new Color(1, 0, 1, 1)]));
        polygons[0].Scale = new Vector2(100, 100);
        polygons[1].Scale = new Vector2(100, 100);
        polygons[2].Scale = new Vector2(100, 100);
    }


    public override void _Draw()
    {
        // var pos = new Vector2(300, 300);
        // DrawCircle(pos, 30, Color.Color8(255, 255, 255), antialiased: true);

        foreach (var poly in polygons)
        {
            DrawPolygon(poly.TransformedShape, poly.Color);
        }
        var cricle = polygons[0].TransformedShape;
        var cricle2 = polygons[1].TransformedShape;

        var result = Geometry2D.IntersectPolygons(cricle, cricle2);

        for (var i = 0; i < result.Count; i++)
        {
            GD.Print($"Size of intersect {i}: {ShoelaceArea(result[i])}");
            DrawPolygon(result[i], [Color.FromHsv(0.5f, 0.3f + i * 0.1f, 1)]);
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        var pos = GetGlobalMousePosition();

        polygons[0].Position = pos;
    }

    private static float ShoelaceArea(Vector2[] polygon)
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

}
