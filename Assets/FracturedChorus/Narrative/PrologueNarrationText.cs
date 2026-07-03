using System.Collections.Generic;

namespace FracturedChorus.Narrative
{
    public static class PrologueNarrationText
    {
        private const int MaxCharsPerLine = 54;
        private const int MinLastLineChars = 20;

        public static string WrapBalanced(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= MaxCharsPerLine)
            {
                return text;
            }

            var words = text.Split(' ');
            if (words.Length <= 1)
            {
                return text;
            }

            var lineCount = 2;
            while (lineCount <= 4)
            {
                var wrapped = TryWrap(words, lineCount);
                if (wrapped != null)
                {
                    return wrapped;
                }

                lineCount++;
            }

            return text;
        }

        private static string TryWrap(string[] words, int lineCount)
        {
            var bestLines = (string[])null;
            var bestScore = int.MaxValue;

            BuildLines(words, 0, lineCount, new List<string>(), ref bestLines, ref bestScore);
            if (bestLines == null)
            {
                return null;
            }

            return string.Join("\n", bestLines);
        }

        private static void BuildLines(
            string[] words,
            int wordIndex,
            int linesLeft,
            List<string> current,
            ref string[] bestLines,
            ref int bestScore)
        {
            if (linesLeft == 1)
            {
                var last = string.Join(" ", words, wordIndex, words.Length - wordIndex);
                if (last.Length > MaxCharsPerLine)
                {
                    return;
                }

                current.Add(last);
                ScoreLines(current, ref bestLines, ref bestScore);
                current.RemoveAt(current.Count - 1);
                return;
            }

            for (var i = wordIndex + 1; i <= words.Length - linesLeft; i++)
            {
                var line = string.Join(" ", words, wordIndex, i - wordIndex);
                if (line.Length > MaxCharsPerLine)
                {
                    break;
                }

                current.Add(line);
                BuildLines(words, i, linesLeft - 1, current, ref bestLines, ref bestScore);
                current.RemoveAt(current.Count - 1);
            }
        }

        private static void ScoreLines(List<string> lines, ref string[] bestLines, ref int bestScore)
        {
            if (lines.Count == 0)
            {
                return;
            }

            var last = lines[lines.Count - 1];
            if (last.Length < MinLastLineChars && lines.Count > 1)
            {
                return;
            }

            var maxLen = 0;
            var minLen = int.MaxValue;
            for (var i = 0; i < lines.Count; i++)
            {
                var len = lines[i].Length;
                maxLen = len > maxLen ? len : maxLen;
                minLen = len < minLen ? len : minLen;
            }

            var score = (maxLen - minLen) * 10 + lines.Count;
            if (score >= bestScore)
            {
                return;
            }

            bestScore = score;
            bestLines = lines.ToArray();
        }
    }
}
