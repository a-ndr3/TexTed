﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexTed
{
    internal class PasteCommand : ICommand
    {
        private PieceList pieceList;
        private int position;
        private string text;

        public PasteCommand(PieceList pieceList, int pos, string text)
        {
            this.pieceList = pieceList;
            this.position = pos;
            this.text = text;
        }

        public void Execute()
        {
            ClipboardData data = AppClipboard.ClipboardContent;
            pieceList.Insert(position, data.Text, data.Font, data.Style, data.FontSize);
        }

        public void Undo()
        {
            pieceList.Delete(position, position + text.Length);
        }
    }
}
