using Android.Text;
using System;

namespace SecurioClient.Helpers
{
    // Minimal ITextWatcher adapter that allows a lambda to be used as a text-change
    // listener on Android EditText / TextInputEditText fields.
    // Inherit from Java.Lang.Object so the Android runtime can hold a reference.
    public sealed class SimpleTextWatcher : Java.Lang.Object, ITextWatcher
    {
        private readonly Action<string> _onChanged;

        public SimpleTextWatcher(Action<string> onChanged) => _onChanged = onChanged;

        // Called after the text has changed — the only event we act on.
        public void AfterTextChanged(IEditable s) => _onChanged(s?.ToString());

        public void BeforeTextChanged(Java.Lang.ICharSequence s, int start, int count, int after) { }
        public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count) { }
    }
}
