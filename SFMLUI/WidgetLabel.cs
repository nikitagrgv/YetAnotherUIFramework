using System.Text;
using Facebook.Yoga;
using SFML.Graphics;
using SFML.System;

namespace SFMLUI;

public class WidgetLabel : Widget
{
	private List<Text> _texts = new();
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
		if (font == null)
			return;

		if (_texts.Count == 0)
			return;

		Text first = _texts[0];
		float offset = -first.GetLocalBounds().Position.Y;

		float lineSpacing = font.GetLineSpacing(FontSize);
		float curPos = offset;
		foreach (Text text in _texts)
		{
			text.Position = new Vector2f(0, curPos);
			painter.Draw(text);
			curPos += lineSpacing;
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

	private static YogaSize MeasureFunction(
		YogaNode node,
		float width,
		YogaMeasureMode widthMode,
		float height,
		YogaMeasureMode heightMode)
	{
		WidgetLabel self = (WidgetLabel)node.Data;

		// Fallback natural bounds if no font or text
		float retWidth = 0f;
		float retHeight = 0f;

		List<Text> texts = new List<Text>();

		Font? font = self.Style?.Font;
		string text = self._textString ?? string.Empty;

		if (font == null || string.IsNullOrEmpty(text))
		{
			self._texts = texts;
			// Apply yoga measure constraints and return zero size (or measured from existing local bounds)
			switch (widthMode)
			{
				case YogaMeasureMode.Exactly: retWidth = width; break;
				case YogaMeasureMode.AtMost: retWidth = MathF.Min(retWidth, width); break;
			}

			switch (heightMode)
			{
				case YogaMeasureMode.Exactly: retHeight = height; break;
				case YogaMeasureMode.AtMost: retHeight = MathF.Min(retHeight, height); break;
			}

			return new YogaSize
			{
				width = retWidth,
				height = retHeight
			};
		}

		uint fontSize = self.FontSize;
		bool isBold = self.isBold;
		bool isUnderlined = self.isUnderlined;
		bool isStrikeThrough = self.isStrikeThrough;
		bool isItalic = self.isItalic;
		float outline = 0f;

		// Letter/line spacing factors: use Text properties if available otherwise default to 1
		// If your Text object exposes LetterSpacing or LineSpacing factors use them instead of 1f
		float letterSpacingFactor = 1f;
		float lineSpacingFactor = 1f;

		// Precompute font metrics like SFML does
		float whitespaceWidth = font.GetGlyph(' ', fontSize, isBold, outline).Advance;
		float letterSpacing = (whitespaceWidth / 3f) * (letterSpacingFactor - 1f);
		whitespaceWidth += letterSpacing;
		float lineSpacing = font.GetLineSpacing(fontSize) * lineSpacingFactor;

		// Layout state
		float x = 0f;
		float y = (float)fontSize; // start baseline at char size like SFML
		uint prevChar = 0;

		// Bounds initialization similar to SFML ensureGeometryUpdate
		float minX = (float)fontSize;
		float minY = (float)fontSize;
		float maxX = 0f;
		float maxY = 0f;

		StringBuilder curLine = new StringBuilder();

		// Helper function to finish current line (push Text and reset curLine)
		void FinishLine()
		{
			if (curLine.Length > 0)
			{
				texts.Add(new Text(curLine.ToString(), font, fontSize));
				curLine.Clear();
			}
		}

		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (ch == '\r')
				continue;

			uint cur = ch;

			// apply kerning from previous character
			x += font.GetKerning((char)prevChar, (char)cur, fontSize);

			// Special handling: whitespace, tab, newline
			if (ch == ' ' || ch == '\t' || ch == '\n')
			{
				// update min/max bounds before we advance
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
						// newline -> push line, advance y, reset x
						FinishLine();

						// update bounds for end of line
						maxX = MathF.Max(maxX, x);
						maxY = MathF.Max(maxY, y);

						y += lineSpacing;
						x = 0f;
						// after a newline we reset previous char so kerning does not leak
						prevChar = 0;
						continue; // skip the prevChar update at bottom since we already set it
				}

				// update max bounds after advancing
				maxX = MathF.Max(maxX, x);
				maxY = MathF.Max(maxY, y);

				prevChar = cur;
				continue;
			}

			// For normal glyphs compute glyph advance and bounds
			Glyph glyph = font.GetGlyph(ch, fontSize, isBold, outline);
			float advance = glyph.Advance;

			// If wrapping width given and we would exceed it, break to new line
			if (width > 0 && x + advance > width && curLine.Length > 0)
			{
				// finalize current line
				FinishLine();

				// update bounds for finished line
				maxX = MathF.Max(maxX, x);
				maxY = MathF.Max(maxY, y);

				// move to next line
				y += lineSpacing;
				x = 0f;

				// reset kerning behaviour (as in SFML newline)
				prevChar = 0;

				// start new line with current character (do not advance prevChar yet)
				// apply kerning from prevChar==0 will be zero
				// we must append the current character and advance x by glyph.advance later
				curLine.Append(ch);
				// update glyph bounds for the new line as below
			}
			else
			{
				// normal append to current line
				curLine.Append(ch);
			}

			// Update bounds using glyph metrics
			float left = glyph.Bounds.Left;
			float top = glyph.Bounds.Top;
			float right = glyph.Bounds.Left + glyph.Bounds.Width;
			float bottom = glyph.Bounds.Top + glyph.Bounds.Height;

			// Note: SFML accounts for italic shear in x positions when computing min/max.
			// For layout width/height we can ignore small italic shear or approximate as zero.
			minX = MathF.Min(minX, x + left);
			maxX = MathF.Max(maxX, x + right);
			minY = MathF.Min(minY, y + top);
			maxY = MathF.Max(maxY, y + bottom);

			// Advance to next char position including letter spacing
			x += advance + letterSpacing;

			prevChar = cur;
		}

		// push last line
		if (curLine.Length > 0)
		{
			texts.Add(new Text(curLine.ToString(), font, fontSize));
		}

		// If no glyphs were processed, set min/max to 0 so width/height become 0
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

		// If outline or thickness were used you should expand the bounds here similar to SFML.
		// (Not included by default but easy to add if you track outline thickness)

		// Compute final measured width/height from bounds
		retWidth = maxX - minX;
		retHeight = maxY - minY;

		// If there was at least one line but width computed as zero, fallback to width of widest created Text using GetLocalBounds
		if (retWidth <= 0f && texts.Count > 0)
		{
			float fallbackMax = 0f;
			foreach (var t in texts)
			{
				var b = t.GetLocalBounds();
				fallbackMax = MathF.Max(fallbackMax, b.Width);
			}

			retWidth = fallbackMax;
		}

		// store generated line Texts on the widget for rendering
		self._texts = texts;

		// Apply Yoga measure constraints
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