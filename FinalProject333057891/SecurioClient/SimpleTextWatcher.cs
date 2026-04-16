  using Android.Text;
using System;

public class SimpleTextWatcher : Java.Lang.Object, ITextWatcher
  {
      private readonly Action<string> _onTextChanged;
      public SimpleTextWatcher(Action<string> onTextChanged) => _onTextChanged = onTextChanged;
      public void AfterTextChanged(IEditable s) { }
      public void BeforeTextChanged(Java.Lang.ICharSequence s, int start, int count, int after) { }
      public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count)
          => _onTextChanged?.Invoke(s?.ToString());
  }
  