using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.TextFormatting;

namespace TexTed
{
    internal class TextViewer : Control
    {
        private PieceList pieceList;
        private int caretPosition;
        private bool showCaret;
        public string FilePath
        {
            set
            {
                pieceList = new PieceList(value);
            }
        }
        public TextViewer()
        {
            this.caretPosition = 0;
            this.showCaret = true;
            this.Focusable = true;

            this.Loaded += (sender, args) => Keyboard.Focus(this);

            InitializeCaretBlinking();
        }

        private void InitializeCaretBlinking()
        {
            DispatcherTimer caretTimer = new DispatcherTimer();

            caretTimer.Interval = TimeSpan.FromMilliseconds(500);

            caretTimer.Tick += (sender, e) =>
            {
                showCaret = !showCaret;
                InvalidateVisual(); // redraw
            };
            caretTimer.Start();
        }

        //todo: check render details because now I pass font/size
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Typeface typeface = new Typeface("Arial");

            double fontSize = 14;

            Point start = new Point(0, fontSize);

            string text = pieceList.GetAllText();

            DrawText(drawingContext, text, start, fontSize, typeface);

            if (showCaret)
            {
                DrawCaret(drawingContext, text, caretPosition, start, fontSize, typeface);
            }
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            foreach (char c in e.Text)
            {
                if (!char.IsControl(c))
                {
                    pieceList.Insert(caretPosition, c);
                    caretPosition++;
                }
            }

            InvalidateVisual();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            string text = pieceList.GetAllText();
            int textLength = text.IndexOf('\0');
            if (textLength == -1) textLength = text.Length;

            switch (e.Key)
            {
                case Key.Left:
                    if (caretPosition > 0)
                    {
                        caretPosition--;
                    }
                    break;
                case Key.Right:
                    if (caretPosition < textLength)
                    {
                        caretPosition++;
                    }
                    break;
                case Key.Back:
                    if (caretPosition > 0)
                    {
                        pieceList.Delete(caretPosition - 1, caretPosition);
                        caretPosition--;
                    }
                    break;
            }

            InvalidateVisual();

        }


        private void DrawText(DrawingContext drawingContext, string text, Point start, double fontSize, Typeface typeface)
        {
            if (string.IsNullOrEmpty(text)) return;

            GlyphTypeface glyphTypeface;
            
            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                throw new InvalidOperationException("No glyph typeface found");
            }

            List<ushort> glyphIndexes = new List<ushort>();
            List<double> advanceWidths = new List<double>();

            foreach (char c in text)
            {
                if (char.IsControl(c)) continue;

                if (glyphTypeface.CharacterToGlyphMap.TryGetValue(c, out ushort glyphIndex))
                {
                    glyphIndexes.Add(glyphIndex);
                    advanceWidths.Add(glyphTypeface.AdvanceWidths[glyphIndex] * fontSize);
                }

            }

            GlyphRun glyphRun = new GlyphRun(glyphTypeface, 0, false, fontSize, glyphIndexes.ToArray(), start, advanceWidths.ToArray(), null, null, null, null, null, null);
            drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
        }

        private void DrawCaret(DrawingContext drawingContext, string text, int caretPosition, Point start, double fontSize, Typeface typeface)
        {
            double caretX = GetCaretXPosition(text, caretPosition, fontSize, typeface);

            Point caretStart = new Point(start.X + caretX, start.Y - fontSize);
            Point caretEnd = new Point(start.X + caretX, start.Y);

            drawingContext.DrawLine(new Pen(Brushes.Black, 1), caretStart, caretEnd);
        }

        private double GetCaretXPosition(string text, int caretPosition, double fontSize, Typeface typeface)
        {
            string substring = text.Substring(0, caretPosition);
            return MeasureTextWidth(substring, fontSize, typeface);
        }

        private double MeasureTextWidth(string text, double fontSize, Typeface typeface)
        {
            GlyphTypeface glyphTypeface;
            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                throw new InvalidOperationException("No glyph typeface found");
            }

            double width = 0;
            foreach (char c in text)
            {
                if (char.IsControl(c)) continue;

                if (glyphTypeface.CharacterToGlyphMap.TryGetValue(c, out ushort glyphIndex))
                {
                    width += glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                }

            }
            return width;
        }

    }
}

// MUST HAVE TODO LIST
//todo: add selection
//todo: add styles/fonts  //handle styles/fonts for EACH piece, add handling at the beginning of each piece already added fields in Piece class
//todo: add text formatting
//todo: add undo/redo
//todo: add save/load
//todo: add scroll
//todo: long lines are not wrapped but simply clipped at the right margin
//todo: select words with double click


// OTHER
//todo: other stuff from the file