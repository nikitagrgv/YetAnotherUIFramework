using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace SFMLUI;

public class WidgetEditLine : Widget
{
	private readonly Text _text = new(null, null, 18);
	private readonly Text _placeholderText = new(null, null, 18);

	private const float ViewportThresholdLeft = 20;
	private const float ViewportThresholdRight = 40;
	private float _textOffset;

	private uint _fontSize = 18;

	private int _selectionBegin;
	private int _selectionLength;

	private readonly RectangleShape _cursorShape = new()
	{
		FillColor = new Color(255, 255, 255),
	};

	private readonly RectangleShape _selectionShape = new()
	{
		FillColor = new Color(10, 66, 122),
	};

	private float _cursorBlinkPeriod = 1f;
	private readonly UITimer _cursorTimer = new();
	private bool _cursorVisible = true;

	private string _textString = "";
	private int _cursorPosition = 0;

	private int CursorPosition
	{
		get => _cursorPosition;
		set
		{
			value = ToValidCharacterIndex(value);
			if (value == _cursorPosition)
			{
				return;
			}

			int oldValue = _cursorPosition;
			_cursorPosition = value;
			CursorPositionChanged?.Invoke(_cursorPosition, oldValue);
			EnsureCursorIsInViewport();
		}
	}

	public delegate void TextChangedDelegate(string text, string oldText);

	public event TextChangedDelegate? TextChanged;

	public delegate void CursorPositionChangedDelegate(int position, int oldPosition);

	public event CursorPositionChangedDelegate? CursorPositionChanged;

	public delegate void SelectionChangedDelegate(int begin, int length);

	public event SelectionChangedDelegate? SelectionChanged;

	public delegate string ValidateText(string text, string oldText);

	public ValidateText? TextValidator { get; set; }

	public Color TextColor
	{
		get => _text.FillColor;
		set => _text.FillColor = value;
	}

	public Color PlaceholderTextColor
	{
		get => _placeholderText.FillColor;
		set => _placeholderText.FillColor = value;
	}

	public string Text
	{
		get => _textString;
		set
		{
			if (string.Equals(_textString, value))
			{
				return;
			}

			if (TextValidator != null)
			{
				value = TextValidator(value, _textString);
				if (string.Equals(value, _textString))
				{
					return;
				}
			}

			string oldText = _textString;
			_textString = value;
			_text.DisplayedString = _textString;
			TextChanged?.Invoke(_textString, oldText);
			CursorPosition = ToValidCharacterIndex(CursorPosition);
			SetSelection(SelectionBegin, SelectionLength);
		}
	}

	public string PlaceholderText
	{
		get => _placeholderText.DisplayedString;
		set => _placeholderText.DisplayedString = value;
	}

	public int Length => _textString.Length;

	public int SelectionBegin => SelectionLength == 0 ? CursorPosition : _selectionBegin;
	public int SelectionLength => _selectionLength;

	public void SetSelection(int begin, int length)
	{
		if (length < 0)
		{
			begin += length;
			length = -length;
		}

		int end = begin + length;
		begin = ToValidCharacterIndex(begin);
		end = ToValidCharacterIndex(end);
		length = end - begin;

		if (begin == SelectionBegin && length == SelectionLength)
		{
			return;
		}

		_selectionBegin = begin;
		_selectionLength = length;
		SelectionChanged?.Invoke(SelectionBegin, SelectionLength);
	}

	public void SelectAll()
	{
		SetSelection(0, Length);
	}

	private void ClearSelection()
	{
		SetSelection(0, 0);
	}

	public uint FontSize
	{
		get => _fontSize;
		set
		{
			if (_fontSize == value)
				return;
			_text.CharacterSize = value;
			_placeholderText.CharacterSize = value;
		}
	}

	public float CursorWidth { get; set; } = 2;

	public float CursorBlinkPeriod
	{
		get => _cursorBlinkPeriod;
		set
		{
			_cursorBlinkPeriod = value;
			_cursorTimer.Interval = value / 2;
		}
	}

	public Color CursorColor
	{
		get => _cursorShape.FillColor;
		set => _cursorShape.FillColor = value;
	}

	public Color SelectionColor
	{
		get => _selectionShape.FillColor;
		set => _selectionShape.FillColor = value;
	}

	public WidgetEditLine()
	{
		BorderFocusColor = new Color(53, 116, 240);
		BorderColor = new Color(110, 110, 110);
		BorderHoverColor = new Color(140, 140, 140);

		TextColor = Color.White;
		PlaceholderTextColor = new Color(150, 150, 150);

		FixedHeight = 30;
		MinWidth = 40;
		FillColor = new Color(38, 38, 38);
		BorderRadius = 8;

		PaddingTop = 3;
		PaddingBottom = 3;
		PaddingLeft = 6;
		PaddingRight = 6;

		BorderWidth = 2;

		Cursor = CursorType.Text;

		_cursorTimer.SingleShot = false;
		_cursorTimer.Interval = _cursorBlinkPeriod / 2;
		_cursorTimer.Triggered += OnCursorTimerTriggered;
	}

	protected override bool HandleStyleChangeEvent(StyleChangeEvent e)
	{
		UpdateFont();
		return base.HandleStyleChangeEvent(e);
	}

	protected override bool HandleFocusEvent(FocusEvent e)
	{
		ResetCursor();
		return base.HandleFocusEvent(e);
	}

	protected override bool HandleUnfocusEvent(UnfocusEvent e)
	{
		_cursorVisible = false;
		_cursorTimer.Stop();
		return base.HandleUnfocusEvent(e);
	}

	protected override bool HandleMousePressEvent(MousePressEvent e)
	{
		int oldCursorPosition = CursorPosition;

		FloatRect textRect = GetTextRect();
		CursorPosition = ToCharacterIndex(e.LocalX - textRect.Left + _textOffset);

		ResetCursor();

		if (e.Button == MouseButton.Left && e.PressIndex > 0 && (e.Modifiers & Modifier.Shift) == 0 && Length > 0)
		{
			bool selectWord = e.PressIndex % 2 == 1;
			if (selectWord)
			{
				int characterIndex = int.Clamp(CursorPosition, 0, Length - 1);
				char character = Text[characterIndex];
				bool isWhiteSpace = char.IsWhiteSpace(character);

				int left = characterIndex;
				while (left > 1 && char.IsWhiteSpace(Text[left - 1]) == isWhiteSpace)
				{
					--left;
				}

				int right = characterIndex;
				while (right < Length && char.IsWhiteSpace(Text[right]) == isWhiteSpace)
				{
					++right;
				}

				SetSelectionBeginEnd(left, right);
			}
			else
			{
				SelectAll();
			}
		}
		else
		{
			bool selection = (e.Modifiers & Modifier.Shift) != 0;
			HandleCursorPositionChange(oldCursorPosition, selection);
		}

		return base.HandleMousePressEvent(e);
	}

	protected override bool HandleMouseMoveEvent(MouseMoveEvent e)
	{
		if ((e.PressedButtons & MouseButton.Left) != 0)
		{
			int oldCursorPosition = CursorPosition;
			FloatRect textRect = GetTextRect();
			CursorPosition = ToCharacterIndex(e.LocalX - textRect.Left + _textOffset);
			HandleCursorPositionChange(oldCursorPosition, selection: true);
			ResetCursor();
		}

		return base.HandleMouseMoveEvent(e);
	}

	protected override bool HandleTextEvent(TextEvent e)
	{
		if (e.Text.Any(char.IsControl) ||
		    e.Text.Contains('\n') ||
		    e.Text.Contains('\r') ||
		    e.Text.Contains('\t'))
		{
			return true;
		}

		RemoveSelectedText();

		if (CursorPosition >= Length)
		{
			Text += e.Text;
		}
		else if (CursorPosition <= 0)
		{
			Text = e.Text + Text;
		}
		else
		{
			string begin = Text.Substring(0, CursorPosition);
			string end = Text.Substring(CursorPosition, Length - CursorPosition);
			Text = begin + e.Text + end;
		}

		CursorPosition += e.Text.Length;
		ResetCursor();

		base.HandleTextEvent(e);
		return true;
	}

	protected override bool HandleKeyPressEvent(KeyPressEvent e)
	{
		int oldCursorPosition = CursorPosition;
		bool selection = (e.Modifiers & Modifier.Shift) != 0;

		switch (e.Key)
		{
			case Keyboard.Key.Backspace:
			{
				if (SelectionLength > 0)
				{
					RemoveSelectedText();
					ResetCursor();
					return true;
				}

				int nextCursorPos;
				if ((e.Modifiers & Modifier.Control) != 0)
				{
					nextCursorPos = PreviousWordPosition(CursorPosition, Text);
				}
				else
				{
					nextCursorPos = CursorPosition - 1;
				}

				if (nextCursorPos < 0 || nextCursorPos >= CursorPosition)
				{
					return true;
				}

				Text = Text.Remove(nextCursorPos, CursorPosition - nextCursorPos);
				CursorPosition = nextCursorPos;
				ResetCursor();
				return true;
			}
			case Keyboard.Key.Delete:
			{
				if (SelectionLength > 0)
				{
					RemoveSelectedText();
					ClearSelection();
					ResetCursor();
					return true;
				}

				int nextCursorPos;
				if ((e.Modifiers & Modifier.Control) != 0)
				{
					nextCursorPos = NextWordPosition(CursorPosition, Text);
				}
				else
				{
					nextCursorPos = CursorPosition + 1;
				}

				if (nextCursorPos > Length || nextCursorPos <= CursorPosition)
				{
					return true;
				}

				Text = Text.Remove(CursorPosition, nextCursorPos - CursorPosition);
				ResetCursor();
				return true;
			}
			case Keyboard.Key.PageUp:
			case Keyboard.Key.Home:
			case Keyboard.Key.Up:
				CursorPosition = 0;
				HandleCursorPositionChange(oldCursorPosition, selection);
				ResetCursor();
				return true;
			case Keyboard.Key.PageDown:
			case Keyboard.Key.End:
			case Keyboard.Key.Down:
				CursorPosition = Length;
				HandleCursorPositionChange(oldCursorPosition, selection);
				ResetCursor();
				return true;
			case Keyboard.Key.Left:
			{
				if ((e.Modifiers & Modifier.Control) != 0)
				{
					CursorPosition = PreviousWordPosition(CursorPosition, Text);
				}
				else
				{
					CursorPosition--;
				}

				HandleCursorPositionChange(oldCursorPosition, selection);
				ResetCursor();
				return true;
			}
			case Keyboard.Key.Right:
				if ((e.Modifiers & Modifier.Control) != 0)
				{
					CursorPosition = NextWordPosition(CursorPosition, Text);
				}
				else
				{
					CursorPosition++;
				}

				HandleCursorPositionChange(oldCursorPosition, selection);
				ResetCursor();
				return true;
			case Keyboard.Key.A when e.Modifiers == Modifier.Control:
				SelectAll();
				ResetCursor();
				return true;
		}

		return base.HandleKeyPressEvent(e);
	}

	protected override bool HandleLayoutChangeEvent(LayoutChangeEvent e)
	{
		EnsureCursorIsInViewport();
		return base.HandleLayoutChangeEvent(e);
	}

	private void EnsureCursorIsInViewport()
	{
		FloatRect textRect = GetTextRect();

		float halfWidth = textRect.Width / 2;
		float thresholdLeft = Math.Min(ViewportThresholdLeft, halfWidth);
		float thresholdRight = Math.Min(ViewportThresholdRight, halfWidth);

		float characterPosition = ToCharacterPosition(CursorPosition);
		if (characterPosition - _textOffset > textRect.Width - thresholdRight)
		{
			_textOffset = characterPosition - textRect.Width + thresholdRight;
		}

		if (characterPosition - _textOffset < thresholdLeft)
		{
			_textOffset = characterPosition - thresholdLeft;
		}

		float maxThreshold = Math.Max(0, ToCharacterPosition(Length) - textRect.Width);
		_textOffset = float.Clamp(_textOffset, 0, maxThreshold);
	}

	private void RemoveSelectedText()
	{
		if (SelectionLength <= 0)
		{
			return;
		}

		int pos = SelectionBegin;
		Text = Text.Remove(SelectionBegin, SelectionLength);
		CursorPosition = pos;
		ClearSelection();
	}

	private void HandleCursorPositionChange(int oldPosition, bool selection)
	{
		if (!selection)
		{
			SetSelection(0, 0);
			return;
		}

		if (SelectionLength != 0)
		{
			if (oldPosition == SelectionBegin)
			{
				SetSelectionBeginEnd(CursorPosition, SelectionBegin + SelectionLength);
				return;
			}

			if (oldPosition == SelectionBegin + SelectionLength)
			{
				SetSelectionBeginEnd(SelectionBegin, CursorPosition);
				return;
			}
		}

		SetSelectionBeginEnd(oldPosition, CursorPosition);
	}

	private void SetSelectionBeginEnd(int begin, int end)
	{
		begin = int.Clamp(begin, 0, Length);
		end = int.Clamp(end, 0, Length);
		if (begin < end)
		{
			SetSelection(begin, end - begin);
		}
		else
		{
			SetSelection(end, begin - end);
		}
	}

	private void OnCursorTimerTriggered()
	{
		_cursorVisible = !_cursorVisible;
	}

	protected override void Draw(IPainter painter)
	{
		base.Draw(painter);

		FloatRect textRect = GetTextRect();

		float lineSpacing = _text.Font.GetLineSpacing(_text.CharacterSize);
		float halfLineSpacing = lineSpacing / 2;
		float halfHeight = textRect.Height / 2;
		float verticalTextOffset = halfHeight - halfLineSpacing;

		Vector2f offset = new(-_textOffset, 0);

		if (SelectionLength > 0)
		{
			float selectionBegin = ToCharacterPosition(SelectionBegin);
			float selectionEnd = ToCharacterPosition(SelectionBegin + SelectionLength);
			float selectionLength = selectionEnd - selectionBegin;
			_selectionShape.Position = textRect.Position + new Vector2f(selectionBegin, 0) + offset;
			_selectionShape.Size = new Vector2f(selectionLength, textRect.Height);
			painter.Draw(_selectionShape);
		}

		Vector2f textPosition = textRect.Position + offset + new Vector2f(0, verticalTextOffset);
		if (Text.Length > 0)
		{
			_text.Position = textPosition;
			painter.Draw(_text);
		}
		else if (!IsFocused && PlaceholderText.Length > 0)
		{
			_placeholderText.Position = textPosition;
			painter.Draw(_placeholderText);
		}

		if (IsFocused)
		{
			if (_cursorVisible)
			{
				float cursorPos = ToCharacterPosition(CursorPosition);

				_cursorShape.Position = textRect.Position + new Vector2f(cursorPos, 0) + offset;
				_cursorShape.Size = new Vector2f(CursorWidth, textRect.Height);
				painter.Draw(_cursorShape);
			}
		}
	}

	private void UpdateFont()
	{
		_text.Font = Style?.Font;
		_placeholderText.Font = Style?.Font;
	}

	private void ResetCursor()
	{
		_cursorVisible = true;
		_cursorTimer.Restart();
	}

	private FloatRect GetTextRect()
	{
		float top = OuterYoga.LayoutPaddingTop + BorderWidth;
		float bottom = Height - OuterYoga.LayoutPaddingBottom - BorderWidth;
		float left = OuterYoga.LayoutPaddingLeft + BorderWidth;
		float right = Width - OuterYoga.LayoutPaddingRight - BorderWidth;
		float width = Math.Max(0, right - left);
		float height = Math.Max(0, bottom - top);
		return new FloatRect(left, top, width, height);
	}

	private float ToCharacterPosition(int index)
	{
		return _text.FindCharacterPos((uint)index).X;
	}

	private int ToCharacterIndex(float position)
	{
		int left = 0;
		int right = Length;

		while (left < right)
		{
			int mid = left + (right - left) / 2;
			float midPos = ToCharacterPosition(mid);
			if (midPos < position)
				left = mid + 1;
			else
				right = mid;
		}

		int best = left;
		if (best > 0)
		{
			float prevPos = ToCharacterPosition(best - 1);
			float curPos = ToCharacterPosition(best);
			if (MathF.Abs(position - prevPos) <= MathF.Abs(curPos - position))
				--best;
		}

		return ToValidCharacterIndex(best);
	}

	private int ToValidCharacterIndex(int position)
	{
		int clamped = int.Clamp(position, 0, Length);
		return clamped;
	}

	private static int NextWordPosition(int currentPosition, string text)
	{
		int position = currentPosition;
		while (position < text.Length && char.IsWhiteSpace(text[position]))
			position++;

		while (position < text.Length && char.IsLetterOrDigit(text[position]))
			position++;

		if (position == currentPosition)
			position++;

		return position;
	}

	private static int PreviousWordPosition(int currentPosition, string text)
	{
		int position = currentPosition;
		while (position >= 1 && char.IsWhiteSpace(text[position - 1]))
			position--;

		while (position >= 1 && char.IsLetterOrDigit(text[position - 1]))
			position--;

		if (position == currentPosition)
			position--;

		return position;
	}
}