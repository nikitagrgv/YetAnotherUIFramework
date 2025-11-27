using System.Diagnostics;
using System.Text;
using Facebook.Yoga;
using SFML.Graphics;
using SFML.System;

namespace SFMLUI;

public class WidgetLabel : Widget
{
	private List<Text> _textRows = new();
	private TextWrapMode _textWrap = TextWrapMode.NoWrap;
	private string _textString = "";
	private Color _textColor = Color.Black;
	private uint _fontSize = 10;

	public enum TextWrapMode
	{
		NoWrap,
		CharWrap,
		WordWrap,
	}

	public Color TextColor
	{
		get => _textColor;
		set => _textColor = value;
	}

	// TODO#
	public bool IsBold { get; set; } = false;
	public bool IsUnderlined { get; set; } = false;
	public bool IsStrikeThrough { get; set; } = false;
	public bool IsItalic { get; set; } = false;
	public float Outline { get; set; } = 0f;
	public float LetterSpacingFactor { get; set; } = 1f;
	public float LineSpacingFactor { get; set; } = 1f;

	public string Text
	{
		get => _textString;
		set
		{
			_textString = value;
			OuterYoga.MarkDirty();
		}
	}

	public uint FontSize
	{
		get => _fontSize;
		set
		{
			_fontSize = value;
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
			OuterYoga.MarkDirty();
		}
	}

	public WidgetLabel()
	{
		OuterYoga.SetMeasureFunction(MeasureFunction);
	}

	protected override bool HandleStyleChangeEvent(StyleChangeEvent e)
	{
		return base.HandleStyleChangeEvent(e);
	}

	protected override void Draw(IPainter painter)
	{
		base.Draw(painter);

		Font? font = Style?.Font;
		if (font == null || _textRows.Count == 0)
			return;

		uint fontSize = FontSize;
		float lineSpacing = font.GetLineSpacing(fontSize);
		float curY = 0;
		foreach (Text line in _textRows)
		{
			FloatRect bounds = line.GetLocalBounds();
			float yOffset = -bounds.Top;
			line.Position = new Vector2f(0f, curY + yOffset);
			painter.Draw(line);
			curY += lineSpacing;
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

	private static FloatRect GetTextRect(string text, TextMetrics textMetrics, uint prevChar)
	{
		float minX = (float)textMetrics.FontSize;
		float minY = (float)textMetrics.FontSize;
		float maxX = 0f;
		float maxY = 0f;

		float x = 0f;
		float y = (float)textMetrics.FontSize;

		foreach (char cur in text)
		{
			if (cur == '\r')
				continue;

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
		}

		if (textMetrics.Outline != 0)
		{
			float outlineWidth = float.Abs(float.Ceiling(textMetrics.Outline));
			minX -= outlineWidth;
			maxX += outlineWidth;
			minY -= outlineWidth;
			maxY += outlineWidth;
		}

		FloatRect rect = new(
			minX,
			minY,
			maxX - minX,
			maxY - minY);
		return rect;
	}

	private static float GetWidth(string t, TextMetrics textMetrics)
	{
		FloatRect rect = GetTextRect(t, textMetrics, prevChar: 0);
		return rect.Width;
	}

	private List<string> WrapText(string text, TextMetrics textMetrics, float maxWidth)
	{
		List<string> textRows = [];

		if (float.IsPositiveInfinity(maxWidth))
		{
			textRows.Add(_textString);
			return textRows;
		}

		string GetLongestSubstring(string s)
		{
			int length = 1;
			while (length < s.Length)
			{
				string substr = s[..length];
				float w = GetWidth(substr, textMetrics);
				if (w >= maxWidth)
					break;
				++length;
			}

			return s[..length];
		}

		string[] originalLines = text.Replace("\r", string.Empty).Split('\n');
		foreach (string line in originalLines)
		{
			string remaining = line;
			while (remaining.Length > 0)
			{
				string substr = GetLongestSubstring(remaining);
				textRows.Add(substr);
				remaining = remaining[substr.Length..];
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