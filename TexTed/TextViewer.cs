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
using System.Diagnostics;
using TexTed.Commands;
using ICommand = TexTed.Commands.ICommand;
using TexTed.Clipboard;
using TexTed.PieceBase;

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
            get
            {
                return pieceList.GetFilePath();
            }
        }

        public const int lineOffset = 5;

        private int selectionStart = -1;
        private int selectionEnd = -1;

        private Stack<ICommand> undoStack = new Stack<ICommand>();
        private Stack<ICommand> redoStack = new Stack<ICommand>();

        public TextViewer()
        {
            caretPosition = 0;
            showCaret = true;
            Focusable = true;

            Loaded += (sender, args) => Keyboard.Focus(this);

            InitializeCaretBlinking();
        }

        private void InitializeCaretBlinking()
        {
            DispatcherTimer caretTimer = new DispatcherTimer();

            caretTimer.Interval = TimeSpan.FromMilliseconds(500);

            caretTimer.Tick += (sender, e) =>
            {
                showCaret = !showCaret;
                InvalidateVisual();
            };
            caretTimer.Start();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            try
            {
                var head = pieceList.GetHead();
                double lineHeight = GetLineHeight(head);
                Point start = new Point(0, lineHeight);

                while (head != null)
                {
                    var typeFace = new Typeface(head.Font, head.Style, FontWeights.Normal, FontStretches.Normal);

                    string text = head.GetText();

                    var lines = text.Split('\n');

                    GlyphTypeface glyphTypeface;

                    if (!typeFace.TryGetGlyphTypeface(out glyphTypeface))
                    {
                        throw new InvalidOperationException("No glyph typeface found");
                    }

                    var i = 0;
                    foreach (var line in lines)
                    {
                        lineHeight = GetLineHeight(head);

                        if (i > 0)
                        {
                            start.Y += GetLineHeight(head.Next ?? head); start.X = 0;
                        }
                        i++;

                        if (!string.IsNullOrEmpty(line))
                        {
                            DrawTextLine(drawingContext, line, start, head.FontSize, glyphTypeface);
                            start.X += MeasureTextWidth(line, head.FontSize, typeFace);
                        }
                    }

                    head = head.Next;
                }

                if (showCaret)
                {
                    var piece = GetPieceFromCaretPosition(caretPosition);
                    var caretLocation = GetCaretPointUpd(caretPosition);
                    lineHeight = GetLineHeight(piece.Next ?? piece);

                    DrawCaretN(drawingContext, piece, caretLocation, lineHeight);
                }

                if (selectionStart != -1 && selectionEnd != -1 && selectionStart != selectionEnd)
                {
                    DrawSelection(drawingContext);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        public void SaveFile()
        {
            pieceList.SaveFile();
            pieceList = new PieceList(FilePath); //to redraw with new pos
        }

        private int GetLineHeight(Piece piece)
        {
            return GetMaxHeightStartingFromPiece(piece) + lineOffset;
        }

        private int GetMinLineHeight(Piece piece)
        {
            return GetMinHeightStartingFromPiece(piece) + lineOffset;
        }

        protected void DrawCaretN(DrawingContext drawingContext, Piece piece, Point caretLocation, double lineHeight)
        {
            Point caretStart, caretEnd;

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
                if (accumulatedLength + pieceLength >= caretPosition)
                {
                    return currentPiece;
                }
                accumulatedLength += pieceLength;

                if (currentPiece.Next == null) return currentPiece;

                currentPiece = currentPiece.Next;
            }

            return currentPiece;
        }

        private Point GetCaretPointUpd(int caretPosition)
        {
            Piece currentPiece = pieceList.GetHead();
            int accumulatedLength = 0;
            double x = 0.0;
            double y = 0.0;
            double maxLineHeight = 0.0;

            while (currentPiece != null)
            {
                string currentText = currentPiece.GetText();

                var lineHeight = GetLineHeight(currentPiece);

                Typeface typeface = new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal);

                for (int i = 0; i < currentText.Length && accumulatedLength + i < caretPosition; i++)
                {
                    maxLineHeight = Math.Max(maxLineHeight, lineHeight);

                    if (currentText[i] == '\n')
                    {
                        y += maxLineHeight;
                        x = 0;
                        maxLineHeight = 0;
                    }
                    else
                    {
                        x += MeasureTextWidth(currentText[i].ToString(), currentPiece.FontSize, typeface);
                    }
                }

                if (accumulatedLength + currentText.Length >= caretPosition)
                {
                    if (caretPosition == accumulatedLength + currentText.Length && currentText.EndsWith("\n"))
                    {
                        y += maxLineHeight;
                        x = 0;
                    }
                    break;
                }

                accumulatedLength += currentText.Length;
                if (currentText.EndsWith("\n"))
                {
                    y += maxLineHeight;
                    x = 0;
                    maxLineHeight = 0;
                }

                currentPiece = currentPiece.Next;
            }

            return new Point(x, y);
        }



        //влево и вправо до \n
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

        private int GetMinHeightStartingFromPiece(Piece piece)
        {
            var minHeight = 1000; var localPiece = piece;

            while (localPiece != null)
            {
                var text = localPiece.GetText();
                var lastIndexOfn = text.LastIndexOf("\n");

                if (lastIndexOfn != -1)
                {
                    if (text.Length != lastIndexOfn + 1)
                    {
                        if (localPiece.FontSize < minHeight)
                        {
                            minHeight = localPiece.FontSize;
                        }
                    }

                    break;
                }

                if (localPiece.FontSize < minHeight)
                {
                    minHeight = localPiece.FontSize;
                }

                localPiece = localPiece.Prev;
            }

            while (piece != null)
            {
                var text = piece.GetText();

                if (piece.FontSize < minHeight)
                {
                    minHeight = piece.FontSize;
                }

                if (text.Contains("\n"))
                {
                    break;
                }

                piece = piece.Next;
            }
            return minHeight;
        }

        private List<(double, double)> CalculateLineHeights()
        {
            var lineHeights = new List<(double, double)>();
            Piece currentPiece = pieceList.GetHead();

            while (currentPiece != null)
            {
                if (lineHeights.Count == 0)
                {
                    lineHeights.Add((GetMinLineHeight(currentPiece), GetLineHeight(currentPiece)));
                }

                if (currentPiece.GetText().Contains("\n"))
                {
                    lineHeights.Add((GetMinLineHeight(currentPiece.Next), GetLineHeight(currentPiece.Next)));
                }

                currentPiece = currentPiece.Next;
            }

            return lineHeights;
        }


        #region Event_Controls
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            //foreach (char c in e.Text)
            //{
            //    if (!char.IsControl(c))
            //    {
            //        pieceList.InsertUpdated(caretPosition, c);
            //        caretPosition++;
            //    }
            //}

            foreach (char c in e.Text)
            {
                if (!char.IsControl(c))
                {
                    InsertCommand(c);
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

            if (textLength == -1)
                textLength = text.Length;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.C:
                        Copy();
                        break;
                    case Key.X:
                        Cut();
                        break;
                    case Key.V:
                        Paste();
                        break;
                }
            }

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
                        //pieceList.Delete(caretPosition - 1, caretPosition);
                        DeleteCommand();

                        caretPosition--;
                    }
                    break;
                case Key.Enter:
                    // pieceList.InsertUpdated(caretPosition, '\n');
                    InsertCommand('\n');
                    caretPosition++;
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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            try
            {
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
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
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


        //todo: fix
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

        internal void HandleArrowKeyPress(KeyEventArgs keyArgs)
        {
            OnKeyDown(keyArgs);
        }


        #region Selection

        private void ClearSelection()
        {
            selectionStart = -1;
            selectionEnd = -1;
        }

        private void DrawSelection(DrawingContext drawingContext)
        {
            int currentPos = 0;
            Piece currentPiece = pieceList.GetHead();
            double currentX = 0;
            double currentY = 0;

            while (currentPiece != null)
            {
                string pieceText = currentPiece.GetText();
                int pieceLength = pieceText.Length;

                if (currentPos + pieceLength > selectionStart && currentPos < selectionEnd)
                {
                    string[] lines = pieceText.Split('\n');
                    foreach (string line in lines)
                    {
                        int lineLength = line.Length + 1; // +1 for the newline character

                        if (currentPos + lineLength > selectionStart && currentPos < selectionEnd)
                        {
                            int selectionInLineStart = Math.Max(selectionStart - currentPos, 0);
                            int selectionInLineEnd = Math.Min(selectionEnd - currentPos, lineLength);

                            if (selectionInLineEnd > line.Length)
                            {
                                selectionInLineEnd = line.Length;
                            }

                            string selectedTextInLine = line.Substring(selectionInLineStart, selectionInLineEnd - selectionInLineStart);

                            double widthBeforeSelection = MeasureTextWidth(line.Substring(0, selectionInLineStart), currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));
                            double selectedTextWidth = MeasureTextWidth(selectedTextInLine, currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));

                            Rect selectionRect = new Rect(new Point(currentX + widthBeforeSelection, currentY), new Size(selectedTextWidth, GetLineHeight(currentPiece)));
                            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, 0, 0, 255)), null, selectionRect);
                        }

                        currentPos += lineLength;
                        currentX = 0;
                        currentY += GetLineHeight(currentPiece);
                    }
                    currentX = MeasureTextWidth(pieceText, currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));
                }
                else
                {
                    currentPos += pieceLength;
                    currentX += MeasureTextWidth(pieceText, currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));

                    if (pieceText.Contains("\n"))
                    {
                        currentY += GetLineHeight(currentPiece) * pieceText.Count(x => x == '\n');
                        currentX = 0;
                    }
                }

                currentPiece = currentPiece.Next;
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
            var defaultPoint = new Point(0, 0);
            if (point.X <= defaultPoint.X && point.Y <= GetLineHeight(pieceList.GetHead()))
            {
                return 0;
            }

            Piece currentPiece = pieceList.GetHead();
            double y = 0.0;
            double x = 0.0;
            int accumulatedLength = 0;

            while (currentPiece != null)
            {
                string currentText = currentPiece.GetText();
                string[] lines = currentText.Split('\n');

                foreach (var line in lines)
                {
                    if (point.Y <= y + GetLineHeight(currentPiece))
                    {
                        for (int j = 0; j <= line.Length; j++)
                        {
                            double charWidth;
                            if (j < line.Length)
                            {
                                charWidth = MeasureTextWidth(line[j].ToString(), currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));
                            }
                            else
                            {
                                charWidth = MeasureTextWidth("M", currentPiece.FontSize, new Typeface(currentPiece.Font, currentPiece.Style, FontWeights.Normal, FontStretches.Normal));
                            }

                            if (point.X <= x + charWidth / 2)
                            {
                                return accumulatedLength + j + 1;
                            }
                            x += charWidth;
                        }

                    }

                    if (lines.Length > 1)
                    { y += GetLineHeight(currentPiece); accumulatedLength += line.Length + 1; x = 0; }
                    else { accumulatedLength += line.Length; } // +1 for the newline character

                }

                currentPiece = currentPiece.Next;
            }

            return 0;
        }

        public void SetFontAttributesForSelection(FontFamily fontFamily, int fontSize, FontStyle fontStyle)
        {
            if (selectionStart == -1 || selectionEnd == -1 || selectionStart == selectionEnd)
                return;

            Piece startPiece = pieceList.Split(selectionStart);
            Piece endPiece = pieceList.Split(selectionEnd);

            Piece currentPiece = startPiece;

            while (currentPiece != null && currentPiece != endPiece)
            {
                currentPiece.Font = fontFamily;
                currentPiece.FontSize = fontSize;
                currentPiece.Style = fontStyle;
                currentPiece = currentPiece.Next;
            }

            InvalidateVisual();
        }

        #endregion

        #region CopyPaste

        private void Copy()
        {
            if (selectionStart != -1 && selectionEnd != -1 && selectionStart != selectionEnd)
            {
                string selectedText = GetSelectedText();

                Piece selectedPiece = GetSelectedPiece();

                AppClipboard.Copy(new ClipboardData(selectedText, selectedPiece.Font, selectedPiece.Style, selectedPiece.FontSize));
            }
        }

        private Piece GetSelectedPiece()
        {
            if (selectionStart == -1 || selectionEnd == -1 || selectionStart == selectionEnd)
                return null;

            Piece currentPiece = pieceList.GetHead();
            int accumulatedLength = 0;

            while (currentPiece != null)
            {
                int pieceEnd = accumulatedLength + currentPiece.Length;
                if (pieceEnd > selectionStart)
                {
                    return currentPiece;
                }

                accumulatedLength = pieceEnd;
                currentPiece = currentPiece.Next;
            }

            return null;
        }

        public string GetSelectedText()
        {
            if (selectionStart == -1 || selectionEnd == -1 || selectionStart == selectionEnd)
                return string.Empty;

            StringBuilder selectedTextBuilder = new StringBuilder();
            Piece currentPiece = pieceList.GetHead();
            int accumulatedLength = 0;

            while (currentPiece != null)
            {
                int pieceEnd = accumulatedLength + currentPiece.Length;
                if (pieceEnd > selectionStart)
                {
                    int startInPiece = Math.Max(accumulatedLength, selectionStart) - accumulatedLength;
                    int endInPiece = Math.Min(pieceEnd, selectionEnd) - accumulatedLength;
                    string pieceText = currentPiece.GetText();
                    selectedTextBuilder.Append(pieceText.Substring(startInPiece, endInPiece - startInPiece));
                }

                if (pieceEnd >= selectionEnd)
                    break;

                accumulatedLength = pieceEnd;
                currentPiece = currentPiece.Next;
            }

            return selectedTextBuilder.ToString();
        }

        private void Cut()
        {
            if (selectionStart != -1 && selectionEnd != -1)
            {
                Copy();
                // pieceList.Delete(selectionStart, selectionEnd);

                CutCommand cutCommand = new CutCommand(pieceList, selectionStart, selectionEnd, GetSelectedPiece(), GetSelectedText());
                cutCommand.Execute();
                undoStack.Push(cutCommand);
                redoStack.Clear();

                caretPosition = selectionStart;
                ClearSelection();

                InvalidateVisual();
            }
        }

        private void Paste()
        {
            if (AppClipboard.HasContent)
            {
                ClipboardData data = AppClipboard.ClipboardContent;

                // pieceList.Insert(caretPosition, data.Text, data.Font, data.Style, data.FontSize);

                PasteCommand pasteCommand = new PasteCommand(pieceList, caretPosition, data.Text);
                pasteCommand.Execute();
                undoStack.Push(pasteCommand);
                redoStack.Clear();

                caretPosition += data.Text.Length;
                ClearSelection();

                InvalidateVisual();
            }
        }

        #endregion

        #region Find

        private int FindNextOccurrence(string searchString, int startPosition)
        {
            if (string.IsNullOrEmpty(searchString))
                return -1;

            string text = pieceList.GetAllText();

            int index = text.IndexOf(searchString, startPosition, StringComparison.CurrentCultureIgnoreCase);

            return index;
        }

        public void FindAndSelect(string searchString, int stIndex = -1)
        {
            int startIndex;

            if (stIndex == -1)
                startIndex = caretPosition;
            else
                startIndex = stIndex;

            int foundIndex = FindNextOccurrence(searchString, startIndex);

            if (foundIndex != -1)
            {
                caretPosition = foundIndex + searchString.Length;
                selectionStart = foundIndex;
                selectionEnd = foundIndex + searchString.Length;

                InvalidateVisual();
            }
        }

        #endregion

        #region FontStyleChanges
        public void SetFontStyleForSelection(FontStyle fontStyle)
        {
            if (selectionStart == -1 || selectionEnd == -1 || selectionStart == selectionEnd)
                return;

            ChangeStyleCommand changeStyleCommand = new ChangeStyleCommand(GetSelectedPiece(), fontStyle);
            changeStyleCommand.Execute();
            undoStack.Push(changeStyleCommand);
            redoStack.Clear();


            InvalidateVisual();
        }

        internal void SetFont(FontFamily fontFamily)
        {
            if (selectionStart == -1 || selectionEnd == -1 || selectionStart == selectionEnd)
                return;

            ChangeStyleCommand changeStyleCommand = new ChangeStyleCommand(GetSelectedPiece(), fontFamily);
            changeStyleCommand.Execute();
            undoStack.Push(changeStyleCommand);
            redoStack.Clear();

            InvalidateVisual();
        }

        internal void SetFontSize(int size)
        {
            if (selectionStart == -1 || selectionEnd == -1 || selectionStart == selectionEnd)
                return;

            ChangeStyleCommand changeStyleCommand = new ChangeStyleCommand(GetSelectedPiece(), size);
            changeStyleCommand.Execute();
            undoStack.Push(changeStyleCommand);
            redoStack.Clear();

            InvalidateVisual();
        }
        #endregion

        #region Commands

        public void PerformAction(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                ICommand command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                ICommand command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }
        }

        private void DeleteCommand()
        {
            var deleteCommand = new DeleteCommand(pieceList, caretPosition - 1, caretPosition, GetPieceFromCaretPosition(caretPosition));
            deleteCommand.Execute();
            undoStack.Push(deleteCommand);
            redoStack.Clear();
        }

        private void InsertCommand(char ch)
        {
            var insertCommand = new InsertCommand(pieceList, caretPosition, ch);
            insertCommand.Execute();
            undoStack.Push(insertCommand);
            redoStack.Clear();
        }

        #endregion
    }

}