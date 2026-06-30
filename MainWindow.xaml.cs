using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using EasyDL.Models;

namespace EasyDL;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private bool _suppressResCombo;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => SetTitleBarColor();

        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.IsDarkMode))
                    ApplyTheme(vm.IsDarkMode);
            };
        }
    }

    private void SetTitleBarColor()
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            int color = 0x00CC7608;
            DwmSetWindowAttribute(handle, 35, ref color, 4);
            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(handle, 36, ref textColor, 4);
        }
        catch { }
    }

    private void ApplyTheme(bool dark)
    {
        try
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri(dark ? "pack://application:,,,/DarkTheme.xaml" : "pack://application:,,,/LightTheme.xaml")
            };

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);
            Background = (Brush)FindResource("WindowBg");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Theme error: {ex.Message}");
        }
    }

    private static DownloadItem? FindDataContext(object sender)
    {
        if (sender is not DependencyObject source) return null;
        var current = source;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is DownloadItem item)
                return item;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void DownloadBtn_Click(object sender, RoutedEventArgs e)
    {
        if (FindDataContext(sender) is not DownloadItem item) return;
        var vm = (ViewModels.MainViewModel)DataContext;

        if (item.Status == "Downloading" || item.Status == "Converting for mobile (128x160)...")
        {
            vm.CancelDownloadCommand.Execute(item);
        }
        else
        {
            item.Status = "Ready";
            vm.DownloadSingleCommand.Execute(item);
        }
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (FindDataContext(sender) is not DownloadItem item) return;
        ((ViewModels.MainViewModel)DataContext).DeleteItemCommand.Execute(item);
    }

    private void ResolutionPill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string resolution)
        {
            if (FindDataContext(rb) is DownloadItem item)
                item.SelectedResolution = resolution;
        }
    }

    private void ResCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressResCombo) return;
        if (ResCombo.SelectedIndex < 0 || DataContext is not ViewModels.MainViewModel vm) return;
        string res = ResCombo.SelectedItem?.ToString() ?? "720p";
        vm.SetAllResolutions(res);
    }

    private void ConvertToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.MainViewModel vm) return;
        bool on = ConvertToggle.IsChecked == true;
        vm.SetAllConvertToKeypad(on);

        _suppressResCombo = true;
        if (on)
        {
            ResCombo.Items.Clear();
            ResCombo.Items.Add("240p");
            ResCombo.SelectedIndex = 0;
            ResCombo.IsEnabled = false;
        }
        else
        {
            ResCombo.Items.Clear();
            ResCombo.Items.Add("1080p");
            ResCombo.Items.Add("720p");
            ResCombo.Items.Add("480p");
            ResCombo.Items.Add("320p");
            ResCombo.SelectedIndex = 1;
            ResCombo.IsEnabled = true;
        }
        _suppressResCombo = false;
        vm.SetAllResolutions(on ? "240p" : "720p");
    }

    private void StatusPill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string filter && DataContext is ViewModels.MainViewModel vm)
            vm.SetFilter(filter);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var about = new Views.AboutWindow { Owner = this };
            if (DataContext is ViewModels.MainViewModel vm)
                about.ApplyTheme(vm.IsDarkMode);
            about.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"About error: {ex.Message}", "Gograb", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Folder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select Download Folder" };
        if (dialog.ShowDialog() == true)
        {
            Services.SettingsManager.Settings.VideoFolder = dialog.FolderName;
            Services.SettingsManager.Save();
            if (DataContext is ViewModels.MainViewModel vm)
                vm.StatusMessage = $"Folder: {dialog.FolderName}";
        }
    }

    private void DarkModeToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
            vm.IsDarkMode = DarkModeToggle.IsChecked == true;
    }

    private void ClearCompleted_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.MainViewModel vm) return;
        var completed = vm.AllItems.Where(i => i.Status == "Completed").ToList();
        foreach (var item in completed)
            vm.AllItems.Remove(item);
        vm.RefreshCounts();
    }

    private void Thumbnail_RightClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
    {
        if (FindDataContext(sender) is not DownloadItem item) return;

        var bg = (Brush)FindResource("BgToolbar");
        var fg = (Brush)FindResource("Text");
        var brd = (Brush)FindResource("Border");

        var menu = new ContextMenu
        {
            Padding = new Thickness(0),
            MinWidth = 0,
            FontSize = 11,
            Background = bg,
            Foreground = fg,
            BorderBrush = brd,
            BorderThickness = new Thickness(1),
            Template = BuildContextMenuTemplate()
        };

        menu.Items.Add(BuildMenuItem("Start", bg, fg, brd, (_, _) => { if (DataContext is ViewModels.MainViewModel vm && item.Status == "Ready") vm.DownloadSingleCommand.Execute(item); }));
        menu.Items.Add(BuildMenuItem("Open in browser", bg, fg, brd, (_, _) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Url) { UseShellExecute = true }); } catch { } }));
        menu.Items.Add(BuildMenuItem("Copy link", bg, fg, brd, (_, _) => { Clipboard.SetText(item.Url); if (DataContext is ViewModels.MainViewModel vm) vm.StatusMessage = "Link copied!"; }));
        menu.Items.Add(new Separator { Padding = new Thickness(0), Background = brd });
        menu.Items.Add(BuildMenuItem("Refresh", bg, fg, brd, async (_, _) => { if (DataContext is ViewModels.MainViewModel vm) await vm.RefreshItemAsync(item); }));
        menu.Items.Add(BuildMenuItem("Remove", bg, fg, brd, (_, _) => { if (DataContext is ViewModels.MainViewModel vm) vm.AllItems.Remove(item); }, (Brush)FindResource("Red")));

        menu.IsOpen = true;
    }

    private static ControlTemplate BuildContextMenuTemplate()
    {
        var template = new ControlTemplate(typeof(ContextMenu));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "Bd";
        borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var scrollFactory = new FrameworkElementFactory(typeof(ScrollViewer));
        scrollFactory.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        var itemsFactory = new FrameworkElementFactory(typeof(ItemsPresenter));
        itemsFactory.SetValue(KeyboardNavigation.DirectionalNavigationProperty, KeyboardNavigationMode.Cycle);
        scrollFactory.AppendChild(itemsFactory);
        borderFactory.AppendChild(scrollFactory);

        template.VisualTree = borderFactory;
        return template;
    }

    private MenuItem BuildMenuItem(string header, Brush bg, Brush fg, Brush border, RoutedEventHandler click, Brush? foreground = null)
    {
        var mi = new MenuItem
        {
            Header = header,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            FontSize = 11,
            Foreground = foreground ?? fg,
            Template = BuildMenuItemTemplate(fg, border)
        };
        mi.Click += click;
        return mi;
    }

    private ControlTemplate BuildMenuItemTemplate(Brush fg, Brush border)
    {
        var template = new ControlTemplate(typeof(MenuItem));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "Bd";
        borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(MenuItem.BackgroundProperty));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(10, 3, 14, 3));
        borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        cpFactory.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        cpFactory.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        borderFactory.AppendChild(cpFactory);

        template.VisualTree = borderFactory;

        var trigger = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        trigger.Setters.Add(new Setter(Border.BackgroundProperty, border, "Bd"));
        template.Triggers.Add(trigger);

        return template;
    }
}
