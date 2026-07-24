using System.Collections.Generic;

namespace FracturedChorus.Narrative.Vn
{
    public readonly struct VnDialogueLogEntry
    {
        public readonly string Speaker;
        public readonly string Text;

        public VnDialogueLogEntry(string speaker, string text)
        {
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
        }
    }

    public sealed class VnDialogueLog
    {
        public static VnDialogueLog Session { get; } = new VnDialogueLog();

        private readonly List<VnDialogueLogEntry> _entries = new List<VnDialogueLogEntry>(64);

        public IReadOnlyList<VnDialogueLogEntry> Entries => _entries;
        public int Count => _entries.Count;

        public void Clear()
        {
            _entries.Clear();
        }

        public void Append(string speaker, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            _entries.Add(new VnDialogueLogEntry(speaker, text.Trim()));
        }
    }
}
