#if WINDOWS
using BrewLib.Util;
using System.Windows.Forms;

namespace StorybrewEditor.Util
{
    public class FormsClipboard : ClipboardBackend
    {
        public void SetText(string text)
            => Clipboard.SetText(text, TextDataFormat.UnicodeText);

        public string GetText()
            => Clipboard.GetText(TextDataFormat.UnicodeText);
    }
}
#endif
