using Android.Text;
using System;

namespace SecurioClient.Helpers
{
    /// <summary>Minimal ITextWatcher adapter that allows a lambda to be used as a text-change listener on Android EditText fields.</summary>
    public sealed class SimpleTextWatcher : Java.Lang.Object, ITextWatcher
    {
        private readonly Action<string> _onChanged;

        /// <summary>Initializes a new instance of SimpleTextWatcher.</summary>
        public SimpleTextWatcher(Action<string> onChanged) => _onChanged = onChanged;

        /// <summary>Called after the text has changed; invokes the provided callback.</summary>
        public void AfterTextChanged(IEditable s) => _onChanged(s?.ToString());

        /// <summary>Called before the text changes; not used.</summary>
        public void BeforeTextChanged(Java.Lang.ICharSequence s, int start, int count, int after) { }
        /// <summary>Called when the text is changing; not used.</summary>
        public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count) { }
    }
}
