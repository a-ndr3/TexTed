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
    internal class PieceList
    {
        private Piece head;
        private string filePath;
        private string scratchFilePath; //to handle insertion

        public PieceList(string filePath)
        {
            this.filePath = filePath;

            if (File.Exists(filePath + ".scratch"))
            {
                File.Delete(filePath + ".scratch");
            }

            this.scratchFilePath = filePath + ".scratch";

            head = null;
            LoadFromFile();
        }

        private void LoadFromFile()
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
                return;
            }

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                StreamReader reader = new StreamReader(fs);
                long currentPos = 0;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    byte[] buffer = Encoding.UTF8.GetBytes(line + Environment.NewLine);
                    Piece piece = new Piece(buffer.Length, filePath, currentPos, "DefaultFont", "DefaultStyle");
                    currentPos += buffer.Length;

                    AddPieceToEnd(piece);
                }
            }
        }

        private void AddPieceToEnd(Piece piece)
        {
            if (head == null)
            {
                head = piece;
            }
            else
            {
                Piece current = head;
                while (current.Next != null)
                {
                    current = current.Next;
                }
                current.Next = piece;
            }
        }
        private void WritePieceToFile(string text, Piece piece)
        {
            using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                fs.Write(buffer, 0, buffer.Length);
                piece.FilePos = fs.Length - buffer.Length;
                piece.Length = buffer.Length;
            }
        }
       
        //todo: check if needed, change 0s pos
        public void AddPiece(string text, string font, string style)
        {
            Piece newPiece = new Piece(0, filePath, 0, font, style);
           
            WritePieceToFile(text, newPiece);

            if (head == null)
            {
                head = newPiece;
            }
            else
            {
                Piece current = head;
                while (current.Next != null)
                {
                    current = current.Next;
                }
                current.Next = newPiece;
            }
        }
       
        public string GetAllText()
        {
            StringBuilder sb = new StringBuilder();
            Piece current = head;
            while (current != null)
            {
                sb.Append(current.GetText());
                current = current.Next;
            }
            return sb.ToString();
        }

        //Split from slides
        public Piece Split(int pos)
        {
            Piece p = head;
            int accumulatedLength = p.Length;

            while (pos > accumulatedLength && p.Next != null)
            {
                p = p.Next;
                accumulatedLength += p.Length;
            }

            if (pos != accumulatedLength)
            {
                int len2 = accumulatedLength - pos;
                int len1 = p.Length - len2;
                p.Length = len1;

                Piece q = new Piece(len2, p.File, p.FilePos + len1, p.Font, p.Style)
                {
                    Next = p.Next
                };
                p.Next = q;
            }

            return p;
        }

        //todo: should we create a new piece for EACH char? What to do with inserted string instead of char? Convert bunch of chars to string aka to one piece
        public void Insert(int pos, char ch)
        {
            Piece p = Split(pos);

            // write char to the scratch file
            using (var fs = new FileStream(scratchFilePath, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(new char[] { ch });
                fs.Write(buffer, 0, buffer.Length);
            }

            // create new piece for the char
            Piece newPiece = new Piece(1, scratchFilePath, GetFileLength(scratchFilePath) - 1, "DefaultFont", "DefaultStyle");

            // adjust linked list
            if (p != null)
            {
                newPiece.Next = p.Next;
                p.Next = newPiece;
            }
            else // if insert beginning
            {
                newPiece.Next = head;
                head = newPiece;
            }
        }

        private long GetFileLength(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return fs.Length;
            }
        }

        //Delete from slides
        public void Delete(int from, int to)
        {
            Piece a = Split(from);
            Piece b = Split(to);
            a.Next = b.Next;
        }
    }
}
