using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace TexTed
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

        public DeleteCommand(PieceList pieceList, int startPosition, int endPosition)
        {
            this.pieceList = pieceList;
            this.startPosition = startPosition;
            this.endPosition = endPosition;

            this.deletedText = pieceList.GetAllText().Substring(startPosition, endPosition);
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
