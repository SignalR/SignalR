// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Microsoft.AspNet.SignalR
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
            }
        }

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
            return MakeCursor(cursors, s => s);
        }

        public static string MakeCursor(IList<Cursor> cursors, Func<string, string> keyMap)
        {
            return MakeCursorFast(cursors, keyMap) ?? MakeCursorSlow(cursors, keyMap);
        }

        private static string MakeCursorSlow(IList<Cursor> cursors, Func<string, string> keyMap)
        {
            var serialized = new string[cursors.Count];
            for (int i = 0; i < cursors.Count; i++)
            {
                serialized[i] = Escape(keyMap(cursors[i].Key)) + ',' + cursors[i].Id.ToString("X");
            }

            return String.Join("|", serialized);
        }

        private static string MakeCursorFast(IList<Cursor> cursors, Func<string, string> keyMap)
        {
            const int MAX_CHARS = 8 * 1024;
            char* pChars = stackalloc char[MAX_CHARS];
            char* pNextChar = pChars;
            int numCharsInBuffer = 0;

            // Start shoving data into the buffer
            for (int i = 0; i < cursors.Count; i++)
            {
                Cursor cursor = cursors[i];
                string escapedKey = Escape(keyMap(cursor.Key));

                // comma + up to 16-char hex Id + pipe
                numCharsInBuffer += escapedKey.Length + 18;

                if (numCharsInBuffer > MAX_CHARS)
                {
                    return null; // we will overrun the buffer
                }

                for (int j = 0; j < escapedKey.Length; j++)
                {
                    *pNextChar++ = escapedKey[j];
                }

                *pNextChar++ = ',';
                int hexLength = WriteUlongAsHexToBuffer(cursor.Id, pNextChar);

                // Since we reserved 16 chars for the hex value, update numCharsInBuffer to reflect the actual number of
                // characters written by WriteUlongAsHexToBuffer.
                numCharsInBuffer += hexLength - 16;
                pNextChar += hexLength;
                *pNextChar++ = '|';
            }

            return (numCharsInBuffer == 0) ? String.Empty : new String(pChars, 0, numCharsInBuffer - 1); // -1 for final pipe
        }

        private static int WriteUlongAsHexToBuffer(ulong value, char* pBuffer)
        {
            // This tracks the length of the output and serves as the index for the next character to be written into the pBuffer.
            // The length could reach up to 16 characters, so at least that much space should remain in the pBuffer.
            int length = 0;

            // Write the hex value from left to right into the buffer without zero padding.
            for (int i = 0; i < 16; i++)
            {
                // Convert the first 4 bits of the value to a valid hex character.
                pBuffer[length] = Int32ToHex((int)((value & 0xf000000000000000) >> 60)); // take first 4 bits and shift 60 bits
                value <<= 4;

                // Don't increment length if it would just add zero padding
                if (length != 0 || pBuffer[length] != '0')
                {
                    length++;
                }
            }

            // The final length will be 0 iff the original value was 0. In this case we want to add 1 character, '0', to pBuffer
            // '0' will have already been written to pBuffer[0] 16 times, so it is safe to simply return that 1 character was
            // written to the output.
            if (length == 0)
            {
                return 1;
            }

            return length;
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
            return GetCursors(cursor, s => s);
        }

        public static Cursor[] GetCursors(string cursor, Func<string, string> keyMap)
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
                        current.Key = keyMap(sb.ToString());
                        // If the keyMap cannot find a key, we cannot create an array of cursors.
                        if (current.Key == null)
                        {
                            return null;
                        }
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
