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

	private readonly MSGPolygon preview_polygon = new(MSGPolygon.GetCricle(50));
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
				DrawColoredPolygon(poly.TransformedShape, colorConfig[k], null, null);
			}
		}

		DrawPolyline(preview_polygon.TransformedShape, colorConfig[current_preview_color].Lerp(new(1, 1, 1, 0), 0.5f), preview_line_width, true);
	}

	private void SolveScene()
	{
		var origin_r = inputPolygons[PolygonsColor.Red].Select(p => p.TransformedShape).ToArray();
		var origin_g = inputPolygons[PolygonsColor.Green].Select(p => p.TransformedShape).ToArray();
		var origin_b = inputPolygons[PolygonsColor.Blue].Select(p => p.TransformedShape).ToArray();
		var origin_c = inputPolygons[PolygonsColor.Cyan].Select(p => p.TransformedShape).ToArray();
		var origin_m = inputPolygons[PolygonsColor.Magenta].Select(p => p.TransformedShape).ToArray();
		var origin_y = inputPolygons[PolygonsColor.Yellw].Select(p => p.TransformedShape).ToArray();
		var origin_w = inputPolygons[PolygonsColor.White].Select(p => p.TransformedShape).ToArray();
		origin_r = Concat(origin_r, origin_y, origin_m, origin_w);
		origin_g = Concat(origin_g, origin_y, origin_c, origin_w);
		origin_b = Concat(origin_b, origin_m, origin_c, origin_w);

		var yellow = ResolveIntersect(origin_r, origin_g);
		var magenta = ResolveIntersect(origin_r, origin_b);
		var cyan = ResolveIntersect(origin_g, origin_b);

		var white = ResolveIntersect(yellow, origin_b);

		var red_only = ResolveClip(origin_r, Concat(yellow, magenta));
		var green_only = ResolveClip(origin_g, Concat(cyan, yellow));
		var blue_only = ResolveClip(origin_b, Concat(cyan, magenta));

		var yellow_only = ResolveClip(yellow, white);
		var magenta_only = ResolveClip(magenta, white);
		var cyan_only = ResolveClip(cyan, white);


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
			if (current == null) continue;
			v.Clear();
			v.AddRange(current.Select(MSGPolygon.FromTransformedShape));
		}
	}

	private Vector2[][] Concat(params Vector2[][][] lists)
	=> [.. lists.SelectMany(x => x)];

	private Vector2[][] ResolveIntersect(ReadOnlySpan<Vector2[]> a, ReadOnlySpan<Vector2[]> b)
	{

		var result = new List<Vector2[]>();
		foreach (var ae in a)
		{
			foreach (var be in b)
			{
				result.AddRange(Geometry2D.IntersectPolygons(ae, be));
			}
		}
		return [.. result];
	}

	private Vector2[][] ResolveClip(ReadOnlySpan<Vector2[]> a, ReadOnlySpan<Vector2[]> b)
	{
		var result = new List<Vector2[]>(a.ToArray());
		foreach (var mask in b)
		{
			var next = new List<Vector2[]>();
			foreach (var part in result)
				next.AddRange(Geometry2D.ClipPolygons(part, mask));
			result = next;
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
