using SFML.Window;

namespace SFMLUI;

[Flags]
public enum MouseButton
{
	None = 0,
	Left = 1 << 0,
	Middle = 1 << 1,
	Right = 1 << 2,
}

[Flags]
public enum Modifier
{
	None = 0,
	Shift = 1 << 0,
	Control = 1 << 1,
	Alt = 1 << 2,
}

internal static partial class Utils
{
	public static MouseButton ToMouseButton(Mouse.Button button)
	{
		return button switch
		{
			Mouse.Button.Left => MouseButton.Left,
			Mouse.Button.Middle => MouseButton.Middle,
			Mouse.Button.Right => MouseButton.Right,
			_ => MouseButton.None,
		};
	}
}