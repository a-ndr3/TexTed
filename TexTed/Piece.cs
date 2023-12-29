using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows;
using System.Windows.Media;

namespace TexTed
{
    internal class Piece
    {
        public int Length { get; internal set; }
        public string File { get; internal set; }
        public long FilePos { get; internal set; }
        public Piece Next { get; set; }
        public Piece Prev { get; set; }
        public FontFamily Font { get; set; }
        public FontStyle Style { get; set; }
        public int FontSize { get; set; }

        public Piece(int len, string file, long filePos, FontFamily font, FontStyle style, int fontSize = 12)
        {
            Length = len;
            File = file;
            FilePos = filePos;
            Font = font;
            Style = style;
            Next = null;
            Prev = null;
            FontSize = fontSize;
        }

        public Piece(string text, string file, long filePos, FontFamily font, FontStyle style, int fontSize = 12)
        {
            Length = Encoding.UTF8.GetByteCount(text);
            File = file;
            FilePos = filePos;
            Font = font;
            Style = style;
            Next = null;
            Prev = null;
            FontSize = fontSize;
        }

        public string GetText()
        {
            byte[] buffer;
            try
            {
                buffer = new byte[Length];
            }
            catch (Exception ex)
            {
                
                throw;
            }

            using (var fs = new FileStream(File, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(FilePos, SeekOrigin.Begin);
                fs.Read(buffer, 0, Length);
                return Encoding.UTF8.GetString(buffer);
            }
        }
    }
}
