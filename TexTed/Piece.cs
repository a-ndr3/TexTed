using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows;

namespace TexTed
{
    internal class Piece
    {
        public int Length { get; internal set; }
        public string File { get; internal set; }
        public long FilePos { get; internal set; }
        public Piece Next { get; set; }
        public string Font { get; set; }
        public string Style { get; set; }

        public Piece(int len, string file, long filePos, string font, string style)
        {
            Length = len;
            File = file;
            FilePos = filePos;
            Font = font;
            Style = style;
            Next = null;
        }


        public string GetText()
        {
            byte[] buffer = new byte[Length];

            using (var fs = new FileStream(File, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(FilePos, SeekOrigin.Begin);
                fs.Read(buffer, 0, Length);
                return Encoding.UTF8.GetString(buffer);
            }
        }
    }
}
