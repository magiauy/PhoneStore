using Microsoft.UI.Xaml.Controls;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class NotesInputDialog : ContentDialog
    {
        public string Notes { get; private set; } = string.Empty;

        public NotesInputDialog()
        {
            this.InitializeComponent();
        }

        public NotesInputDialog(string initialNotes) : this()
        {
            if (!string.IsNullOrWhiteSpace(initialNotes))
            {
                NotesTextBox.Text = initialNotes;
                Notes = initialNotes;
            }
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Notes = NotesTextBox.Text?.Trim() ?? string.Empty;
        }
    }
}

