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
    internal class ChangeStyleCommand : ICommand
    {
        private FontStyle? fontStyleChangeTo = null;
        private int? sizeChangeTo = null;
        private FontFamily? fontFamilyChangeTo = null;

        private FontStyle style;
        private int size;
        private FontFamily fontFamily;

        private Piece piece;

        public ChangeStyleCommand(Piece piece, FontStyle fontStyle)
        {
            this.piece = piece;

            fontStyleChangeTo = fontStyle;

            style = piece.Style;
        }

        public ChangeStyleCommand(Piece piece, FontFamily fontFamily)
        {
            this.piece = piece;
            fontFamilyChangeTo = fontFamily;
            this.fontFamily = piece.Font;
        }

        public ChangeStyleCommand(Piece piece, int size)
        {
            this.piece = piece;
            sizeChangeTo = size;
            this.size = piece.FontSize;
        }

        public void Execute()
        {
            if (fontStyleChangeTo != null)
                piece.Style = fontStyleChangeTo.Value;
            else if (fontFamilyChangeTo != null)
                piece.Font = fontFamilyChangeTo;
            else if (sizeChangeTo != null)
                piece.FontSize = sizeChangeTo.Value;
        }

        public void Undo()
        {
            if (fontStyleChangeTo != null)
                piece.Style = style;
            else if (fontFamilyChangeTo != null)
                piece.Font = fontFamily;
            else if (sizeChangeTo != null)
                piece.FontSize = size;
        }
    }
}
