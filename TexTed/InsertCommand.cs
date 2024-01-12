using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace TexTed
{
    internal class InsertCommand : ICommand
    {
        private PieceList pieceList;
        private int position;
        private char ch;

        public InsertCommand(PieceList pieceList, int position, char ch)
        {
            this.pieceList = pieceList;
            this.position = position;
            this.ch = ch;
        }

        public void Execute()
        {
            pieceList.InsertUpdated(position, ch);
        }

        public void Undo()
        {
            pieceList.Delete(position, position + 1);
        }
    }
}
