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

	bool isBold { get; set; } = false;
	bool isUnderlined { get; set; } = false;
	bool isStrikeThrough { get; set; } = false;
	bool isItalic { get; set; } = false;

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

	private static List<Text> WrapText(WidgetLabel self, float maxWidth)
	{
		List<Text> textRows = new List<Text>();

		Font? font = self.Style?.Font;
		string text = self._textString;

		if (font == null || string.IsNullOrEmpty(text))
		{
			return textRows;
		}

		if (float.IsPositiveInfinity(maxWidth))
		{
			textRows.Add(new Text());
		}

		uint fontSize = self.FontSize;
		bool isBold = self.isBold;
		bool isUnderlined = self.isUnderlined;
		bool isStrikeThrough = self.isStrikeThrough;
		bool isItalic = self.isItalic;
		float outline = 0f;

		float letterSpacingFactor = 1f;
		float lineSpacingFactor = 1f;

		float whitespaceWidth = font.GetGlyph(' ', fontSize, isBold, outline).Advance;
		float letterSpacing = (whitespaceWidth / 3f) * (letterSpacingFactor - 1f);
		whitespaceWidth += letterSpacing;
		float lineSpacing = font.GetLineSpacing(fontSize) * lineSpacingFactor;

		float x = 0f;
		float y = (float)fontSize;
		uint prevChar = 0;

		float minX = (float)fontSize;
		float minY = (float)fontSize;
		float maxX = 0f;
		float maxY = 0f;

		StringBuilder curLine = new();

		void FinishLine()
		{
			if (curLine.Length > 0)
			{
				textRows.Add(new Text(curLine.ToString(), font, fontSize));
				curLine.Clear();
			}
		}

		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (ch == '\r')
				continue;

			uint cur = ch;

			x += font.GetKerning((char)prevChar, (char)cur, fontSize);

			if (ch == ' ' || ch == '\t' || ch == '\n')
			{
				minX = MathF.Min(minX, x);
				minY = MathF.Min(minY, y);

				switch (ch)
				{
					case ' ':
						x += whitespaceWidth;
						curLine.Append(' ');
						break;
					case '\t':
						x += whitespaceWidth * 4;
						curLine.Append('\t');
						break;
					case '\n':
						FinishLine();

						maxX = MathF.Max(maxX, x);
						maxY = MathF.Max(maxY, y);

						y += lineSpacing;
						x = 0f;
						prevChar = 0;
						continue;
				}

				maxX = MathF.Max(maxX, x);
				maxY = MathF.Max(maxY, y);

				prevChar = cur;
				continue;
			}

			Glyph glyph = font.GetGlyph(ch, fontSize, isBold, outline);
			float advance = glyph.Advance;

			if (x + advance > maxWidth && curLine.Length > 0)
			{
				FinishLine();

				maxX = MathF.Max(maxX, x);
				maxY = MathF.Max(maxY, y);

				y += lineSpacing;
				x = 0f;

				prevChar = 0;

				curLine.Append(ch);
			}
			else
			{
				curLine.Append(ch);
			}

			float left = glyph.Bounds.Left;
			float top = glyph.Bounds.Top;
			float right = glyph.Bounds.Left + glyph.Bounds.Width;
			float bottom = glyph.Bounds.Top + glyph.Bounds.Height;

			minX = MathF.Min(minX, x + left);
			maxX = MathF.Max(maxX, x + right);
			minY = MathF.Min(minY, y + top);
			maxY = MathF.Max(maxY, y + bottom);

			x += advance + letterSpacing;

			prevChar = cur;
		}

		if (curLine.Length > 0)
		{
			textRows.Add(new Text(curLine.ToString(), font, fontSize));
		}

		if (maxX < minX)
		{
			minX = 0f;
			maxX = 0f;
		}

		if (maxY < minY)
		{
			minY = 0f;
			maxY = 0f;
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

		float maxWidth = widthMode == YogaMeasureMode.Undefined ? float.PositiveInfinity : width;
		List<Text> textRows = WrapText(self, maxWidth);

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