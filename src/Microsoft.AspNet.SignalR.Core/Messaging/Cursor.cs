// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        public static void WriteCursors(TextWriter textWriter, IList<Cursor> cursors)
        {
            for (int i = 0; i < cursors.Count; i++)
            {
                if (i > 0)
                {
                    textWriter.Write('|');
                }

                textWriter.Write(cursors[i]._escapedKey);
                textWriter.Write(',');
                textWriter.Write(cursors[i].Id.ToString("X", CultureInfo.InvariantCulture));
            }
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

        public static List<Cursor> GetCursors(string cursor)
        {
            return GetCursors(cursor, s => s);
        }

        public static List<Cursor> GetCursors(string cursor, Func<string, string> keyMaximizer)
        {
            return GetCursors(cursor, (key, state) => ((Func<string, string>)state).Invoke(key), keyMaximizer);
        }

        public static List<Cursor> GetCursors(string cursor, Func<string, object, string> keyMaximizer, object state)
        {
            // Technically GetCursors should never be called with a null value, so this is extra cautious
            if (String.IsNullOrEmpty(cursor))
            {
                throw new FormatException(Resources.Error_InvalidCursorFormat);
            }

            var signals = new HashSet<string>();
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
                // escape can only be true if we are consuming the key
                if (escape)
                {
                    if (ch != '\\' && ch != ',' && ch != '|')
                    {
                        throw new FormatException(Resources.Error_InvalidCursorFormat);
                    }

                    sb.Append(ch);
                    sbEscaped.Append(ch);
                    escape = false;
                }
                else
                {
                    if (ch == '\\')
                    {
                        if (!consumingKey)
                        {
                            throw new FormatException(Resources.Error_InvalidCursorFormat);
                        }

                        sbEscaped.Append('\\');
                        escape = true;
                    }
                    else if (ch == ',')
                    {
                        if (!consumingKey)
                        {
                            throw new FormatException(Resources.Error_InvalidCursorFormat);
                        }

                        // For now String.Empty is an acceptable key, but this should change once we verify
                        // that empty keys cannot be created legitimately.
                        currentKey = keyMaximizer(sb.ToString(), state);

                        // If the keyMap cannot find a key, we cannot create an array of cursors.
                        // This most likely means there was an AppDomain restart or a misbehaving client.
                        if (currentKey == null)
                        {
                            return null;
                        }
                        // Don't allow duplicate keys
                        if (!signals.Add(currentKey))
                        {
                            throw new FormatException(Resources.Error_InvalidCursorFormat);
                        }

                        currentEscapedKey = sbEscaped.ToString();

                        sb.Clear();
                        sbEscaped.Clear();
                        consumingKey = false;
                    }
                    else if (ch == '|')
                    {
                        if (consumingKey)
                        {
                            throw new FormatException(Resources.Error_InvalidCursorFormat);
                        }

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

            if (consumingKey)
            {
                throw new FormatException(Resources.Error_InvalidCursorFormat);
            }

            currentId = UInt64.Parse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            cursors.Add(new Cursor(currentKey, currentId, currentEscapedKey));

            return cursors;
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
