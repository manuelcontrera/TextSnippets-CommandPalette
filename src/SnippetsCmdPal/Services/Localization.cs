using System;
using System.Globalization;

namespace SnippetsCmdPal.Services;

/// <summary>
/// Centralized bilingual localization service (English default, Spanish for Spanish locales).
/// </summary>
public static class Strings
{
    public static bool IsSpanish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("es", StringComparison.OrdinalIgnoreCase);

    // ── Command Palette Provider ──
    public static string ProviderTitle => "Text Snippets";
    public static string ProviderSubtitle => IsSpanish
        ? "Gestiona y pega snippets de texto con expansión automática"
        : "Manage and paste text snippets with automatic expansion";

    // ── List Page ──
    public static string PageTitle => "Text Snippets (Raycast Style)";
    public static string PagePlaceholder => IsSpanish
        ? "Busca por disparador (ej. !email), nombre o contenido..."
        : "Search by trigger (e.g. !email), title, or content...";
    public static string CreateNewSnippet => IsSpanish ? "➕ Crear nuevo snippet..." : "➕ Create new snippet...";
    public static string CreateNewSnippetSubtitle => IsSpanish
        ? "Abre el formulario visual para agregar un nuevo atajo y texto"
        : "Open visual form to add a new trigger and text";
    public static string CreateNewSnippetDetailsTitle => IsSpanish ? "Crear nuevo snippet" : "Create new snippet";
    public static string CreateNewSnippetDetailsBody => IsSpanish
        ? "Haz clic o pulsa **Enter** para abrir la ventana visual de creación de snippets.\n\nPodrás configurar:\n- **Disparador** (ej: `!correo`, `!tel`)\n- **Título**\n- **Contenido** con variables como `{date}`, `{time}`, `{clipboard}`\n- **Etiquetas**"
        : "Click or press **Enter** to open the visual snippet editor.\n\nYou can configure:\n- **Trigger** (e.g. `!email`, `!phone`)\n- **Title**\n- **Content** with variables like `{date}`, `{time}`, `{clipboard}`\n- **Tags**";

    public static string TriggerHeading => IsSpanish ? "Disparador" : "Trigger";
    public static string ContentHeading => IsSpanish ? "Contenido" : "Content";
    public static string OptionsHeading => IsSpanish ? "Opciones configuradas" : "Configured Options";
    public static string PreviewHeading => IsSpanish ? "Vista Previa Evaluada" : "Evaluated Preview";
    public static string OptionsHint(string trigger) => IsSpanish
        ? $"*Al escribir `{trigger}` en cualquier app, se desplegará el selector rápido.*"
        : $"*Typing `{trigger}` in any app will open the quick selector menu.*";
    public static string SingleHint => IsSpanish
        ? "*Pulsa **Enter** para pegar en tu ventana activa.*\n*Usa **Ctrl+K** o el menú contextual para editarlo.*"
        : "*Press **Enter** to paste into your active window.*\n*Use **Ctrl+K** or context menu to edit.*";
    public static string TagsLabel => IsSpanish ? "Etiquetas" : "Tags";
    public static string OptionsCount(int count) => IsSpanish
        ? $"{count} opciones disponibles"
        : $"{count} options available";
    public static string PasteOption(int index, string value) => IsSpanish
        ? $"Pegar opción {index}: {value}"
        : $"Paste option {index}: {value}";

    // ── Commands ──
    public static string CommandNewSnippet => IsSpanish ? "Crear nuevo snippet..." : "Create new snippet...";
    public static string CommandEditSnippet(string title) => IsSpanish
        ? $"Editar snippet \"{title}\"..."
        : $"Edit snippet \"{title}\"...";
    public static string CommandCopySnippet(string title) => IsSpanish
        ? $"Copiar snippet \"{title}\""
        : $"Copy snippet \"{title}\"";
    public static string CommandDeleteSnippet(string title) => IsSpanish
        ? $"Eliminar snippet \"{title}\""
        : $"Delete snippet \"{title}\"";
    public static string CommandOpenFolder => IsSpanish
        ? "Abrir carpeta de snippets (JSON)"
        : "Open snippets folder (JSON)";
    public static string CommandToggleHook(bool enabled) => enabled
        ? (IsSpanish ? "Desactivar expansión automática (inline)" : "Disable automatic inline expansion")
        : (IsSpanish ? "Activar expansión automática (inline)" : "Enable automatic inline expansion");

    public static string ToastDeleted(string title) => IsSpanish
        ? $"Snippet \"{title}\" eliminado."
        : $"Snippet \"{title}\" deleted.";
    public static string ToastHookState(bool enabled) => enabled
        ? (IsSpanish ? "Expansión automática global activada" : "Global auto-expansion enabled")
        : (IsSpanish ? "Expansión automática global desactivada" : "Global auto-expansion disabled");

    // ── Editor Window ──
    public static string EditorTitle(bool isEditing) => isEditing
        ? (IsSpanish ? "✏️ Editar Snippet" : "✏️ Edit Snippet")
        : (IsSpanish ? "➕ Crear Nuevo Snippet" : "➕ Create New Snippet");
    public static string EditorWindowTitle(bool isEditing, string title) => isEditing
        ? (IsSpanish ? $"Editar: {title}" : $"Edit: {title}")
        : (IsSpanish ? "Crear Nuevo Snippet" : "Create New Snippet");
    public static string EditorSubtitle => IsSpanish
        ? "Configura el disparador rápido, texto expandido o lista de opciones."
        : "Configure quick trigger, expanded text, or multiple choice options.";
    public static string LabelTitle => IsSpanish ? "Título del snippet" : "Snippet title";
    public static string PlaceholderTitle => IsSpanish
        ? "ej: Correo Personal, Saludo de Reunión…"
        : "e.g. Personal Email, Meeting Intro…";
    public static string LabelTrigger => IsSpanish ? "Disparador (atajo de teclado)" : "Trigger (keyboard shortcut)";
    public static string TipTrigger => IsSpanish
        ? "Recomendado: empieza con ! (ej: !email, !tel, !firma)"
        : "Recommended: starts with ! (e.g. !email, !phone, !sig)";
    public static string LabelContentType => IsSpanish ? "Tipo de contenido" : "Content type";
    public static string RadioSingle => IsSpanish
        ? "Texto único / Plantilla con variables"
        : "Single text / Template with variables";
    public static string LabelContentToExpand => IsSpanish ? "Contenido a expandir:" : "Content to expand:";
    public static string PlaceholderContent => IsSpanish
        ? "Escribe el texto aquí. Usa {date}, {time}, {clipboard}, {uuid}…"
        : "Type your text here. Use {date}, {time}, {clipboard}, {uuid}…";
    public static string LabelInsertVariable => IsSpanish ? "Insertar variable en el cursor:" : "Insert variable at cursor:";
    public static string RadioMultiple => IsSpanish
        ? "Múltiples opciones (menú interactivo al escribir)"
        : "Multiple options (interactive picker on trigger)";
    public static string HelpMultiple => IsSpanish
        ? "Una opción por línea. Al escribir el disparador, aparecerá un menú para elegir:"
        : "One option per line. When you type the trigger, a popup lets you choose:";
    public static string PlaceholderMultiple => IsSpanish
        ? "Primera opción\nSegunda opción\nTercera opción…"
        : "First option\nSecond option\nThird option…";
    public static string LabelTags => IsSpanish
        ? "Etiquetas (opcional, separadas por coma)"
        : "Tags (optional, comma-separated)";
    public static string PlaceholderTags => IsSpanish ? "ej: trabajo, plantilla, correo" : "e.g. work, template, email";
    public static string TipVariables => IsSpanish
        ? "💡 Variables disponibles: {date} (fecha), {time} (hora), {clipboard} (portapapeles), {uuid} (ID único), {datetime} (fecha y hora)."
        : "💡 Available variables: {date} (date), {time} (time), {clipboard} (clipboard text), {uuid} (unique ID), {datetime} (date and time).";
    public static string EditorTipMultiple => IsSpanish
        ? "💡 Al teclear el disparador aparecerá un selector rápido. Elige con flechas o números 1-9."
        : "💡 Typing the trigger will show a quick picker. Select with arrow keys or numbers 1-9.";
    public static string KeyHint => IsSpanish
        ? "Guardar: Ctrl+Enter | Cancelar: Esc"
        : "Save: Ctrl+Enter | Cancel: Esc";
    public static string BtnCancel => IsSpanish ? "Cancelar (Esc)" : "Cancel (Esc)";
    public static string BtnSave(bool isEditing) => isEditing
        ? (IsSpanish ? "💾 Actualizar snippet" : "💾 Update snippet")
        : (IsSpanish ? "💾 Guardar snippet" : "💾 Save snippet");
    public static string BtnDelete => IsSpanish ? "🗑️ Eliminar" : "🗑️ Delete";

    public static string DialogDeleteTitle => IsSpanish ? "Confirmar eliminación" : "Confirm deletion";
    public static string DialogDeleteContent(string title, string trigger) => IsSpanish
        ? $"¿Eliminar permanentemente \"{title}\" ({trigger})?"
        : $"Permanently delete \"{title}\" ({trigger})?";
    public static string DialogDeleteConfirm => IsSpanish ? "Eliminar" : "Delete";
    public static string DialogDeleteCancel => IsSpanish ? "Cancelar" : "Cancel";

    public static string ErrorTitleRequired => IsSpanish
        ? "Por favor ingresa un título para el snippet."
        : "Please enter a title for the snippet.";
    public static string ErrorTriggerRequired => IsSpanish
        ? "Por favor ingresa un disparador (ej: !email)."
        : "Please enter a trigger (e.g. !email).";
    public static string ErrorOptionsRequired => IsSpanish
        ? "Debes escribir al menos una opción (una por línea)."
        : "You must enter at least one option (one per line).";
    public static string ErrorContentRequired => IsSpanish
        ? "Por favor escribe el contenido del snippet."
        : "Please enter snippet content.";
}
