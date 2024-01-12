using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace TexTed
{
    internal class CutCommand : ICommand
    {
        private PieceList pieceList;
        private string deletedText;
        private int startPosition;
        private int endPosition;
        private FontFamily font;
        private FontStyle style;
        private int fontSize;

        public CutCommand(PieceList pieceList, int start, int end, Piece piece, String deletedText)
        {
            this.pieceList = pieceList;
            this.startPosition = start;
            this.endPosition = end;

            this.deletedText = deletedText;

            this.font = piece.Font;
            this.style = piece.Style;
            this.fontSize = piece.FontSize;
        }

        public void Execute()
        {
            pieceList.Delete(startPosition, endPosition);
        }

        public void Undo()
        {
            pieceList.Insert(startPosition, deletedText, font, style, fontSize);
        }
    }
}
