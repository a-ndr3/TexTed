using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace TexTed
{
    internal class ClipboardData
    {
        public string Text { get; set; }
        public FontFamily Font { get; set; }
        public FontStyle Style { get; set; }
        public int FontSize { get; set; }

        public ClipboardData(string text, FontFamily font, FontStyle style, int fontSize)
        {
            Text = text;
            Font = font;
            Style = style;
            FontSize = fontSize;
        }
    }
}
