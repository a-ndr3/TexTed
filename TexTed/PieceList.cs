using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace TexTed
{
    internal class PieceList
    {
        private Piece head;
        private string filePath;
        private string scratchFilePath; //to handle insertion

        private FontFamily defaultFont = new FontFamily("Arial");
        private FontStyle defaultStyle = FontStyles.Normal;
        private int defaultFontSize = 12;

        public PieceList(string filePath)
        {
            this.filePath = filePath;

            if (File.Exists(filePath + ".scratch"))
            {
                File.Delete(filePath + ".scratch");
            }

            this.scratchFilePath = filePath + ".scratch";

            head = null;
            LoadFile();
        }

        private void LoadFile()
        {
            if (!File.Exists(filePath)) return;

            using (var reader = new StreamReader(filePath))
            {
                string line = reader.ReadLine();

                if (line.StartsWith("{"))
                {
                    string metadata = line;

                    while (!(line = reader.ReadLine()).Contains("<<END_METADATA>>"))
                    {
                        metadata += line;
                    }

                    ApplyMetadata(metadata);
                }
                else
                {
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    LoadPlainText(reader);
                }
            }
        }

        private void ApplyMetadata(string metadata)
        {
            var metadataObject = JsonConvert.DeserializeObject<Metadata>(metadata);
            long metadataLength = GetMetadataLength(metadata) + 12; //todo: fix counting
            long currentPos = 0;

            foreach (var pieceInfo in metadataObject.Pieces)
            {
                long filePosWithOffset = metadataLength + currentPos;
                Piece piece = new Piece(pieceInfo.Length, filePath, filePosWithOffset, new FontFamily(pieceInfo.Font), GetStyle(pieceInfo.Style), pieceInfo.FontSize);
                currentPos += piece.Length;
                AddPieceToEnd(piece);
            }
        }
        private long GetMetadataLength(string metadata)
        {
            return (metadata + "\n<<END_METADATA>>\n").Length;
        }

        private FontStyle GetStyle(string style)
        {
            switch (style)
            {
                case "Italic":
                    return FontStyles.Italic;
                case "Oblique":
                    return FontStyles.Oblique;
                default:
                    return FontStyles.Normal;
            }
        }

        public Piece GetHead()
        {
            return head;
        }

        public class Metadata
        {
            public List<PieceInfo> Pieces { get; set; }
        }

        public class PieceInfo
        {
            public int Length { get; set; }
            public string Font { get; set; }
            public int FontSize { get; set; }
            public string Style { get; set; }
        }

        private void LoadPlainText(StreamReader reader)
        {
            int currentPos = 0;

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();

                byte[] buffer = Encoding.UTF8.GetBytes(line + Environment.NewLine);

                Piece piece = new Piece((line + Environment.NewLine).Length, filePath, currentPos, defaultFont, defaultStyle, defaultFontSize);

                currentPos += buffer.Length;

                AddPieceToEnd(piece);
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
                piece.Prev = current;
            }
        }
        private void WritePieceToFile(string text, Piece piece)
        {
            using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                fs.Write(buffer, 0, buffer.Length);
                piece.FilePos = fs.Length - buffer.Length; // todo: check if correct
                piece.Length = buffer.Length;
            }
        }

        private void SaveFile()
        {
            var metadata = GenerateMetadata();

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine(metadata);
                writer.WriteLine();
                writer.WriteLine("<<END_METADATA>>\n");

                while (head != null)
                {
                    writer.Write(head.GetText());
                    head = head.Next;
                }
            }
        }

        private string GenerateMetadata()
        {
            Metadata metadata = new Metadata();

            Piece current = head;

            while (current != null)
            {
                var pieceMetadata = new PieceInfo
                {
                    Length = current.Length,
                    Font = current.Font.Source,
                    FontSize = current.FontSize,
                    Style = current.Style.ToString()
                };

                metadata.Pieces.Add(pieceMetadata);
                current = current.Next;
            }

            return JsonConvert.SerializeObject(metadata);
        }

        //todo: check if needed, change 0s pos
        public void AddPiece(string text, FontFamily font, FontStyle style, int size)
        {
            Piece newPiece = new Piece(0, filePath, 0, font, style, size);

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

                if (len2 == -1)
                {

                }

                Piece q = new Piece(len2, p.File, p.FilePos + len1, p.Font, p.Style, p.FontSize)
                {
                    Next = p.Next,
                    Prev = p
                };
                p.Next = q;
            }

            return p;
        }

        //todo: should we create a new piece for EACH char? What to do with inserted string instead of char? Convert bunch of chars to string aka to one piece
        public void Insert(int pos, char ch)
        {
            Piece p = Split(pos);

            using (var fs = new FileStream(scratchFilePath, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(new char[] { ch });
                fs.Write(buffer, 0, buffer.Length);
            }


            Piece newPiece = new Piece(1, scratchFilePath, GetFileLength(scratchFilePath) - 1, p.Font, p.Style, p.FontSize);

            if (p != null)
            {
                newPiece.Next = p.Next;
                p.Next = newPiece;

                if (newPiece.Next != null)
                    newPiece.Next.Prev = newPiece;
                
                newPiece.Prev = p;
            }
            else
            {
                newPiece.Next = head;
                head = newPiece;
            }
        }

        public void Insert2(int pos, char ch)
        {
            Piece p = Split(pos);

            //if last piece
            if (p != null && p.FilePos + p.Length != GetFileLength(scratchFilePath))
            {
                Piece q = new Piece(0, scratchFilePath, GetFileLength(scratchFilePath), p.Font, p.Style, p.FontSize);
                q.Next = p.Next;
                p.Next = q;
                p = q; //last
            }

            using (var fs = new FileStream(scratchFilePath, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(new char[] { ch });
                fs.Write(buffer, 0, buffer.Length);
            }

            if (p != null)
            {
                p.Length++;
            }
            else
            {
                //list is empty
                p = new Piece(1, scratchFilePath, GetFileLength(scratchFilePath) - 1, defaultFont, defaultStyle, defaultFontSize);
                if (head == null)
                {
                    head = p;
                }
                else
                {
                    Piece current = head;
                    while (current.Next != null)
                    {
                        current = current.Next;
                    }
                    current.Next = p;
                    p.Prev = current;
                }
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
