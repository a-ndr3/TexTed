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
using static System.Net.Mime.MediaTypeNames;
using System.Collections;
using System.Data.Common;
using System.Windows.Shapes;
using System.Diagnostics;

namespace TexTed
{
    internal enum Navigation
    {
        UP, DOWN, LEFT, RIGHT
    }
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

        private int selectionStart = -1;
        private int selectionEnd = -1;

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
                var piece = GetPieceFromCaretPosition(caretPosition);

                if (piece != null)
                    DrawCaretN(drawingContext, caretPosition);
            }

            if (selectionStart != -1 && selectionEnd != -1 && selectionStart != selectionEnd)
            {
                DrawSelection(drawingContext, fontSize, typeface);
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
                case Key.Enter:
                    pieceList.Insert(caretPosition, '\n');
                    caretPosition = MoveCaret(Navigation.DOWN);
                    break;
                case Key.Up:
                    caretPosition = MoveCaret(Navigation.UP);
                    break;
                case Key.Down:
                    caretPosition = MoveCaret(Navigation.DOWN);
                    break;
            }

            InvalidateVisual();

        }

        private int MoveCaret(Navigation direction)
        {
            string text = pieceList.GetAllText();
            string[] lines = text.Split('\n');
            int currentLine = 0, charCount = 0, prevLineLength = 0;

            // find the current line
            for (int i = 0; i < lines.Length; i++)
            {
                if (caretPosition <= charCount + lines[i].Length)
                {
                    currentLine = i;
                    break;
                }
                prevLineLength = lines[i].Length + 1; // +1 to count \n
                charCount += prevLineLength;
            }

            // calculate new caret pos
            int lineIndexInText = caretPosition - charCount;

            if (direction == Navigation.UP && currentLine > 0)
            {
                int newPosition = Math.Max(charCount - prevLineLength, charCount - prevLineLength + lineIndexInText);
                return Math.Min(newPosition, charCount - 1); // to get position within the previous line
            }
            else if (direction == Navigation.DOWN && currentLine < lines.Length - 1)
            {
                int nextLineLength = lines[currentLine + 1].Length;
                int newPosition = charCount + lines[currentLine].Length + 1 + lineIndexInText;
                return Math.Min(newPosition, charCount + lines[currentLine].Length + 1 + nextLineLength); //position is within the next line
            }

            return caretPosition;
        }
       
        private void DrawText(DrawingContext drawingContext, string text, Point start, double fontSize, Typeface typeface)
        {
            if (string.IsNullOrEmpty(text)) return;

            GlyphTypeface glyphTypeface;

            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                throw new InvalidOperationException("No glyph typeface found");
            }

            Point currentPoint = start;

            string[] lines = text.Split('\n');

            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    DrawTextLine(drawingContext, line, currentPoint, fontSize, typeface, glyphTypeface);
                }
                currentPoint.Y += fontSize; // move to the next line
            }
        }
        private void DrawTextLine(DrawingContext drawingContext, string text, Point start, double fontSize, Typeface typeface, GlyphTypeface glyphTypeface)
        {
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
            if (glyphIndexes.Count == 0) return;

            GlyphRun glyphRun = new GlyphRun(glyphTypeface, 0, false, fontSize, glyphIndexes.ToArray(), start, advanceWidths.ToArray(), null, null, null, null, null, null);
            drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
        }
        private void DrawCaret(DrawingContext drawingContext, string text, int caretPosition, Point start, double fontSize, Typeface typeface)
        {
            var caretLocation = GetCaretLocation(text, caretPosition, start, fontSize, typeface);

            Point caretStart = new Point(caretLocation.X, caretLocation.Y - fontSize);
            Point caretEnd = new Point(caretLocation.X, caretLocation.Y);

            drawingContext.DrawLine(new Pen(Brushes.Black, 1), caretStart, caretEnd);
        }
       
        private Point GetCaretLocation(string text, int caretPosition, Point start, double fontSize, Typeface typeface)
        {
            string[] lines = text.Split('\n');
            double x = start.X;
            double y = start.Y;
            int accumulatedLength = 0;

            foreach (var line in lines)
            {
                if (accumulatedLength + line.Length >= caretPosition)
                {
                    string substring = line.Substring(0, caretPosition - accumulatedLength);
                    double lineWidth = MeasureTextWidth(substring, fontSize, typeface);
                    x += lineWidth;
                    break;
                }

                accumulatedLength += line.Length + 1; // +1 for the newline character
                y += fontSize; // move to the next line
            }

            return new Point(x, y);
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
       
        private void ClearSelection()
        {
            selectionStart = -1;
            selectionEnd = -1;
        }  
        private void DrawSelection(DrawingContext drawingContext, double fontSize, Typeface typeface)
        {
            string text = pieceList.GetAllText();
            string[] lines = text.Split('\n');
            int start = Math.Min(selectionStart, selectionEnd);
            int end = Math.Max(selectionStart, selectionEnd);
            double y = 0.0;
            int accumulatedLength = 0;

            foreach (var line in lines)
            {
                if (accumulatedLength + line.Length >= start)
                {
                    // Start of selection is in this line
                    int lineStart = accumulatedLength > start ? 0 : start - accumulatedLength;
                    int lineEnd = accumulatedLength + line.Length < end ? line.Length : end - accumulatedLength;
                    string selectedText = line.Substring(lineStart, lineEnd - lineStart);

                    double xStart = MeasureTextWidth(line.Substring(0, lineStart), fontSize, typeface);
                    double xEnd = MeasureTextWidth(line.Substring(0, lineEnd), fontSize, typeface);

                    Rect selectionRect = new Rect(new Point(xStart, y), new Point(xEnd, y + fontSize));

                    drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, 0, 0, 255)), null, selectionRect);

                    if (accumulatedLength + line.Length >= end)
                    {
                        break; // End of selection reached
                    }
                }

                y += fontSize;
                accumulatedLength += line.Length + 1;
            }
        }
       
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Point mousePosition = e.GetPosition(this);
            int clickedPosition = GetCaretIndexFromPoint(mousePosition);

            if (e.ClickCount == 1)
            {
                caretPosition = clickedPosition;
                ClearSelection();

                // selection
                selectionStart = clickedPosition;
                selectionEnd = selectionStart;

            }
            else if (e.ClickCount == 2) // selection of a word
            {
                (selectionStart, selectionEnd) = GetWordBoundaries(clickedPosition);
                caretPosition = selectionEnd;
            }
            
            InvalidateVisual();
            CaptureMouse();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point mousePosition = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed && selectionStart != -1)
            {
                selectionEnd = GetCaretIndexFromPoint(mousePosition);
                caretPosition = selectionEnd;
                InvalidateVisual();
            }
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);          
            ReleaseMouseCapture();
        }
        
        private (int, int) GetWordBoundaries(int position)
        {
            string text = pieceList.GetAllText();
            int start = position;
            int end = position;

            //start of the word
            while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
            {
                start--;
            }

            //end of the word
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            return (start, end);
        }
        private int GetCaretIndexFromPoint(Point point)
        {
            Typeface typeface = new Typeface("Arial"); //todo: change
            double fontSize = 14;//todo: change


            string text = pieceList.GetAllText();
            string[] lines = text.Split('\n');
            double y = 0.0;
            int accumulatedLength = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (point.Y < y + fontSize)
                {
                    // point is within this line
                    string line = lines[i];
                    double x = 0.0;
                    for (int j = 0; j <= line.Length; j++)
                    {
                        double charWidth = (j < line.Length) ? MeasureTextWidth(line[j].ToString(), fontSize, typeface) : fontSize;
                        if (point.X <= x + (charWidth / 2)) // check if the mouse X is before the midpoint of the character
                        {
                            return accumulatedLength + j;
                        }
                        x += charWidth;
                    }
                    return accumulatedLength + line.Length;
                }
                y += fontSize;
                accumulatedLength += lines[i].Length + 1; // +1 for the newline character
            }

            return text.Length; // in case the click is below all the lines
        }
    }
}

// MUST HAVE TODO LIST
//todo: add styles/fonts  //handle styles/fonts for EACH piece, add handling at the beginning of each piece already added fields in Piece class
//todo: add text formatting
//todo: add undo/redo
//todo: add save/load
//todo: add scroll

// OTHER
//todo: other stuff from the file
//todo: add drag&drop text file