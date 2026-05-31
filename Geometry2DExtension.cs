using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotTool;

public static class Geometry2DExtension
{

    public static Vector2[][] MergeAll(this Vector2[][] polygons)
    {
        var current = polygons.ToList();

        bool anyMerged;
        do
        {
            anyMerged = false;
            for (int i = 0; i < current.Count; i++)
            {
                for (int j = i + 1; j < current.Count; j++)
                {
                    var result = Geometry2D.MergePolygons(current[i], current[j]);
                    if (result.Count == 1)
                    {
                        current[i] = result[0];
                        current.RemoveAt(j);
                        anyMerged = true;
                        j--;
                    }
                }
            }
        } while (anyMerged);

        return [.. current];
    }

    public static Rect2 CountAabb(this Vector2[] c)
    {
        var max = new Vector2(c[0].X, c[0].Y);
        var min = new Vector2(c[0].X, c[0].Y);

        foreach (var p in c)
        {
            if (p.X > max.X)
            {
                max.X = p.X;
            }
            if (p.X < min.X)
            {
                min.X = p.X;
            }
            if (p.Y > max.Y)
            {
                max.Y = p.Y;
            }
            if (p.Y < min.Y)
            {
                min.Y = p.Y;
            }
        }

        return new Rect2()
        {
            Position = min,
            End = max,
        };
    }
}
