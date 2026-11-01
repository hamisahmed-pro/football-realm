// Football Realm — MiniJson: dependency-free JSON for Core.
// Why this exists: System.Text.Json is not officially available as
// a Unity package and requires manual dependency management in Unity,
// while Newtonsoft.Json would add a package dependency to Core.
// Core must compile in Unity (including IL2CPP) with zero packages,
// so JSON for the two shapes Core actually needs is implemented here:
//
//   - clubs.json: [ { country, league, tier, clubs: [name, ...] } ]
//   - SaveData: { version, playerClubId, leagueName, country,
//                 seasonYear, currentRound, managerName, funds,
//                 legacyPoints, achievements: [...],
//                 clubTotals: { clubId: [pts, w, d, l, gf, ga] } }
//
// This is a small, strict parser (objects, arrays, strings with
// escapes and \uXXXX, numbers, true/false/null) and a matching
// writer for SaveData. It is not a general JSON library, and it
// does not try to be — its scope is exactly the two shapes above,
// which keeps it auditable and IL2CPP-safe (no reflection).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FootballRealm.Core
{
    public static class MiniJson
    {
        public static Dictionary<string, object> ParseObject(string json)
        {
            var parser = new Parser(json);
            var value = parser.ParseValue();
            if (value is Dictionary<string, object> obj) return obj;
            throw new FormatException("JSON top level is not an object.");
        }

        public static List<object> ParseArray(string json)
        {
            var parser = new Parser(json);
            var value = parser.ParseValue();
            if (value is List<object> arr) return arr;
            throw new FormatException("JSON top level is not an array.");
        }

        public static string GetString(Dictionary<string, object> obj, string key, string fallback = "")
        {
            if (obj.TryGetValue(key, out var v) && v is string s) return s;
            return fallback;
        }

        public static int GetInt(Dictionary<string, object> obj, string key, int fallback = 0)
        {
            if (obj.TryGetValue(key, out var v))
            {
                if (v is long l) return (int)l;
                if (v is double d) return (int)d;
            }
            return fallback;
        }

        public static long GetLong(Dictionary<string, object> obj, string key, long fallback = 0)
        {
            if (obj.TryGetValue(key, out var v))
            {
                if (v is long l) return l;
                if (v is double d) return (long)d;
            }
            return fallback;
        }

        public static string Escape(string s)
        {
            if (s == null) return string.Empty;
            var sb = new StringBuilder(s.Length + 2);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s ?? string.Empty; _i = 0; }

            public object ParseValue()
            {
                SkipWhitespace();
                if (_i >= _s.Length) throw new FormatException("Unexpected end of JSON.");
                char c = _s[_i];
                if (c == '{') return ParseObjectInner();
                if (c == '[') return ParseArrayInner();
                if (c == '"') return ParseString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null!; }
                return ParseNumber();
            }

            private Dictionary<string, object> ParseObjectInner()
            {
                var obj = new Dictionary<string, object>();
                _i++; // {
                SkipWhitespace();
                if (Peek() == '}') { _i++; return obj; }
                while (true)
                {
                    SkipWhitespace();
                    string key = ParseString();
                    SkipWhitespace();
                    ExpectChar(':');
                    object value = ParseValue();
                    obj[key] = value;
                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',') { _i++; continue; }
                    if (c == '}') { _i++; return obj; }
                    throw new FormatException($"Expected ',' or '}}' at position {_i}.");
                }
            }

            private List<object> ParseArrayInner()
            {
                var arr = new List<object>();
                _i++; // [
                SkipWhitespace();
                if (Peek() == ']') { _i++; return arr; }
                while (true)
                {
                    arr.Add(ParseValue());
                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',') { _i++; continue; }
                    if (c == ']') { _i++; return arr; }
                    throw new FormatException($"Expected ',' or ']' at position {_i}.");
                }
            }

            private string ParseString()
            {
                ExpectChar('"');
                var sb = new StringBuilder();
                while (_i < _s.Length)
                {
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\' && _i < _s.Length)
                    {
                        char e = _s[_i++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'u':
                                if (_i + 4 > _s.Length) throw new FormatException("Bad \\u escape.");
                                string hex = _s.Substring(_i, 4);
                                _i += 4;
                                sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                                break;
                            default: throw new FormatException($"Bad escape \\{e}.");
                        }
                    }
                    else sb.Append(c);
                }
                throw new FormatException("Unterminated string.");
            }

            private object ParseNumber()
            {
                int start = _i;
                while (_i < _s.Length && "-+0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
                string token = _s.Substring(start, _i - start);
                if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                throw new FormatException($"Bad number '{token}'.");
            }

            private void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            private char Peek()
            {
                SkipWhitespace();
                return _i < _s.Length ? _s[_i] : '\0';
            }

            private void ExpectChar(char c)
            {
                SkipWhitespace();
                if (_i >= _s.Length || _s[_i] != c) throw new FormatException($"Expected '{c}' at position {_i}.");
                _i++;
            }

            private void Expect(string word)
            {
                if (_i + word.Length > _s.Length || _s.Substring(_i, word.Length) != word)
                    throw new FormatException($"Expected '{word}' at position {_i}.");
                _i += word.Length;
            }
        }
    }
}
