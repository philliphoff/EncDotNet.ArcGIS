using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace EncDotNet.SoundCharts.Themes;

/// <summary>
/// Attached properties the theme's templates read: a glyph, a count, actions
/// and a trailing element, and a button that opens its context menu on click.
/// </summary>
/// <remarks>
/// See "Using the theme" in <c>docs/design/look-and-feel.md</c> for the styles
/// that read each property.
/// </remarks>
public static class Ui
{
    /// <summary>
    /// Identifies <c>sc:Ui.Glyph</c>: the 16 px glyph geometry (an
    /// <c>SC_Glyph…</c> resource) a toolbar button, icon button, section
    /// header, list row, chip or empty state shows.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.RegisterAttached(
        "Glyph",
        typeof(Geometry),
        typeof(Ui),
        new FrameworkPropertyMetadata(null));

    /// <summary>Identifies <c>sc:Ui.Count</c>: the count a section header shows after its title.</summary>
    public static readonly DependencyProperty CountProperty = DependencyProperty.RegisterAttached(
        "Count",
        typeof(object),
        typeof(Ui),
        new FrameworkPropertyMetadata(null));

    /// <summary>Identifies <c>sc:Ui.Actions</c>: the buttons an empty state shows under its text.</summary>
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.RegisterAttached(
        "Actions",
        typeof(object),
        typeof(Ui),
        new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Identifies <c>sc:Ui.Trailing</c>: what a list row shows at its right end,
    /// such as a chip or a row action.
    /// </summary>
    public static readonly DependencyProperty TrailingProperty = DependencyProperty.RegisterAttached(
        "Trailing",
        typeof(object),
        typeof(Ui),
        new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Identifies <c>sc:Ui.OpensContextMenu</c>: when true, clicking the button
    /// opens its <see cref="FrameworkElement.ContextMenu"/> below it. The
    /// burger and menu button styles set it.
    /// </summary>
    public static readonly DependencyProperty OpensContextMenuProperty = DependencyProperty.RegisterAttached(
        "OpensContextMenu",
        typeof(bool),
        typeof(Ui),
        new FrameworkPropertyMetadata(false, OnOpensContextMenuChanged));

    /// <summary>Gets an element's glyph.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The glyph geometry, or null.</returns>
    public static Geometry? GetGlyph(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (Geometry?)element.GetValue(GlyphProperty);
    }

    /// <summary>Sets an element's glyph.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The glyph geometry, or null for none.</param>
    public static void SetGlyph(DependencyObject element, Geometry? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(GlyphProperty, value);
    }

    /// <summary>Gets a section header's count.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The count, or null.</returns>
    public static object? GetCount(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(CountProperty);
    }

    /// <summary>Sets a section header's count.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The count, or null for none.</param>
    public static void SetCount(DependencyObject element, object? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(CountProperty, value);
    }

    /// <summary>Gets an empty state's actions.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The actions, or null.</returns>
    public static object? GetActions(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(ActionsProperty);
    }

    /// <summary>Sets an empty state's actions.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The actions, usually a horizontal panel of buttons.</param>
    public static void SetActions(DependencyObject element, object? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ActionsProperty, value);
    }

    /// <summary>Gets a list row's trailing element.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The trailing element, or null.</returns>
    public static object? GetTrailing(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(TrailingProperty);
    }

    /// <summary>Sets a list row's trailing element.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The trailing element, or null for none.</param>
    public static void SetTrailing(DependencyObject element, object? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TrailingProperty, value);
    }

    /// <summary>Gets whether clicking the button opens its context menu.</summary>
    /// <param name="element">The button.</param>
    /// <returns>True if it does.</returns>
    public static bool GetOpensContextMenu(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(OpensContextMenuProperty);
    }

    /// <summary>Sets whether clicking the button opens its context menu.</summary>
    /// <param name="element">The button.</param>
    /// <param name="value">True to open the context menu on click.</param>
    public static void SetOpensContextMenu(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(OpensContextMenuProperty, value);
    }

    private static void OnOpensContextMenuChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ButtonBase button)
        {
            return;
        }

        button.Click -= OnButtonClick;
        if (e.NewValue is true)
        {
            button.Click += OnButtonClick;
        }
    }

    private static void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ButtonBase { ContextMenu: { } menu } button)
        {
            return;
        }

        // A menu opened through IsOpen, rather than by a right-click, doesn't
        // take the button's DataContext, so its command bindings would fail.
        // A local value (a binding included) is the view's choice; keep it.
        if (menu.ReadLocalValue(FrameworkElement.DataContextProperty) == DependencyProperty.UnsetValue)
        {
            menu.SetBinding(FrameworkElement.DataContextProperty, new Binding(nameof(FrameworkElement.DataContext)) { Source = button });
        }

        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
