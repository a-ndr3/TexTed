﻿using System;
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

            var head = pieceList.GetHead();
            double lineHeight = GetMaxHeightStartingFromPiece(head);
            Point start = new Point(0, lineHeight);

            while (head != null)
            {
                var typeFace = new Typeface(head.Font, head.Style, FontWeights.Normal, FontStretches.Normal);
                string text = head.GetText();

                string[] lines = text.Split('\n');

                GlyphTypeface glyphTypeface;

                if (!typeFace.TryGetGlyphTypeface(out glyphTypeface))
                {
                    throw new InvalidOperationException("No glyph typeface found");
                }

                var i = 0;
                foreach (var line in lines)
                {
                    lineHeight = GetMaxHeightStartingFromPiece(head);

                    if (!string.IsNullOrEmpty(line))
                    {
                        DrawTextLine(drawingContext, line, start, head.FontSize, glyphTypeface);
                        start.X += MeasureTextWidth(line, head.FontSize, typeFace);
                    }

                    if (i > 0)
                    {
                        start.Y += lineHeight; start.X = 0;
                    }
                    i++;
                }

                head = head.Next;
            }

            if (showCaret)
            {
                var piece = GetPieceFromCaretPosition(caretPosition);
                lineHeight = GetMaxHeightStartingFromPiece(piece);

                if (piece != null)
                    DrawCaretN(drawingContext, piece, caretPosition, lineHeight);
                else
                {

                }
            }

            if (selectionStart != -1 && selectionEnd != -1 && selectionStart != selectionEnd)
            {
                //DrawSelection(drawingContext, head.FontSize, typeFace);
            }

        }

        protected void DrawCaretN(DrawingContext drawingContext, Piece piece, int caretPosition, double lineHeight)
        {
            var caretLocation = GetCaretPoint(caretPosition);

            Point caretStart;
            Point caretEnd;


            caretStart = new Point(caretLocation.X, caretLocation.Y + lineHeight - piece.FontSize);
            caretEnd = new Point(caretLocation.X, caretLocation.Y + lineHeight);


            drawingContext.DrawLine(new Pen(Brushes.Black, 1), caretStart, caretEnd);
        }


        private Piece GetPieceFromCaretPosition(int caretPosition)
        {
            Piece currentPiece = pieceList.GetHead();
            int accumulatedLength = 0;

            while (currentPiece != null)
            {
                int pieceLength = currentPiece.Length;
                if (accumulatedLength + pieceLength > caretPosition)
                {
                    return currentPiece;
                }
                accumulatedLength += pieceLength;

                if (currentPiece.Next == null) return currentPiece;

                currentPiece = currentPiece.Next;
            }

            return currentPiece;
        }

        private Point GetCaretPoint(int caretPosition)
        {
            Piece currentPiece = pieceList.GetHead();
            int accumulatedLength = 0;
            double x = 0.0;
            double y = 0.0;
            int maxHeight = 0;

            while (currentPiece != null)
            {
                string currentText = currentPiece.GetText();
                int pieceLength = currentText.Length;
                maxHeight = Math.Max(maxHeight, currentPiece.FontSize);

                if (accumulatedLength + pieceLength >= caretPosition)
                {
                    string textUpToCaret = currentText.Substring(0, caretPosition - accumulatedLength);
                    x += MeasureTextWidth(textUpToCaret, currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));
                    break;
                }

                accumulatedLength += pieceLength;
                x += MeasureTextWidth(currentText, currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));

                if (currentText.Contains("\n"))
                {
                    y += maxHeight;
                    x = 0;
                }

                currentPiece = currentPiece.Next;
            }

            return new Point(x, y);
        }



        //влево и врпаво до \n
        private int GetMaxHeightStartingFromPiece(Piece piece)
        {
            var maxHeight = 1; var localPiece = piece;

            while (localPiece != null)
            {
                var text = localPiece.GetText();
                var lastIndexOfn = text.LastIndexOf("\n");

                if (lastIndexOfn != -1)
                {
                    if (text.Length != lastIndexOfn + 1)
                    {
                        if (localPiece.FontSize > maxHeight)
                        {
                            maxHeight = localPiece.FontSize;
                        }
                    }

                    break;
                }

                if (localPiece.FontSize > maxHeight)
                {
                    maxHeight = localPiece.FontSize;
                }

                localPiece = localPiece.Prev;
            }

            while (piece != null)
            {
                var text = piece.GetText();

                if (piece.FontSize > maxHeight)
                {
                    maxHeight = piece.FontSize;
                }

                if (text.Contains("\n"))
                {
                    break;
                }

                piece = piece.Next;
            }
            return maxHeight;
        }

        private List<double> CalculateLineHeights()
        {
            List<double> lineHeights = new List<double>();
            double currentLineHeight = 0.0;
            Piece currentPiece = pieceList.GetHead();

            while (currentPiece != null)
            {
                currentLineHeight = Math.Max(currentLineHeight, currentPiece.FontSize);
                string text = currentPiece.GetText();

                if (text.Contains("\n"))
                {
                    lineHeights.Add(currentLineHeight);
                    currentLineHeight = 0.0;
                }

                currentPiece = currentPiece.Next;
            }

            if (currentLineHeight > 0)
            {
                lineHeights.Add(currentLineHeight);
            }

            return lineHeights;
        }






        #region Event_Controls
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
                    caretPosition++;
                    //caretPosition = MoveCaret(Navigation.DOWN);
                    break;
                case Key.Up:
                    //caretPosition = MoveCaret(Navigation.UP);
                    break;
                case Key.Down:
                    //caretPosition = MoveCaret(Navigation.DOWN);
                    break;
            }

            InvalidateVisual();

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

        #endregion


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
            else if (direction == Navigation.DOWN && currentLine <= lines.Length)
            {
                int nextLineLength = lines[currentLine].Length;
                int newPosition = charCount + lines[currentLine].Length + 1 + lineIndexInText;
                return Math.Min(newPosition, charCount + lines[currentLine].Length + 1 + nextLineLength); //position is within the next line
            }

            return caretPosition;
        }


        private void DrawTextLine(DrawingContext drawingContext, string text, Point start, int fontSize, GlyphTypeface glyphTypeface)
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



        #region Selection

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
            //get font and size from piece that is closes to the point
            var typeface = GetSpecificTypeFace(point);
            double fontSize = typeface.XHeight;//todo: change


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
        private Typeface GetSpecificTypeFace(Point point)
        {
            double accumulatedWidth = 0;
            Piece currentPiece = pieceList.GetHead();

            while (currentPiece != null)
            {
                string text = currentPiece.GetText();
                double textWidth = MeasureTextWidth(text, currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));

                // check if the point's X coordinate falls within the current piece
                if (point.X >= accumulatedWidth && point.X < accumulatedWidth + textWidth)
                {
                    return new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal);
                }

                accumulatedWidth += textWidth;
                currentPiece = currentPiece.Next;
            }

            return new Typeface("Arial");
        }

        #endregion


        #region NotUsed
        //---------------NOT_USED----------------------
        private double GetFontSizeAtPosition(Piece head, int caretPosition)
        {
            double defaultFontSize = 12;

            int accumulatedLength = 0;
            while (head != null)
            {
                int pieceEnd = accumulatedLength + head.GetText().Length;
                if (caretPosition <= pieceEnd)
                {
                    return head.FontSize;
                }
                accumulatedLength = pieceEnd;
                head = head.Next;
            }

            return defaultFontSize;
        }


        private double GetAllTextWidthInLine(Piece piece)
        {
            double result = 0.0;
            while (piece != null)
            {
                if (!piece.GetText().Contains("\n"))
                {
                    result += MeasureTextWidth(piece.GetText(), piece.FontSize, new Typeface(piece.Font, piece.Style, FontWeights.Normal, FontStretches.Normal));
                }
                else
                {
                    result += MeasureTextWidth(piece.GetText().Substring(piece.GetText().LastIndexOf("\n")), piece.FontSize, new Typeface(piece.Font, piece.Style, FontWeights.Normal, FontStretches.Normal));
                    break;
                }
                piece = piece.Prev;
            }
            return result;
        }

        #endregion
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