using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace HarManager.Helpers
{
    public class TextMateHelper
    {
        private static readonly RegistryOptions _registryOptions = new(ThemeName.DarkPlus);

        public static readonly AttachedProperty<string> LanguageProperty =
            AvaloniaProperty.RegisterAttached<TextMateHelper, TextEditor, string>(
                "Language", defaultValue: "text");

        public static string GetLanguage(TextEditor element)
        {
            return element.GetValue(LanguageProperty);
        }

        public static void SetLanguage(TextEditor element, string value)
        {
            element.SetValue(LanguageProperty, value);
        }

        static TextMateHelper()
        {
            LanguageProperty.Changed.AddClassHandler<TextEditor>(OnLanguageChanged);
        }

        private static void OnLanguageChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.NewValue is string language)
            {
                var installation = editor.GetValue(TextMateInstallationProperty);
                if (installation == null)
                {
                    installation = editor.InstallTextMate(_registryOptions);
                    editor.SetValue(TextMateInstallationProperty, installation);
                }

                var scope = _registryOptions.GetScopeByLanguageId(language);
                if (scope != null)
                {
                    installation.SetGrammar(scope);
                }
            }
        }

        private static readonly AttachedProperty<TextMate.Installation> TextMateInstallationProperty =
            AvaloniaProperty.RegisterAttached<TextMateHelper, TextEditor, TextMate.Installation>(
                "TextMateInstallation");
    }
}

