using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotTool;

namespace MSG;

public partial class Boolean : Node2D
{
    enum PolygonsColor
    {
        Red,
        Green,
        Blue,
        Cyan,
        Magenta,
        Yellw,
        White,

        MAXCOUNT,
    }
    [Export] private Godot.Collections.Dictionary<PolygonsColor, Color> colorConfig { get; set; } = [];
    [Export] private float preview_line_width = 2;
    private readonly Dictionary<PolygonsColor, UnstableFixedArray<MSGPolygon>> polygons = new()
    {
        {PolygonsColor.Red, new(30)},
        {PolygonsColor.Green, new(30)},
        {PolygonsColor.Blue, new(30)},
        {PolygonsColor.Cyan, new(30)},
        {PolygonsColor.Magenta, new(30)},
        {PolygonsColor.Yellw, new(30)},
        {PolygonsColor.White, new(30)},
    };
    private readonly MSGPolygon preview_polygon = new(MSGPolygon.GetCricle(50));
    private PolygonsColor current_preview_color;

    public override void _Ready()
    {
        preview_polygon.Scale = new(100, 100);
    }

    public override void _Draw()
    {
        foreach (var (k, v) in polygons)
        {
            var shapes = v.Select(p => p.TransformedShape).ToArray();
            var merge = shapes.MergeAll();
            v.Clear();
            foreach (var poly in merge)
            {
                v.Add(new(poly));
            }
        }

        foreach (var (k, v) in polygons)
        {
            foreach (var poly in v)
            {
                DrawColoredPolygon(poly.TransformedShape, colorConfig[k], null, null);
            }
        }

        DrawPolyline(preview_polygon.TransformedShape, colorConfig[current_preview_color].Lerp(new(1, 1, 1, 0), 0.5f), preview_line_width, true);
    }
    public override void _PhysicsProcess(double delta)
    {
        QueueRedraw();
    }


    public override void _Process(double delta)
    {
        var pos = GetGlobalMousePosition();
        preview_polygon.Position = pos;
        if (Input.IsActionJustPressed(ProjectInput.PUT_POLYGON))
        {
            if (MSGPolygon.IsIntersects(preview_polygon, polygons[current_preview_color].AsSpan()))
            {
                GD.Print($"Can't overlay same color polygon");
                return;
            }
            polygons[current_preview_color].Add(new(preview_polygon));
        }

        var colorChangeDir = 0;
        if (Input.IsActionJustPressed(ProjectInput.CHANGE_COLOR_NEXT))
        {
            colorChangeDir = 1;
        }
        else if (Input.IsActionJustPressed(ProjectInput.CHANGE_COLOR_PREV))
        {
            colorChangeDir = -1;
        }
        var max = (int)PolygonsColor.MAXCOUNT;
        current_preview_color = (PolygonsColor)(((int)current_preview_color + colorChangeDir % max + max) % max);

    }

}
