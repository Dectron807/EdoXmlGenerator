using System.Collections.Generic;
using System.Text;

namespace EdoXmlGenerator.Services;

public static class XmlTokenizer
{
    public record XmlToken(string Text, string Color);

    public static List<XmlToken> Tokenize(string xml)
    {
        var tokens = new List<XmlToken>();
        int i = 0;
        int len = xml.Length;

        while (i < len)
        {
            if (xml[i] == '<')
            {
                if (i + 3 < len && xml[i + 1] == '!' && xml[i + 2] == '-' && xml[i + 3] == '-')
                {
                    int end = xml.IndexOf("-->", i + 4);
                    if (end < 0) end = len;
                    tokens.Add(new XmlToken(xml.Substring(i, end - i + 3), "#7F8C8D"));
                    i = end + 3;
                    continue;
                }

                if (i + 1 < len && xml[i + 1] == '?')
                {
                    int end = xml.IndexOf("?>", i + 2);
                    if (end < 0) end = len - 1;
                    tokens.Add(new XmlToken(xml.Substring(i, end - i + 2), "#E67E22"));
                    i = end + 2;
                    continue;
                }

                int tagEnd = FindTagEnd(xml, i);
                if (tagEnd < 0) tagEnd = len;
                var tag = xml.Substring(i, tagEnd - i + 1);
                tokens.AddRange(TokenizeTag(tag));
                i = tagEnd + 1;
            }
            else
            {
                int next = xml.IndexOf('<', i);
                if (next < 0) next = len;
                tokens.Add(new XmlToken(xml.Substring(i, next - i), "#1F2430"));
                i = next;
            }
        }

        return tokens;
    }

    private static int FindTagEnd(string xml, int start)
    {
        bool inQuote = false;
        char quoteChar = '"';
        for (int i = start; i < xml.Length; i++)
        {
            char c = xml[i];
            if (inQuote)
            {
                if (c == quoteChar) inQuote = false;
            }
            else if (c == '"' || c == '\'')
            {
                inQuote = true;
                quoteChar = c;
            }
            else if (c == '>')
            {
                return i;
            }
        }
        return -1;
    }

    private static List<XmlToken> TokenizeTag(string tag)
    {
        var tokens = new List<XmlToken>();
        int i = 0;

        while (i < tag.Length)
        {
            char c = tag[i];

            var isSpace = c == ' ' || c == '\t' || c == '\n' || c == '\r';
            if (isSpace)
            {
                var sb = new StringBuilder();
                while (i < tag.Length && (tag[i] == ' ' || tag[i] == '\t' || tag[i] == '\n' || tag[i] == '\r'))
                {
                    sb.Append(tag[i]);
                    i++;
                }
                tokens.Add(new XmlToken(sb.ToString(), "#7F8C8D"));
                continue;
            }

            if (c == '<' || c == '>' || c == '/' || c == '?' || c == '!')
            {
                tokens.Add(new XmlToken(c.ToString(), "#1F2430"));
                i++;
                continue;
            }

            if (c == '=')
            {
                tokens.Add(new XmlToken("=", "#1F2430"));
                i++;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                int end = tag.IndexOf(c, i + 1);
                if (end < 0) end = tag.Length - 1;
                var quoted = tag.Substring(i, end - i + 1);
                tokens.Add(new XmlToken(quoted, "#C0392B"));
                i = end + 1;
                continue;
            }

            var cb = new StringBuilder();
            while (i < tag.Length)
            {
                var cc = tag[i];
                if (cc == ' ' || cc == '\t' || cc == '\n' || cc == '\r' ||
                    cc == '=' || cc == '"' || cc == '\'' ||
                    cc == '<' || cc == '>' || cc == '/' || cc == '?')
                    break;
                cb.Append(cc);
                i++;
            }

            var s = cb.ToString().Trim();
            if (s.Length > 0)
                tokens.Add(new XmlToken(s, s.StartsWith("?") || s.EndsWith("?") ? "#E67E22" : "#2E6FDB"));
        }

        return tokens;
    }
}
