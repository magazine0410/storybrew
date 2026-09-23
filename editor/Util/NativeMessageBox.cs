namespace StorybrewEditor.Util
{
    /// <summary>
    /// A system message box, for when the editor's own interface isn't available.
    /// </summary>
    public static class NativeMessageBox
    {
        /// <summary>
        /// Returns true if the message was accepted with OK.
        /// </summary>
        public static bool Show(string message, string title, bool okCancel = false)
        {
#if WINDOWS
            var result = System.Windows.Forms.MessageBox.Show(message, title,
                okCancel ? System.Windows.Forms.MessageBoxButtons.OKCancel : System.Windows.Forms.MessageBoxButtons.OK);
            return result == System.Windows.Forms.DialogResult.OK;
#else
            return LinuxDialogs.ShowMessage(message, title, okCancel);
#endif
        }
    }
}
