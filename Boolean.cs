using System;
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
	[Export] private bool debugInvalidPolygons = false;

	private const float VertexEpsilon = 0.02f;
	private const float AreaEpsilon = 0.02f;

	private readonly Dictionary<PolygonsColor, UnstableFixedArray<MSGPolygon>> inputPolygons = new()
	{
		{PolygonsColor.Red, new(30)},
		{PolygonsColor.Green, new(30)},
		{PolygonsColor.Blue, new(30)},
		{PolygonsColor.Cyan, new(30)},
		{PolygonsColor.Magenta, new(30)},
		{PolygonsColor.Yellw, new(30)},
		{PolygonsColor.White, new(30)},
	};

	private readonly Dictionary<PolygonsColor, UnstableFixedArray<MSGPolygon>> resolvePolygons = new()
	{
		{PolygonsColor.Red, new(30)},
		{PolygonsColor.Green, new(30)},
		{PolygonsColor.Blue, new(30)},
		{PolygonsColor.Cyan, new(30)},
		{PolygonsColor.Magenta, new(30)},
		{PolygonsColor.Yellw, new(30)},
		{PolygonsColor.White, new(30)},
	};

	private readonly MSGPolygon preview_polygon = new(MSGPolygon.GetCricle(10));
	private PolygonsColor current_preview_color;

	public override void _Ready()
	{
		preview_polygon.Scale = new(100, 100);
	}

	public override void _Draw()
	{
		foreach (var (k, v) in resolvePolygons)
		{
			foreach (var poly in v)
			{
				var shape = poly.TransformedShape;
				shape = [.. shape.Select(o=> o/100)];
				DrawColoredPolygon(shape, colorConfig[k], null, null);
			}
		}

		DrawPolyline(preview_polygon.TransformedShape, colorConfig[current_preview_color].Lerp(new(1, 1, 1, 0), 0.5f), preview_line_width, true);
	}

	private void SolveScene()
	{
		var origin_r = inputPolygons[PolygonsColor.Red].Select(p => p.TransformedShape.Select(o => o * 100).ToArray()).ToArray();
		var origin_g = inputPolygons[PolygonsColor.Green].Select(p => p.TransformedShape.Select(o => o * 100).ToArray()).ToArray();
		var origin_b = inputPolygons[PolygonsColor.Blue].Select(p => p.TransformedShape.Select(o => o * 100).ToArray()).ToArray();
		var origin_c = inputPolygons[PolygonsColor.Cyan].Select(p => p.TransformedShape.Select(o => o * 100).ToArray()).ToArray();
		var origin_m = inputPolygons[PolygonsColor.Magenta].Select(p => p.TransformedShape.Select(o => o * 100).ToArray()).ToArray();
		var origin_y = inputPolygons[PolygonsColor.Yellw].Select(p => p.TransformedShape.Select(o => o * 100).ToArray()).ToArray();
		var origin_w = inputPolygons[PolygonsColor.White].Select(p => p.TransformedShape.Select(o => o * 100).ToArray()).ToArray();

		origin_r = MergeAndClean(Concat(origin_r, origin_y, origin_m, origin_w), PolygonsColor.Red);
		origin_g = MergeAndClean(Concat(origin_g, origin_y, origin_c, origin_w), PolygonsColor.Green);
		origin_b = MergeAndClean(Concat(origin_b, origin_m, origin_c, origin_w), PolygonsColor.Blue);

		var yellow = ResolveIntersect(origin_r, origin_g, PolygonsColor.Yellw);
		var magenta = ResolveIntersect(origin_r, origin_b, PolygonsColor.Magenta);
		var cyan = ResolveIntersect(origin_g, origin_b, PolygonsColor.Cyan);

		var white = ResolveIntersect(yellow, origin_b, PolygonsColor.White);

		var yellow_only = ResolveClip(yellow, origin_b, PolygonsColor.Yellw);
		var magenta_only = ResolveClip(magenta, origin_g, PolygonsColor.Magenta);
		var cyan_only = ResolveClip(cyan, origin_r, PolygonsColor.Cyan);

		var red_only = ResolveClip(origin_r, Concat(origin_g, origin_b), PolygonsColor.Red);
		var green_only = ResolveClip(origin_g, Concat(origin_r, origin_b), PolygonsColor.Green);
		var blue_only = ResolveClip(origin_b, Concat(origin_r, origin_g), PolygonsColor.Blue);

		foreach (var (k, v) in resolvePolygons)
		{
			var current = k switch
			{
				PolygonsColor.Red => red_only,
				PolygonsColor.Green => green_only,
				PolygonsColor.Blue => blue_only,
				PolygonsColor.Cyan => cyan_only,
				PolygonsColor.Magenta => magenta_only,
				PolygonsColor.Yellw => yellow_only,
				PolygonsColor.White => white,
				_ => throw new NotImplementedException(),
			};

			v.Clear();
			v.AddRange(MergeAndClean(current, k).Select(MSGPolygon.FromTransformedShape));
		}
	}

	private Vector2[][] MergeAndClean(Vector2[][] polygons, PolygonsColor color)
	{
		// var beforeMerge = CleanPolygons(polygons, color);
		var afterMerge = polygons.MergeAll();
		return CleanPolygons(afterMerge, color);
	}

	private static bool IsSamePoint(Vector2 a, Vector2 b)
	{
		return a.IsEqualApprox(b);
	}

	private static Vector2[][] CleanPolygon(Vector2[] polygon)
	{
		if (polygon.Length == 0) return [];

		var cleaned = new List<Vector2>(polygon.Length);

		foreach (var point in polygon)
		{
			if (cleaned.Count == 0 || !IsSamePoint(cleaned[^1], point))
			{
				cleaned.Add(point);
			}
		}

		var removedPoint = true;
		while (removedPoint && cleaned.Count > 3)
		{
			removedPoint = false;
			for (var i = 0; i < cleaned.Count; i++)
			{
				var prev = cleaned[(i - 1 + cleaned.Count) % cleaned.Count];
				var current = cleaned[i];
				var next = cleaned[(i + 1) % cleaned.Count];
				var prevToCurrent = current - prev;
				var currentToNext = next - current;
				var cross = prevToCurrent.X * currentToNext.Y - prevToCurrent.Y * currentToNext.X;

				if (Mathf.Abs(cross) <= VertexEpsilon && prev.IsEqualApprox(next))
				{
					cleaned.RemoveAt(i);
					removedPoint = true;
					break;
				}
			}
		}


		if (IsSelfIntersecting([.. cleaned]))
		{
			var cleaneds = Geometry2D.MergePolygons(cleaned.ToArray(), cleaned.ToArray());

			return [.. cleaneds];
		}

		return [[.. cleaned]];
	}

	private Vector2[][] CleanPolygons(IEnumerable<Vector2[]> polygons, PolygonsColor color)
	{
		var result = new List<Vector2[]>();
		foreach (var polygon in polygons)
		{
			var cleaned = CleanPolygon(polygon);
			foreach (var c in cleaned)
			{
				if (IsValidPolygon(c, color))
				{
					result.Add(c);
				}
			}
		}
		return [.. result];
	}

	private static bool IsSelfIntersecting(Vector2[] poly)
	{
		int n = poly.Length;
		for (int i = 0; i < n; i++)
		{
			Vector2 a1 = poly[i];
			Vector2 a2 = poly[(i + 1) % n];

			for (int j = i + 2; j < n; j++)
			{
				if (i == 0 && j == n - 1) continue;

				Vector2 b1 = poly[j];
				Vector2 b2 = poly[(j + 1) % n];

				Variant intersect = Geometry2D.SegmentIntersectsSegment(a1, a2, b1, b2);
				if (intersect.Obj != null)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsValidPolygon(Vector2[] p, PolygonsColor c)
	{
		var area = p.Length >= 3 ? MSGPolygon.ShoelaceArea(p) : 0;
		if (p.Length < 3 || area <= AreaEpsilon)
		{
			return false;
		}
		var tri = Geometry2D.TriangulatePolygon(p);
		if (tri.Length == 0)
		{
			var reason = IsSelfIntersecting(p) ? "failed to triangulate, self intersect" : "failed to triangulate";
			PrintInvalidPolygon(c, p, area, "Triangulate Failed");
			Debug.Assert(false, "Should not happened can't triangulate");
		}
		return tri.Length != 0;
	}

	private void PrintInvalidPolygon(PolygonsColor color, Vector2[] polygon, float area, string reason)
	{
		if (!debugInvalidPolygons) return;

		GD.Print($"Invalid polygon [{color}] color={color} reason={reason} vertices={polygon.Length} area={area}");
		for (var i = 0; i < polygon.Length; i++)
		{
			GD.Print($"  {i}: {polygon[i]}");
		}
	}

	private Vector2[][] Concat(params Vector2[][][] lists)
	=> [.. lists.SelectMany(x => x)];

	private Vector2[][] ResolveIntersect(ReadOnlySpan<Vector2[]> a, ReadOnlySpan<Vector2[]> b, PolygonsColor color)
	{
		var result = new List<Vector2[]>();
		foreach (var ae in a)
		{
			foreach (var be in b)
			{
				result.AddRange(Geometry2D.IntersectPolygons(ae, be));
			}
		}
		return CleanPolygons(result, color);
	}

	private Vector2[][] ResolveClip(ReadOnlySpan<Vector2[]> a, ReadOnlySpan<Vector2[]> b, PolygonsColor color)
	{
		var result = CleanPolygons(a.ToArray(), color).ToList();
		foreach (var mask in b)
		{
			var next = new List<Vector2[]>();
			foreach (var part in result)
			{
				next.AddRange(Geometry2D.ClipPolygons(part, mask));
			}
			result = [.. CleanPolygons(next, color)];
		}
		return [.. result];
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
			if (MSGPolygon.IsIntersects(preview_polygon, resolvePolygons[current_preview_color].AsSpan()))
			{
				GD.Print($"Can't overlay same color polygon: {current_preview_color}");
				return;
			}
			inputPolygons[current_preview_color].Add(new(preview_polygon));
			SolveScene();
			QueueRedraw();
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
