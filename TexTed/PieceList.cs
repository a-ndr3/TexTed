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
using System.Reflection;
using System.Windows.Media.Media3D;

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

        public class Metadata
        {
            public List<PieceInfo> Pieces { get; set; }

            public Metadata()
            {
                Pieces = new List<PieceInfo>();
            }
        }

        public class PieceInfo
        {
            public long Start { get; set; }
            public int Length { get; set; }
            public string Font { get; set; }
            public int FontSize { get; set; }
            public string Style { get; set; }
        }

        readonly static FieldInfo charPosField = typeof(StreamReader).GetField("_charPos", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        readonly static FieldInfo charLenField = typeof(StreamReader).GetField("_charLen", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        readonly static FieldInfo charBufferField = typeof(StreamReader).GetField("_charBuffer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        static long ActualPosition(StreamReader reader)
        {
            var charBuffer = (char[])charBufferField.GetValue(reader);
            var charLen = (int)charLenField.GetValue(reader);
            var charPos = (int)charPosField.GetValue(reader);

            return reader.BaseStream.Position - reader.CurrentEncoding.GetByteCount(charBuffer, charPos, charLen - charPos);
        }

        public string GetFilePath()
        {
            return filePath;
        }

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
                string? line = reader.ReadLine();

                if (line == null) { LoadPlainText(reader); return; }

                if (line.StartsWith("{"))
                {
                    string metadata = line;

                    while (!(line = reader.ReadLine()).Contains("<<END_METADATA>>"))
                    {
                        metadata += line;
                    }

                    var textPos = ActualPosition(reader);

                    ApplyMetadata(metadata, textPos);
                }
                else
                {
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    LoadPlainText(reader);
                }
            }
        }

        private void ApplyMetadata(string metadata, long textPos)
        {
            var metadataObject = JsonConvert.DeserializeObject<Metadata>(metadata);

            var diff = 0;
            foreach (var pieceInfo in metadataObject.Pieces)
            {
                Piece piece = new Piece(pieceInfo.Length, filePath, textPos + diff, new FontFamily(pieceInfo.Font), GetStyle(pieceInfo.Style), pieceInfo.FontSize);
                AddPieceToEnd(piece);
                diff += pieceInfo.Length;
            }
        }

        public List<Piece> GetAllPieces()
        {
            var list = new List<Piece>();

            Piece current = head;

            while (current != null)
            {
                list.Add(current);
                current = current.Next;
            }

            return list;
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

        private void LoadPlainText(StreamReader reader)
        {
            var text = reader.ReadToEnd();

            byte[] buffer = Encoding.UTF8.GetBytes(text);

            Piece piece = new Piece(buffer.Length, filePath, 0, defaultFont, defaultStyle, defaultFontSize);

            AddPieceToEnd(piece);
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

        public void SaveFile()
        {
            if (GetAllPieces().Count != 0)
            {
                var listOfText = GetAllPieces().Select(p => p.GetText()).ToList();

                var metadata = GenerateMetadata();

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(metadata);
                    writer.WriteLine("<<END_METADATA>>");

                    foreach (var text in listOfText)
                    {
                        writer.Write(text);
                    }
                }
            }
        }

        public void SaveFileTest()
        {
            var fp = "C:\\Users\\abloh\\Desktop\\sw_mos\\texted_sharp\\TexTed\\testfile_and_metadata_gen.txt";
            var text1 = "Ahoy! ";
            var text2 = "Matey! ";
            var text3 = "Yarr! ";

            Piece piece1 = new Piece(text1, fp, 0, defaultFont, defaultStyle, defaultFontSize);
            Piece piece2 = new Piece(text2 + text3, fp, 6, new FontFamily("Times New Roman"), FontStyles.Italic, 24);

            AddPieceToEnd(piece1);
            AddPieceToEnd(piece2);

            var metadata = GenerateMetadata();

            using (var writer = new StreamWriter(fp))
            {
                writer.WriteLine(metadata);
                writer.WriteLine("<<END_METADATA>>");

                writer.Write(text1);
                writer.Write(text2 + text3);
            }
        }

        private string GenerateMetadata()
        {
            Metadata metadata = new Metadata();

            Piece current = head;
            
            int i = 0;
            while (current != null)
            {
                var pieceMetadata = new PieceInfo
                {
                    Start = current.FilePos,
                    Length = current.Length,
                    Font = current.Font.Source,
                    FontSize = current.FontSize,
                    Style = current.Style.ToString()
                };

                if (i != 0 && pieceMetadata.Start < metadata.Pieces.Last().Start)
                {
                    pieceMetadata.Start = metadata.Pieces.Last().Start + metadata.Pieces.Last().Length;
                }
                else if (i != 0 && pieceMetadata.Start == metadata.Pieces.Last().Start)
                {
                    pieceMetadata.Start = metadata.Pieces.Last().Start + metadata.Pieces.Last().Length;
                }

                metadata.Pieces.Add(pieceMetadata);
                current = current.Next;
                i++;
            }

            return JsonConvert.SerializeObject(metadata, Formatting.Indented);
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

        public void InsertUpdated(int pos, char ch)
        {
            Piece p = Split(pos);

            //if p is not the last piece on scratch
            if (p != null && (p.File != scratchFilePath || p.FilePos + p.Length != GetFileLength(scratchFilePath)))
            {
                var q = new Piece(0, scratchFilePath, GetFileLength(scratchFilePath), p.Font, p.Style, p.FontSize);
                q.Next = p.Next;
                if (q.Next != null)
                {
                    q.Next.Prev = q;
                }
                p.Next = q;
                q.Prev = p;
                p = q;
            }

            // p is last
            using (var fs = new FileStream(scratchFilePath, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(new char[] { ch });
                fs.Write(buffer, 0, buffer.Length);
            }

            if (p != null)
            {
                p.Length++;
            }
            else if (head == null)
            {
                Piece newPiece = new Piece(1, scratchFilePath, 0, defaultFont, defaultStyle);

                head = newPiece;
            }
            else
            {
                throw new InvalidOperationException("Insertion fail");
            }
        }

        internal void Insert(int caretPosition, string text, FontFamily font, FontStyle style, int fontSize)
        {
            throw new NotImplementedException();
        }

        private long GetFileLength(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

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
