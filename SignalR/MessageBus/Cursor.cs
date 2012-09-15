using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SignalR
{
    internal unsafe class Cursor
    {
        private static char[] _escapeChars = new[] { '\\', '|', ',' };

        private string _key;
        public string Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
                EscapedKey = Escape(value);
            }
        }

        private string EscapedKey { get; set; }

        public ulong Id { get; set; }

        public static Cursor Clone(Cursor cursor)
        {
            return new Cursor
            {
                Id = cursor.Id,
                Key = cursor.Key
            };
        }

        public static string MakeCursor(IList<Cursor> cursors)
        {
            return MakeCursorFast(cursors) ?? MakeCursorSlow(cursors);
        }

        private static string MakeCursorSlow(IList<Cursor> cursors)
        {
            var serialized = new string[cursors.Count];
            for (int i = 0; i < cursors.Count; i++)
            {
                serialized[i] = cursors[i].EscapedKey + ',' + cursors[i].Id;
            }

            return String.Join("|", serialized);
        }

        private static string MakeCursorFast(IList<Cursor> cursors)
        {
            const int MAX_CHARS = 8 * 1024;
            char* pChars = stackalloc char[MAX_CHARS];
            char* pNextChar = pChars;
            int numCharsInBuffer = 0;

            // Start shoving data into the buffer
            for (int i = 0; i < cursors.Count; i++)
            {
                Cursor cursor = cursors[i];
                string escapedKey = cursor.EscapedKey;

                checked
                {
                    numCharsInBuffer += escapedKey.Length + 18; // comma + 16-char hex Id + pipe
                }

                if (numCharsInBuffer > MAX_CHARS)
                {
                    return null; // we will overrun the buffer
                }

                for (int j = 0; j < escapedKey.Length; j++)
                {
                    *pNextChar++ = escapedKey[j];
                }

                *pNextChar = ',';
                pNextChar++;
                WriteUlongAsHexToBuffer(cursor.Id, pNextChar);
                pNextChar += 16;
                *pNextChar = '|';
                pNextChar++;
            }

            return (numCharsInBuffer == 0) ? String.Empty : new String(pChars, 0, numCharsInBuffer - 1); // -1 for final pipe
        }

        private static void WriteUlongAsHexToBuffer(ulong value, char* pBuffer)
        {
            for (int i = 15; i >= 0; i--)
            {
                pBuffer[i] = Int32ToHex((int)value & 0xf); // don't care about overflows here
                value >>= 4;
            }
        }

        private static char Int32ToHex(int value)
        {
            return (value < 10) ? (char)(value + '0') : (char)(value - 10 + 'A');
        }

        private static string Escape(string value)
        {
            // Nothing to do, so bail
            if (value.IndexOfAny(_escapeChars) == -1)
            {
                return value;
            }

            var sb = new StringBuilder();
            // \\ = \
            // \| = |
            // \, = ,
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        sb.Append('\\').Append(ch);
                        break;
                    case '|':
                        sb.Append('\\').Append(ch);
                        break;
                    case ',':
                        sb.Append('\\').Append(ch);
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        public static Cursor[] GetCursors(string cursor)
        {
            var cursors = new List<Cursor>();
            var current = new Cursor();
            bool escape = false;
            var sb = new StringBuilder();

            foreach (var ch in cursor)
            {
                if (escape)
                {
                    sb.Append(ch);
                    escape = false;
                }
                else
                {
                    if (ch == '\\')
                    {
                        escape = true;
                    }
                    else if (ch == ',')
                    {
                        current.Key = sb.ToString();
                        sb.Clear();
                    }
                    else if (ch == '|')
                    {
                        current.Id = UInt64.Parse(sb.ToString(), NumberStyles.HexNumber);
                        cursors.Add(current);
                        current = new Cursor();
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
            }

            if (sb.Length > 0)
            {
                current.Id = UInt64.Parse(sb.ToString(), NumberStyles.HexNumber);
                cursors.Add(current);
            }

            return cursors.ToArray();
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
