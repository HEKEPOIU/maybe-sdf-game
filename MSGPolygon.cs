
using System;
using System.Collections.Generic;
using Godot;
using GodotTool;

namespace MSG;

public class MSGPolygon(Vector2[] shape)
{
	static public Dictionary<int, Vector2[]> CricleCache = [];
	public Vector2 Position = default;
	public Vector2 Scale = Vector2.One;
	public float Rotation = 0;
	public readonly Vector2[] Shape = shape;
	public Transform2D Transform
	{
		get => new(Rotation, Scale, 0, Position);
	}

	public Vector2[] TransformedShape
	{
		get => Transform * Shape;
	}

	public MSGPolygon(MSGPolygon other) : this(other.Shape)
	{
		Position = other.Position;
		Scale = other.Scale;
		Rotation = other.Rotation;
	}

	public bool IsIntersect(MSGPolygon poly)
	{
		return IsIntersect(TransformedShape, poly.TransformedShape);
	}

	public static MSGPolygon FromTransformedShape(Vector2[] ts)
	{
		var position = ts.CountAabb().GetCenter();
		var trans = new Transform2D(1, new(1, 1), 0, position);
		return new(trans.Inverse() * ts)
		{
			Position = position,
		};
	}


	public float CountSize()
	{
		return ShoelaceArea(TransformedShape);
	}

	public static float ShoelaceArea(ReadOnlySpan<Vector2> polygon)
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
		if (CricleCache.TryGetValue(side, out var value))
		{
			return value;
		}

		var result = new Vector2[side + 1];
		var startAngle = -Mathf.Pi / 2;

		for (int i = 0; i < side; i++)
		{
			var currentAngle = startAngle + 2 * Mathf.Pi / side * i;
			result[i] = new Vector2(0 + 1 * Mathf.Cos(currentAngle), 0 + 1 * Mathf.Sin(currentAngle));
		}
		result[^1] = result[0];
		CricleCache[side] = result;

		return result;
	}

	public static bool IsIntersect(ReadOnlySpan<Vector2> a, ReadOnlySpan<Vector2> b)
	{
		var r = Geometry2D.IntersectPolygons(a, b);
		return r.Count != 0;
	}

	public static bool IsIntersects(MSGPolygon target, ReadOnlySpan<MSGPolygon> polys)
	{
		foreach (var poly in polys)
		{
			if (target.IsIntersect(poly)) return true;
		}
		return false;
	}
}
