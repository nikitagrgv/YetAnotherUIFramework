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

	private FloatRect GetTextRect(string text,
		Font font,
		uint fontSize,
		float whitespaceWidth,
		float letterSpacing,
		float lineSpacing,
		float italicShear,
		bool bold,
		float outline,
		uint prevChar)
	{
		float minX = (float)fontSize;
		float minY = (float)fontSize;
		float maxX = 0f;
		float maxY = 0f;

		float x = 0f;
		float y = (float)fontSize;

		foreach (char cur in text)
		{
			if (cur == '\r')
				continue;

			x += font.GetKerning(prevChar, cur, fontSize);
			prevChar = cur;

			if (cur is ' ' or '\t' or '\n')
			{
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);

				switch (cur)
				{
					case ' ': x += whitespaceWidth; break;
					case '\t': x += whitespaceWidth * 4; break;
					case '\n':
						y += lineSpacing;
						x = 0;
						break;
				}

				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
				continue;
			}

			Glyph glyph = font.GetGlyph(cur, fontSize, bold, outlineThickness: 0f);

			FloatRect glyphBounds = glyph.Bounds;
			float left = glyphBounds.Left;
			float top = glyphBounds.Top;
			float right = glyphBounds.Left + glyphBounds.Width;
			float bottom = glyphBounds.Top + glyphBounds.Height;

			minX = Math.Min(minX, x + left - italicShear * bottom);
			maxX = Math.Max(maxX, x + right - italicShear * top);
			minY = Math.Min(minY, y + top);
			maxY = Math.Max(maxY, y + bottom);
			x += glyph.Advance + letterSpacing;
		}

		if (outline != 0)
		{
			float outlineWidth = float.Abs(float.Ceiling(outline));
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

	private List<string> WrapText(string text, float maxWidth)
	{
		List<string> textRows = [];

		Font? font = Style?.Font;
		if (font == null || string.IsNullOrEmpty(text))
		{
			return textRows;
		}

		if (float.IsPositiveInfinity(maxWidth))
		{
			textRows.Add(_textString);
		}

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

		float width = 0f;
		StringBuilder curLine = new();

		void FinishLine()
		{
			if (curLine.Length > 0)
			{
				textRows.Add(curLine.ToString());
				curLine.Clear();
			}
		}

		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (ch == '\r')
				continue;

			if (ch == '\n')
			{
				FinishLine();
				width = 0f;
				continue;
			}

			if (curLine.Length > 0)
				width += font.GetKerning(curLine[^1], ch, fontSize);

			if (ch == ' ' || ch == '\t')
			{
				switch (ch)
				{
					case ' ':
						width += whitespaceWidth;
						curLine.Append(' ');
						break;
					case '\t':
						width += whitespaceWidth * 4;
						curLine.Append('\t');
						break;
				}

				continue;
			}

			Glyph glyph = font.GetGlyph(ch, fontSize, isBold, outline);
			float advance = glyph.Advance;

			if (width + advance > maxWidth && curLine.Length > 0)
			{
				FinishLine();

				width = MathF.Max(width, width);

				y += lineSpacing;
				width = 0f;

				curLine.Append(ch);
			}
			else
			{
				curLine.Append(ch);
			}

			float right = glyph.Bounds.Left + glyph.Bounds.Width;

			width = MathF.Max(width, width + right);
			width += advance + letterSpacing;
		}

		if (curLine.Length > 0)
			textRows.Add(curLine.ToString());

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

		float maxWidth = widthMode == YogaMeasureMode.Undefined ? float.PositiveInfinity : width;
		List<string> textRows = self.WrapText(self._textString, maxWidth);

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