using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using TexTed.PieceBase;

namespace TexTed.Commands
{
    internal class DeleteCommand : ICommand
    {
        private PieceList pieceList;
        private int startPosition;
        private int endPosition;
        private string deletedText;
        private FontFamily font;
        private FontStyle style;
        private int fontSize;

        public DeleteCommand(PieceList pieceList, int startPosition, int endPosition, Piece piece)
        {
            this.pieceList = pieceList;
            this.startPosition = startPosition;
            this.endPosition = endPosition;

            deletedText = pieceList.GetAllText().Substring(startPosition, 1);

            fontSize = piece.FontSize;
            font = piece.Font;
            style = piece.Style;
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
