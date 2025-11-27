using System.Diagnostics;
using System.Reflection;
using Facebook.Yoga;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SFMLUI;

namespace SFML_UI;

public class App
{
	private RenderWindow? _window;
	private bool _vsync = true;

	private Font? _font;

	private UI? _ui;

	private Text? _debugText;
	private TimeSpan _lastFrameTime;

	struct DebugData
	{
		public int MouseX;
		public int MouseY;
	}

	private DebugData _debugData;

	public void Run(bool debug)
	{
		UI.InitializeGL();

		VideoMode mode = new(800, 600);
		ContextSettings.Attribute attributes = ContextSettings.Attribute.Default;
		if (debug)
		{
			attributes |= ContextSettings.Attribute.Debug;
			Console.WriteLine("Debug mode enabled");
		}

		ContextSettings contextSettings = new(
			depthBits: 24,
			stencilBits: 8,
			antialiasingLevel: 0,
			majorVersion: 4,
			minorVersion: 6,
			attributes,
			sRgbCapable: false);
		_window = new(mode, "SFMLUI", Styles.Default, contextSettings);
		_window.SetVerticalSyncEnabled(_vsync);

		_window.Closed += (_, _) => { OnClose(); };
		_window.Resized += (_, e) => { OnResize(e); };
		_window.KeyPressed += (_, e) => { OnKeyPressed(e); };
		_window.KeyReleased += (_, e) => { OnKeyReleased(e); };
		_window.TextEntered += (_, e) => { OnTextEntered(e); };
		_window.MouseMoved += (_, e) => { OnMouseMoved(e); };
		_window.MouseButtonPressed += (_, e) => { OnMousePressed(e); };
		_window.MouseButtonReleased += (_, e) => { OnMouseReleased(e); };
		_window.MouseWheelScrolled += (_, e) => { OnMouseScrolled(e); };

		using Stream? fontStream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("SFML_UI.res.JetBrainsMono-Regular.ttf");

		_font = new Font(fontStream);
		_debugText = new Text("Cursor Pos: ", _font, 16);

		WindowProxy windowProxy = new(_window);

		_ui = new UI((Vector2f)_window.Size, windowProxy)
		{
			Style =
			{
				Font = _font,
			},
		};

		_ui.DrawEnd += () =>
		{
			UpdateDebugText();
			_window.Draw(_debugText);
		};

		Widget root = _ui.Root;

		var containerBig = new Widget
		{
			FlexDirection = YogaFlexDirection.Column,
			Padding = 10,
			Name = "containerBig",
			FixedWidth = YogaValue.Percent(100),
			FixedHeight = YogaValue.Percent(100),
			FillColor = new Color(50, 100, 50),
		};
		root.AddChild(containerBig);

		var container = new Widget
		{
			FlexDirection = YogaFlexDirection.Row,
			FlexGrow = 1,
			Padding = 20,
			Margin = 20,
			Name = "container",
			BorderRadius = 10,
			FillColor = new Color(100, 150, 150),
		};
		containerBig.AddChild(container);

		var scroll = new WidgetScrollArea
		{
			Margin = 5,
			FixedWidth = YogaValue.Percent(40),
			FixedHeight = YogaValue.Percent(80),
			Name = "red scroll area",
			BorderRadius = 14,
			FillColor = new Color(220, 5, 5),
		};
		container.AddChild(scroll);

		var scroll2 = new WidgetScrollArea
		{
			Margin = 5,
			Padding = 14,
			FlexDirection = YogaFlexDirection.Row,
			FlexGrow = 1,
			FixedHeight = YogaValue.Percent(100),
			MinWidth = YogaValue.Point(80),
			Name = "blue scroll area",
			FillColor = Color.Blue,
		};
		container.AddChild(scroll2);

		var spam = new Widget
		{
			FixedWidth = 100,
			FixedHeight = 100,
			Margin = 4,
			Name = "spam",
			BorderRadius = 10,
			BorderWidth = 5,
			FillColor = new Color(50, 100, 120),
		};
		scroll2.AddChild(spam);

		var spam2 = new Widget
		{
			Left = -40,
			Top = 30,
			FixedWidth = 50,
			FixedHeight = 50,
			Margin = 4,
			Name = "spam2",
			BorderRadius = 15,
			FillColor = new Color(120, 150, 10),
		};
		scroll2.AddChild(spam2);

		var spam3 = new Widget
		{
			Left = -130,
			Top = 40,
			FixedWidth = 50,
			FixedHeight = 100,
			Margin = 4,
			Name = "spam3",
			BorderRadius = 25,
			FillColor = new Color(10, 200, 100),
		};
		scroll2.AddChild(spam3);

		var longText = new WidgetLabel()
		{
			Text = "some long text"
		};
		scroll2.AddChild(longText);

		var button = new WidgetButton
		{
			MinWidth = 100,
			MinHeight = 100,
			Margin = 10,
			Padding = 15,
			PaddingTop = 10,
			PaddingBottom = 10,
			AlignSelf = YogaAlign.Center,
			AlignContent = YogaAlign.Center,
			AlignItems = YogaAlign.Center,
			JustifyContent = YogaJustify.Center,
			FlexDirection = YogaFlexDirection.Column,
			FillColor = new Color(51, 51, 51),
			HoverColor = new Color(69, 69, 69),
			PressColor = new Color(102, 102, 102),
			BorderRadius = 10,
			Name = "top button",
		};
		scroll.AddChild(button);

		var editLine = new WidgetEditLine()
		{
			Margin = 10,
			Name = "edit line",
			PlaceholderText = "Enter text..."
		};
		scroll.AddChild(editLine);

		var editLine2 = new WidgetEditLine()
		{
			Margin = 10,
			Name = "edit line",
			PlaceholderText = "Enter text...",
			Text = "spam"
		};
		scroll.AddChild(editLine2);

		var slider = new WidgetSlider()
		{
			FixedHeight = 15,
			MinWidth = 50,
			Margin = 10,
			FillColor = Color.Transparent,
			// FillColor = new Color(50, 50, 50),
			MinValue = 0,
			MaxValue = 100,
			Value = 30,
			Name = "slider",
		};
		scroll.AddChild(slider);

		var slider2 = new WidgetSlider()
		{
			FixedHeight = 15,
			MinWidth = 50,
			Margin = 10,
			FillColor = Color.Transparent,
			MinValue = 0,
			MaxValue = 100,
			Value = 40,
			Name = "slider 2",
		};
		containerBig.AddChild(slider2);

		var buttonLabel = new WidgetLabel
		{
			Text = "button",
			Name = "buttonLabel",
		};
		button.AddChild(buttonLabel);

		{
			var test = new Widget()
			{
				FlexDirection = YogaFlexDirection.Row,
				AlignItems = YogaAlign.Center,
				Padding = 5,
				FixedWidth = YogaValue.Percent(90),
				MinHeight = 90,
				AlignSelf = YogaAlign.Center,
				FillColor = new Color(50, 50, 50),
				Name = "test",
			};
			scroll.AddChild(test);

			var left = new Widget()
			{
				FlexGrow = 1,
				FlexShrink = 1,
				MinWidth = 0,
				FlexDirection = YogaFlexDirection.Row,
				AlignItems = YogaAlign.Center,
				FillColor = new Color(110, 10, 10),
				Name = "left",
			};
			test.AddChild(left);

			var leftSpam1 = new WidgetButton()
			{
				MinWidth = 10,
				MinHeight = 10,
				Margin = 5,
				Name = "leftSpam1",
			};
			left.AddChild(leftSpam1);

			var leftSpam2 = new WidgetButton()
			{
				MinWidth = 20,
				MinHeight = 30,
				Margin = 5,
				Name = "leftSpam2",
			};
			left.AddChild(leftSpam2);

			var center = new Widget()
			{
				FlexGrow = 0,
				FlexShrink = 0,
				FixedWidth = 50,
				FixedHeight = 30,
				FillColor = new Color(10, 110, 10),
				Name = "center",
			};
			test.AddChild(center);

			var right = new Widget()
			{
				FlexGrow = 1,
				FlexShrink = 1,
				MinWidth = 0,
				FlexDirection = YogaFlexDirection.RowReverse,
				AlignItems = YogaAlign.Center,
				FillColor = new Color(10, 10, 110),
				Name = "right",
			};
			test.AddChild(right);

			var rightSpam1 = new WidgetButton()
			{
				MinWidth = 10,
				MinHeight = 10,
				Margin = 5,
				Name = "rightSpam1",
			};
			right.AddChild(rightSpam1);
		}

		var longButton = new WidgetButton
		{
			FixedWidth = YogaValue.Percent(90),
			Margin = 10,
			Padding = 40,
			AlignSelf = YogaAlign.Center,
			AlignContent = YogaAlign.Center,
			AlignItems = YogaAlign.Center,
			JustifyContent = YogaJustify.Center,
			FlexDirection = YogaFlexDirection.Column,
			FillColor = new Color(51, 51, 51),
			HoverColor = new Color(69, 69, 69),
			PressColor = new Color(102, 102, 102),
			BorderRadius = 20,
			BorderRadiusBottomRight = 60,
			BorderWidth = 3,
			Name = "long button",
		};
		scroll.AddChild(longButton);

		var longButtonLabel = new WidgetLabel
		{
			MinHeight = 10,
			Text = "long button",
			Name = "longButtonLabel",
		};
		longButton.AddChild(longButtonLabel);

		var innerScroll = new WidgetScrollArea
		{
			FixedWidth = YogaValue.Percent(80),
			MinWidth = 250,
			AspectRatio = 2f,
			Margin = 5,
			Name = "inner scroll area",
			BorderRadius = 16,
			FillColor = Color.Green,
		};
		scroll.AddChild(innerScroll);

		for (int i = 0; i < 8; ++i)
		{
			var b = new WidgetButton
			{
				Margin = 10,
				Padding = 20,
				PaddingTop = 10,
				PaddingBottom = 10,
				AlignSelf = YogaAlign.Center,
				AlignContent = YogaAlign.Center,
				AlignItems = YogaAlign.Center,
				JustifyContent = YogaJustify.Center,
				FlexDirection = YogaFlexDirection.Column,
				FillColor = new Color(51, 51, 81),
				HoverColor = new Color(69, 69, 99),
				PressColor = new Color(102, 102, 132),
				BorderRadius = 14,
				BorderRadiusBottomRight = 5,
				Name = $"button_{i}",
			};
			innerScroll.AddChild(b);

			var bl = new WidgetLabel
			{
				MinHeight = 10,
				FillColor = Color.Transparent,
				TextColor = Color.White,
				FontSize = 17,
				Text = $"butt {i}",
				Name = $"buttonLabel {i}",
			};
			b.AddChild(bl);
		}

		for (int i = 0; i < 10; ++i)
		{
			var b = new WidgetButton
			{
				Margin = 10,
				Padding = 15,
				PaddingTop = 10,
				PaddingBottom = 10,
				AlignSelf = YogaAlign.Center,
				AlignContent = YogaAlign.Center,
				AlignItems = YogaAlign.Center,
				JustifyContent = YogaJustify.Center,
				FlexDirection = YogaFlexDirection.Column,
				FillColor = new Color(51, 51, 81),
				HoverColor = new Color(69, 69, 99),
				PressColor = new Color(102, 102, 132),
				Name = $"button_{i}",
			};
			scroll.AddChild(b);

			var bl = new WidgetLabel
			{
				FillColor = Color.Transparent,
				TextColor = Color.White,
				FontSize = 17,
				Text = $"butt {i}",
				Name = $"buttonLabel {i}",
			};
			b.AddChild(bl);
		}

		var box3 = new Widget
		{
			FixedWidth = 70,
			FixedHeight = 70,
			Margin = 10,
			Name = "box3 blue",
			FillColor = Color.Blue,
		};
		scroll.AddChild(box3);

		button.Clicked += () => { Console.WriteLine("Clicked!"); };

		editLine.TextChanged += (text, _) => { Console.WriteLine($"Text changed to {text}"); };
		slider.ValueChanged += (value, _) => { Console.WriteLine($"Slider changed to {value:F1}"); };
		slider2.ValueChanged += (value, _) => { scroll.FixedWidth = YogaValue.Percent(value); };
		slider2.Value = 50;

		Stopwatch stopwatch = new();
		while (_window.IsOpen)
		{
			stopwatch.Restart();

			_window.DispatchEvents();
			_window.Clear();

			_ui.Update();
			_ui.Draw(_window);
			_window.Display();

			stopwatch.Stop();
			_lastFrameTime = stopwatch.Elapsed;
		}
	}

	private void OnClose()
	{
		_window?.Close();
	}

	private void OnResize(SizeEventArgs e)
	{
		if (_ui != null)
		{
			_ui.Size = new Vector2f(e.Width, e.Height);
		}
	}

	private void OnKeyPressed(KeyEventArgs e)
	{
		if (_ui == null)
		{
			return;
		}

		if (e.Code == Keyboard.Key.F1)
		{
			_ui.Style.EnableClipping = !_ui.Style.EnableClipping;
		}

		if (e.Code == Keyboard.Key.F2)
		{
			_ui.Style.EnableVisualizer = !_ui.Style.EnableVisualizer;
		}

		if (e.Code == Keyboard.Key.F3 && _window != null)
		{
			_vsync = !_vsync;
			_window.SetVerticalSyncEnabled(_vsync);
		}

		_ui.OnKeyPressed(e);
	}

	private void OnKeyReleased(KeyEventArgs e)
	{
		if (_ui == null)
		{
			return;
		}

		_ui.OnKeyReleased(e);
	}

	private void OnTextEntered(TextEventArgs e)
	{
		if (_ui == null)
		{
			return;
		}

		_ui.OnTextEntered(e);
	}

	private void OnMouseMoved(MouseMoveEventArgs e)
	{
		_debugData.MouseX = e.X;
		_debugData.MouseY = e.Y;
		UpdateDebugText();

		_ui?.OnMouseMoved(e);
	}

	private void OnMousePressed(MouseButtonEventArgs e)
	{
		_ui?.OnMousePressed(e);
	}

	private void OnMouseReleased(MouseButtonEventArgs e)
	{
		_ui?.OnMouseReleased(e);
	}

	private void OnMouseScrolled(MouseWheelScrollEventArgs e)
	{
		_ui?.OnMouseScrolled(e);
	}

	private void UpdateDebugText()
	{
		if (_debugText != null)
		{
			Widget? mouseCaptured = _ui?.MouseCapturedWidget;
			string? mouseCapturedName = mouseCaptured?.Name;

			Widget? hovered = _ui?.HoveredWidget;
			string? hoveredName = hovered?.Name;

			Widget? focusWidget = _ui?.FocusWidget;
			string? focusWidgetName = focusWidget?.Name;

			double elapsedSec = _lastFrameTime.TotalSeconds;
			double fps = elapsedSec == 0 ? 0 : 1.0 / elapsedSec;
			_debugText.DisplayedString = $"FPS: {fps:F1}\n" +
			                             $"Mouse X: {_debugData.MouseX}\n" +
			                             $"Mouse Y: {_debugData.MouseY}\n" +
			                             $"Hovered: {hoveredName}\n" +
			                             $"Captured: {mouseCapturedName}\n" +
			                             $"Focus: {focusWidgetName}" +
			                             $"";
		}
	}
}