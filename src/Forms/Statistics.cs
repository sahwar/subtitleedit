﻿using Nikse.SubtitleEdit.Core;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Logic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class Statistics : PositionAndSizeForm
    {

        public class StingOrdinalComparer : IEqualityComparer<string>, IComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (x == null)
                {
                    return y == null;
                }

                return x.Equals(y, StringComparison.Ordinal);
            }

            public int GetHashCode(string x)
            {
                return x.GetHashCode();
            }

            public int Compare(string x, string y)
            {
                return string.CompareOrdinal(x, y);
            }
        }

        private readonly Subtitle _subtitle;
        private readonly SubtitleFormat _format;
        private readonly LanguageStructure.Statistics _l;
        private string _mostUsedLines;
        private string _general;
        private int _totalWords;
        private string _mostUsedWords;
        private const string WriteFormat = @"File generated by: Subtitle Edit
https://www.nikse.dk/subtitleedit/
https://github.com/SubtitleEdit/subtitleedit
============================= General =============================
{0}
============================= Most Used Words =============================
{1}
============================= Most Used Lines =============================
{2}";

        private static readonly char[] ExpectedChars = { '♪', '♫', '"', '(', ')', '[', ']', ' ', ',', '!', '?', '.', ':', ';', '-', '_', '@', '<', '>', '/', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '،', '؟', '؛' };

        public Statistics(Subtitle subtitle, string fileName, SubtitleFormat format)
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);

            _subtitle = subtitle;
            _format = format;

            _l = Configuration.Settings.Language.Statistics;
            Text = string.IsNullOrEmpty(fileName) ? _l.Title : string.Format(_l.TitleWithFileName, fileName);
            groupBoxGeneral.Text = _l.GeneralStatistics;
            groupBoxMostUsed.Text = _l.MostUsed;
            labelMostUsedWords.Text = _l.MostUsedWords;
            labelMostUsedLines.Text = _l.MostUsedLines;
            buttonExport.Text = _l.Export;
            buttonOK.Text = Configuration.Settings.Language.General.Ok;
            UiUtil.FixLargeFonts(this, buttonOK);

            CalculateWordStatistics();
            CalculateGeneralStatistics();
            textBoxGeneral.Text = _general;
            textBoxGeneral.SelectionStart = 0;
            textBoxGeneral.SelectionLength = 0;
            textBoxGeneral.ScrollToCaret();
            textBoxMostUsedWords.Text = _mostUsedWords;

            CalculateMostUsedLines();
            textBoxMostUsedLines.Text = _mostUsedLines;
        }

        private void CalculateGeneralStatistics()
        {
            if (_subtitle == null || _subtitle.Paragraphs.Count == 0)
            {
                textBoxGeneral.Text = _l.NothingFound;
                return;
            }

            var allText = new StringBuilder();
            int minimumLineLength = 99999999;
            int maximumLineLength = 0;
            long totalLineLength = 0;
            int minimumSingleLineLength = 99999999;
            int maximumSingleLineLength = 0;
            long totalSingleLineLength = 0;
            long totalSingleLines = 0;
            double minimumDuration = 100000000;
            double maximumDuration = 0;
            double totalDuration = 0;
            double minimumCharsSec = 100000000;
            double maximumCharsSec = 0;
            double totalCharsSec = 0;
            foreach (Paragraph p in _subtitle.Paragraphs)
            {
                allText.Append(p.Text);

                int len = p.Text.Replace(Environment.NewLine, string.Empty).Length;
                minimumLineLength = Math.Min(minimumLineLength, len);
                maximumLineLength = Math.Max(len, maximumLineLength);
                totalLineLength += len;

                double duration = p.Duration.TotalMilliseconds;
                minimumDuration = Math.Min(duration, minimumDuration);
                maximumDuration = Math.Max(duration, maximumDuration);
                totalDuration += duration;

                var charsSec = Utilities.GetCharactersPerSecond(p);
                minimumCharsSec = Math.Min(charsSec, minimumCharsSec);
                maximumCharsSec = Math.Max(charsSec, maximumCharsSec);
                totalCharsSec += charsSec;

                foreach (string line in p.Text.SplitToLines())
                {
                    var l = line.Length;
                    minimumSingleLineLength = Math.Min(l, minimumSingleLineLength);
                    maximumSingleLineLength = Math.Max(l, maximumSingleLineLength);
                    totalSingleLineLength += l;
                    totalSingleLines++;
                }
            }

            var sb = new StringBuilder();
            int sourceLength = _subtitle.ToText(_format).Length;
            var allTextToLower = allText.ToString().ToLowerInvariant();

            sb.AppendLine(string.Format(_l.NumberOfLinesX, _subtitle.Paragraphs.Count));
            sb.AppendLine(string.Format(_l.LengthInFormatXinCharactersY, _format.FriendlyName, sourceLength));
            sb.AppendLine(string.Format(_l.NumberOfCharactersInTextOnly, allText.Replace("\r\n", "\n").Length));
            sb.AppendLine(string.Format(_l.TotalDuration, new TimeCode(totalDuration).ToDisplayString()));
            sb.AppendLine(string.Format(_l.TotalCharsPerSecond, HtmlUtil.RemoveHtmlTags(allText.ToString()).Length / (totalDuration / TimeCode.BaseUnit)));
            sb.AppendLine(string.Format(_l.TotalWords, _totalWords));
            sb.AppendLine(string.Format(_l.NumberOfItalicTags, Utilities.CountTagInText(allTextToLower, "<i>")));
            sb.AppendLine(string.Format(_l.NumberOfBoldTags, Utilities.CountTagInText(allTextToLower, "<b>")));
            sb.AppendLine(string.Format(_l.NumberOfUnderlineTags, Utilities.CountTagInText(allTextToLower, "<u>")));
            sb.AppendLine(string.Format(_l.NumberOfFontTags, Utilities.CountTagInText(allTextToLower, "<font ")));
            sb.AppendLine(string.Format(_l.NumberOfAlignmentTags, Utilities.CountTagInText(allTextToLower, "{\\a")));
            sb.AppendLine();
            sb.AppendLine(string.Format(_l.LineLengthMinimum, minimumLineLength));
            sb.AppendLine(string.Format(_l.LineLengthMaximum, maximumLineLength));
            sb.AppendLine(string.Format(_l.LineLengthAverage, totalLineLength / _subtitle.Paragraphs.Count));
            sb.AppendLine(string.Format(_l.LinesPerSubtitleAverage, (((double)totalSingleLines) / _subtitle.Paragraphs.Count)));
            sb.AppendLine();
            sb.AppendLine(string.Format(_l.SingleLineLengthMinimum, minimumSingleLineLength));
            sb.AppendLine(string.Format(_l.SingleLineLengthMaximum, maximumSingleLineLength));
            sb.AppendLine(string.Format(_l.SingleLineLengthAverage, totalSingleLineLength / totalSingleLines));
            sb.AppendLine();
            sb.AppendLine(string.Format(_l.DurationMinimum, minimumDuration / TimeCode.BaseUnit));
            sb.AppendLine(string.Format(_l.DurationMaximum, maximumDuration / TimeCode.BaseUnit));
            sb.AppendLine(string.Format(_l.DurationAverage, totalDuration / _subtitle.Paragraphs.Count / TimeCode.BaseUnit));
            sb.AppendLine();
            sb.AppendLine(string.Format(_l.CharactersPerSecondMinimum, minimumCharsSec));
            sb.AppendLine(string.Format(_l.CharactersPerSecondMaximum, maximumCharsSec));
            sb.AppendLine(string.Format(_l.CharactersPerSecondAverage, totalCharsSec / _subtitle.Paragraphs.Count));
            sb.AppendLine();
            _general = sb.ToString().Trim();
        }

        private void Statistics_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
            else if (e.KeyData == (Keys.Control | Keys.C))
            {
                Clipboard.SetText(string.Format(WriteFormat, _general, _mostUsedWords, _mostUsedLines), TextDataFormat.UnicodeText);
            }
        }

        private void MostUsedWordsAdd(Dictionary<string, int> hashtable, string input)
        {
            var text = input;
            if (text.Contains("< "))
            {
                text = HtmlUtil.FixInvalidItalicTags(text);
            }

            text = StripHtmlTags(text);

            var idx = text.IndexOf("<font", StringComparison.OrdinalIgnoreCase);
            var error = false;
            while (idx >= 0)
            {
                var endIdx = text.IndexOf('>', idx + 5);
                if (endIdx < idx)
                {
                    error = true;
                    break;
                }
                endIdx++;
                text = text.Remove(idx, endIdx - idx);
                idx = text.IndexOf("<font", idx, StringComparison.OrdinalIgnoreCase);
            }
            if (!error)
            {
                text = text.Replace("</font>", ".");
            }

            foreach (string word in text.Split(ExpectedChars, StringSplitOptions.RemoveEmptyEntries))
            {
                var s = word.Trim();
                if (s.Length > 1 && hashtable.ContainsKey(s))
                {
                    hashtable[s]++;
                }
                else if (s.Length > 1)
                {
                    hashtable.Add(s, 1);
                }
            }
        }

        private static void MostUsedLinesAdd(Dictionary<string, int> hashtable, string input)
        {
            var text = StripHtmlTags(input)
                .Replace('!', '.')
                .Replace('?', '.')
                .Replace("...", ".")
                .Replace("..", ".")
                .Replace('-', ' ')
                .FixExtraSpaces();

            foreach (string line in text.Split('.'))
            {
                var s = line.Trim();
                if (hashtable.ContainsKey(s))
                {
                    hashtable[s]++;
                }
                else if (s.Length > 0 && s.Contains(' '))
                {
                    hashtable.Add(s, 1);
                }
            }
        }

        private static string StripHtmlTags(string input)
        {
            var text = input.Trim('\'').Replace("\"", string.Empty);

            if (text.Length < 8)
            {
                return text;
            }

            text = text.Replace("<i>", string.Empty);
            text = text.Replace("</i>", ".");
            text = text.Replace("<I>", string.Empty);
            text = text.Replace("</I>", ".");
            text = text.Replace("<b>", string.Empty);
            text = text.Replace("</b>", ".");
            text = text.Replace("<B>", string.Empty);
            text = text.Replace("</B>", ".");
            text = text.Replace("<u>", string.Empty);
            text = text.Replace("</u>", ".");
            text = text.Replace("<U>", string.Empty);
            text = text.Replace("</U>", ".");
            return text;
        }

        private void CalculateWordStatistics()
        {
            var hashtable = new Dictionary<string, int>(new StingOrdinalComparer());

            foreach (Paragraph p in _subtitle.Paragraphs)
            {
                MostUsedWordsAdd(hashtable, p.Text);
                _totalWords += p.Text.CountWords();
            }

            var sortedTable = new SortedDictionary<string, string>(new StingOrdinalComparer());
            foreach (KeyValuePair<string, int> item in hashtable)
            {
                if (item.Value > 1)
                {
                    sortedTable.Add($"{item.Value:0000}" + "_" + item.Key, item.Value + ": " + item.Key);
                }
            }

            var sb = new StringBuilder();
            if (sortedTable.Count > 0)
            {
                var temp = string.Empty;
                foreach (KeyValuePair<string, string> item in sortedTable)
                {
                    temp = item.Value + Environment.NewLine + temp;
                }
                sb.AppendLine(temp);
            }
            else
            {
                sb.AppendLine(_l.NothingFound);
            }
            _mostUsedWords = sb.ToString();
        }

        private void CalculateMostUsedLines()
        {
            var hashtable = new Dictionary<string, int>();

            foreach (Paragraph p in _subtitle.Paragraphs)
            {
                MostUsedLinesAdd(hashtable, p.Text.Replace(Environment.NewLine, " ").Replace("  ", " "));
            }

            var sortedTable = new SortedDictionary<string, string>(new StingOrdinalComparer());
            foreach (KeyValuePair<string, int> item in hashtable)
            {
                if (item.Value > 1)
                {
                    sortedTable.Add($"{item.Value:0000}" + "_" + item.Key, item.Value + ": " + item.Key);
                }
            }

            var sb = new StringBuilder();
            if (sortedTable.Count > 0)
            {
                var temp = string.Empty;
                foreach (KeyValuePair<string, string> item in sortedTable)
                {
                    temp = item.Value + Environment.NewLine + temp;
                }
                sb.AppendLine(temp);
            }
            else
            {
                sb.AppendLine(_l.NothingFound);
            }
            _mostUsedLines = sb.ToString();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            using (var saveFile = new SaveFileDialog { Filter = Configuration.Settings.Language.Main.TextFiles + " (*.txt)|*.txt|NFO files (*.nfo)|*.nfo" })
            {
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFile.FileName;
                    var statistic = string.Format(WriteFormat, _general, _mostUsedWords, _mostUsedLines);
                    System.IO.File.WriteAllText(fileName, statistic);
                }
            }
        }

    }
}
