using System.Diagnostics;
using System.Text;
using Facebook.Yoga;
using SFML.Graphics;
using SFML.System;

namespace SFMLUI;

public class WidgetLabel : Widget
{
	private readonly List<Text> _textRows = new();

	private TextWrapMode _textWrap = TextWrapMode.NoWrap;

	private string _textString = "";

	private Color _textColor = Color.White;
	private Color _outlineColor = Color.White;

	private uint _fontSize = 10;

	private bool _needUpdateText = true;

	private bool _isBold = false;
	private bool _isUnderlined = false;
	private bool _isStrikeThrough = false;
	private bool _isItalic = false;
	private float _outline = 0f;
	private float _letterSpacingFactor = 1f;
	private float _lineSpacingFactor = 1f;

	public enum TextWrapMode
	{
		NoWrap,
		CharWrap,
		WordWrap,
	}

	public Color TextColor
	{
		get => _textColor;
		set
		{
			_textColor = value;
			_needUpdateText = true;
		}
	}

	public Color OutlineColor
	{
		get => _outlineColor;
		set
		{
			_outlineColor = value;
			_needUpdateText = true;
		}
	}

	public bool IsBold
	{
		get => _isBold;
		set
		{
			_isBold = value;
			_needUpdateText = true;
		}
	}

	public bool IsUnderlined
	{
		get => _isUnderlined;
		set
		{
			_isUnderlined = value;
			_needUpdateText = true;
		}
	}

	public bool IsStrikeThrough
	{
		get => _isStrikeThrough;
		set
		{
			_isStrikeThrough = value;
			_needUpdateText = true;
		}
	}

	public bool IsItalic
	{
		get => _isItalic;
		set
		{
			_isItalic = value;
			_needUpdateText = true;
		}
	}

	public float Outline
	{
		get => _outline;
		set
		{
			_outline = value;
			_needUpdateText = true;
		}
	}

	public float LetterSpacingFactor
	{
		get => _letterSpacingFactor;
		set
		{
			_letterSpacingFactor = value;
			_needUpdateText = true;
		}
	}

	public float LineSpacingFactor
	{
		get => _lineSpacingFactor;
		set
		{
			_lineSpacingFactor = value;
			_needUpdateText = true;
		}
	}

	public string Text
	{
		get => _textString;
		set
		{
			_textString = value;
			_needUpdateText = true;
			OuterYoga.MarkDirty();
		}
	}

	public uint FontSize
	{
		get => _fontSize;
		set
		{
			_fontSize = value;
			_needUpdateText = true;
			OuterYoga.MarkDirty();
		}
	}

	public TextWrapMode TextWrap
	{
		get => _textWrap;
		set
		{
			if (value == _textWrap)
				return;
			_textWrap = value;
			_needUpdateText = true;
			OuterYoga.MarkDirty();
		}
	}

	public WidgetLabel()
	{
		OuterYoga.SetMeasureFunction(MeasureFunction);

		FillColor = Color.Transparent;
		TextColor = Color.White;
		FontSize = 22;
	}

	protected override bool HandleStyleChangeEvent(StyleChangeEvent e)
	{
		_needUpdateText = true;
		return base.HandleStyleChangeEvent(e);
	}

	protected override bool HandleLayoutChangeEvent(LayoutChangeEvent e)
	{
		_needUpdateText = true;
		return base.HandleLayoutChangeEvent(e);
	}

	protected override void Draw(IPainter painter)
	{
		base.Draw(painter);

		Font? font = Style?.Font;
		if (font == null || _textString.Length == 0)
			return;

		TextMetrics textMetrics = GetTextMetrics(font);
		if (_needUpdateText)
		{
			_needUpdateText = false;
			_textRows.Clear();
			List<string> lines = WrapText(_textString, textMetrics, Width);

			Text.Styles styles = SFML.Graphics.Text.Styles.Regular;
			if (IsBold)
				styles |= SFML.Graphics.Text.Styles.Bold;
			if (IsItalic)
				styles |= SFML.Graphics.Text.Styles.Italic;
			if (IsUnderlined)
				styles |= SFML.Graphics.Text.Styles.Underlined;
			if (IsStrikeThrough)
				styles |= SFML.Graphics.Text.Styles.StrikeThrough;

			foreach (string line in lines)
			{
				Text t = new(line, font, FontSize);
				t.Style = styles;
				t.OutlineThickness = Outline;
				t.FillColor = TextColor;
				t.OutlineColor = OutlineColor;
				t.LineSpacing = LineSpacingFactor;
				t.LetterSpacing = LetterSpacingFactor;
				_textRows.Add(t);
			}
		}

		float currentY = 0;
		for (int i = 0; i < _textRows.Count; i++)
		{
			Text row = _textRows[i];
			FloatRect bounds = row.GetLocalBounds();

			if (i == 0)
			{
				currentY -= bounds.Top;
			}

			row.Position = new Vector2f(-bounds.Left, currentY);
			painter.Draw(row);
			currentY += textMetrics.LineSpacing;
		}
	}

	public override bool AcceptsMouse(float x, float y)
	{
		return false;
	}

	protected override bool HandleMousePressEvent(MousePressEvent e)
	{
		base.HandleMousePressEvent(e);
		return false;
	}

	protected override bool HandleMouseReleaseEvent(MouseReleaseEvent e)
	{
		base.HandleMouseReleaseEvent(e);
		return false;
	}

	private struct TextMetrics(
		Font font,
		uint fontSize,
		bool isBold,
		float italicShear,
		float outline,
		float letterSpacingFactor,
		float lineSpacingFactor,
		float whitespaceWidth,
		float letterSpacing,
		float lineSpacing)
	{
		public Font Font { get; set; } = font;
		public uint FontSize { get; set; } = fontSize;
		public bool IsBold { get; set; } = isBold;
		public float ItalicShear { get; set; } = italicShear;
		public float Outline { get; set; } = outline;
		public float LetterSpacingFactor { get; set; } = letterSpacingFactor;
		public float LineSpacingFactor { get; set; } = lineSpacingFactor;
		public float WhitespaceWidth { get; set; } = whitespaceWidth;
		public float LetterSpacing { get; set; } = letterSpacing;
		public float LineSpacing { get; set; } = lineSpacing;
	}

	private TextMetrics GetTextMetrics(Font font)
	{
		uint fontSize = FontSize;
		bool isBold = IsBold;
		float italicShear = IsItalic ? 0.209f : 0f; // Hardcoded values from SFML
		float outline = Outline;
		float letterSpacingFactor = LetterSpacingFactor;
		float lineSpacingFactor = LineSpacingFactor;

		float whitespaceWidth = font.GetGlyph(' ', fontSize, isBold, outlineThickness: 0f).Advance;
		float letterSpacing = (whitespaceWidth / 3f) * (letterSpacingFactor - 1f);
		whitespaceWidth += letterSpacing;
		float lineSpacing = font.GetLineSpacing(fontSize) * lineSpacingFactor;

		return new TextMetrics(
			font,
			fontSize,
			isBold,
			italicShear,
			outline,
			letterSpacingFactor,
			lineSpacingFactor,
			whitespaceWidth,
			letterSpacing,
			lineSpacing
		);
	}

	private static FloatRect GetOutlinedTextRect(float minX,
		float minY,
		float maxX,
		float maxY, float outlineWidth)
	{
		FloatRect rect = new(
			minX - outlineWidth,
			minY - outlineWidth,
			maxX - minX + outlineWidth * 2,
			maxY - minY + outlineWidth * 2);
		return rect;
	}

	private static IEnumerator<FloatRect> IterateTextRect(string text, TextMetrics textMetrics, uint prevChar)
	{
		float minX = (float)textMetrics.FontSize;
		float minY = (float)textMetrics.FontSize;
		float maxX = 0f;
		float maxY = 0f;

		float outlineWidth = textMetrics.Outline == 0 ? 0 : float.Abs(float.Ceiling(textMetrics.Outline));

		float x = 0f;
		float y = (float)textMetrics.FontSize;

		foreach (char cur in text)
		{
			if (cur == '\r')
			{
				yield return GetOutlinedTextRect(minX, minY, maxX, maxY, outlineWidth);
				continue;
			}

			x += textMetrics.Font.GetKerning(prevChar, cur, textMetrics.FontSize);
			prevChar = cur;

			if (cur is ' ' or '\t' or '\n')
			{
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);

				switch (cur)
				{
					case ' ': x += textMetrics.WhitespaceWidth; break;
					case '\t': x += textMetrics.WhitespaceWidth * 4; break;
					case '\n':
						y += textMetrics.LineSpacing;
						x = 0;
						break;
				}

				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
				yield return GetOutlinedTextRect(minX, minY, maxX, maxY, outlineWidth);
				continue;
			}

			Glyph glyph =
				textMetrics.Font.GetGlyph(cur, textMetrics.FontSize, textMetrics.IsBold, outlineThickness: 0f);

			FloatRect glyphBounds = glyph.Bounds;
			float left = glyphBounds.Left;
			float top = glyphBounds.Top;
			float right = glyphBounds.Left + glyphBounds.Width;
			float bottom = glyphBounds.Top + glyphBounds.Height;

			minX = Math.Min(minX, x + left - textMetrics.ItalicShear * bottom);
			maxX = Math.Max(maxX, x + right - textMetrics.ItalicShear * top);
			minY = Math.Min(minY, y + top);
			maxY = Math.Max(maxY, y + bottom);
			x += glyph.Advance + textMetrics.LetterSpacing;
			yield return GetOutlinedTextRect(minX, minY, maxX, maxY, outlineWidth);
		}
	}

	private static FloatRect GetFullTextRect(string text, TextMetrics textMetrics, uint prevChar)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new FloatRect();
		}

		IEnumerator<FloatRect> enumerator = IterateTextRect(text, textMetrics, prevChar);
		return enumerator.GetLast();
	}

	private static int GetNextWordIndex(string text, int curIndex)
	{
		int nextIndex = curIndex + 1;
		while (nextIndex < text.Length && char.IsLetter(text[nextIndex]))
			nextIndex++;
		while (nextIndex < text.Length && char.IsWhiteSpace(text[nextIndex]))
			nextIndex++;
		return nextIndex;
	}

	private static int GetLongestLength(string s, TextMetrics textMetrics, float maxWidth, TextWrapMode wrapMode)
	{
		int length = 0;
		IEnumerator<FloatRect> rectEnumerator = IterateTextRect(s, textMetrics, prevChar: 0);
		while (rectEnumerator.MoveNext())
		{
			int newLength = wrapMode == TextWrapMode.WordWrap ? GetNextWordIndex(s, length) : length + 1;

			FloatRect rect = rectEnumerator.Current;
			if (rect.Width > maxWidth)
			{
				break;
			}

			length = newLength;
		}

		return length;
	}


	private List<string> WrapText(string text, TextMetrics textMetrics, float maxWidth)
	{
		// TODO: Really shitty algorythm

		List<string> textRows = [];

		if (float.IsPositiveInfinity(maxWidth) || TextWrap == TextWrapMode.NoWrap)
		{
			textRows.Add(_textString);
			return textRows;
		}

		string[] originalLines = text.Replace("\r", string.Empty).Split('\n');
		foreach (string line in originalLines)
		{
			string remaining = line;
			while (remaining.Length > 0)
			{
				int length = GetLongestLength(remaining, textMetrics, maxWidth, TextWrap);
				length = Math.Max(1, length);
				string substr = remaining[..length];
				textRows.Add(substr);
				remaining = remaining[length..];
			}
		}

		return textRows;
	}

	private static YogaSize MeasureFunction(
		YogaNode node,
		float width,
		YogaMeasureMode widthMode,
		float height,
		YogaMeasureMode heightMode)
	{
		WidgetLabel self = (WidgetLabel)node.Data;

		float retWidth = 0f;
		float retHeight = 0f;

		Font? font = self.Style?.Font;
		if (font != null)
		{
			float maxWidth = widthMode == YogaMeasureMode.Undefined ? float.PositiveInfinity : width;
			TextMetrics textMetrics = self.GetTextMetrics(font);
			List<string> textRows = self.WrapText(self._textString, textMetrics, maxWidth);

			float top = 0;
			float bottom = 0;
			for (int i = 0; i < textRows.Count; i++)
			{
				string row = textRows[i];
				FloatRect rect = GetFullTextRect(row, textMetrics, 0);
				retWidth = float.Max(retWidth, rect.Width);

				if (i == 0)
				{
					top = rect.Top;
				}

				if (i == textRows.Count - 1)
				{
					bottom = i * textMetrics.LineSpacing + rect.Top + rect.Height;
				}
			}

			retHeight = bottom - top;
		}

		switch (widthMode)
		{
			case YogaMeasureMode.Undefined:
				break;
			case YogaMeasureMode.Exactly:
				retWidth = width;
				break;
			case YogaMeasureMode.AtMost:
				retWidth = MathF.Min(retWidth, width);
				break;
		}

		switch (heightMode)
		{
			case YogaMeasureMode.Undefined:
				break;
			case YogaMeasureMode.Exactly:
				retHeight = height;
				break;
			case YogaMeasureMode.AtMost:
				retHeight = MathF.Min(retHeight, height);
				break;
		}

		return new YogaSize
		{
			width = retWidth,
			height = retHeight
		};
	}
}