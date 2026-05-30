using Godot;

namespace GodotTool;

public static class Debug
{
	public static void Assert(bool condition, string message = null)
	{
#if DEBUG
		if (!condition)
		{
			if (message is not null)
				GD.PushError("Assertion failed:", message);
			else
				GD.PushError("Assertion failed!");

			EngineDebugger.Debug(true, true);
		}
#endif
	}
}
