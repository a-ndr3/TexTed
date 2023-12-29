using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexTed
{
    internal static class AppClipboard
    {
        public static ClipboardData ClipboardContent { get; private set; }

        public static void Copy(ClipboardData data)
        {
            ClipboardContent = data;
        }

        public static void Clear()
        {
            ClipboardContent = null;
        }

        public static bool HasContent => ClipboardContent != null;
    }
}
