// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal unsafe class Cursor
    {
        private static char[] _escapeChars = new[] { '\\', '|', ',' };
        private string _escapedKey;

        public string Key { get; private set; }

        public ulong Id { get; set; }

        public static Cursor Clone(Cursor cursor)
        {
            return new Cursor(cursor.Key, cursor.Id, cursor._escapedKey);
        }

        public Cursor(string key, ulong id)
            : this(key, id, Escape(key))
        {
        }
        
        public Cursor(string key, ulong id, string minifiedKey)
        {
            Key = key;
            Id = id;
            _escapedKey = minifiedKey;
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
                serialized[i] = cursors[i]._escapedKey + ',' + cursors[i].Id.ToString("X", CultureInfo.InvariantCulture);
            }

            return String.Join("|", serialized);
        }

        private static string MakeCursorFast(IList<Cursor> cursors)
        {
            checked
            {
                const int MAX_CHARS = 8 * 1024;
                char* pChars = stackalloc char[MAX_CHARS];
                char* pNextChar = pChars;
                int numCharsInBuffer = 0;

                // Start shoving data into the buffer
                for (int i = 0; i < cursors.Count; i++)
                {
                    Cursor cursor = cursors[i];
                    string escapedKey = cursor._escapedKey;

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
                pBuffer[length] = Int32ToHex((int)(value >> 60));
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

        public static Cursor[] GetCursors(string cursor, Func<string, string> keyMaximizer)
        {
            var cursors = new List<Cursor>();
            string currentKey = null;
            string currentEscapedKey = null;
            ulong currentId;
            bool escape = false;
            bool consumingKey = true;
            var sb = new StringBuilder();
            var sbEscaped = new StringBuilder();

            foreach (var ch in cursor)
            {
                if (escape)
                {
                    sb.Append(ch);
                    if (consumingKey)
                    {
                        sbEscaped.Append(ch);
                    }
                    escape = false;
                }
                else
                {
                    if (ch == '\\')
                    {
                        if (consumingKey)
                        {
                            sbEscaped.Append('\\');
                        }
                        escape = true;
                    }
                    else if (ch == ',')
                    {
                        currentEscapedKey = sbEscaped.ToString();
                        currentKey = keyMaximizer(sb.ToString());
                        // If the keyMap cannot find a key, we cannot create an array of cursors.
                        if (currentKey == null)
                        {
                            return null;
                        }
                        sb.Clear();
                        sbEscaped.Clear();
                        consumingKey = false;
                    }
                    else if (ch == '|')
                    {
                        currentId = UInt64.Parse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        cursors.Add(new Cursor(currentKey, currentId, currentEscapedKey));
                        sb.Clear();
                        consumingKey = true;
                    }
                    else
                    {
                        sb.Append(ch);
                        if (consumingKey)
                        {
                            sbEscaped.Append(ch);
                        }
                    }
                }
            }

            if (sb.Length > 0)
            {
                currentId = UInt64.Parse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                cursors.Add(new Cursor(currentKey, currentId, currentEscapedKey));
            }

            return cursors.ToArray();
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
