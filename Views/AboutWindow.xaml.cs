using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace EasyDL.Views;

public partial class AboutWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public AboutWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => SetTitleBarColor();
    }

    private void SetTitleBarColor()
    {
        try
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int color = 0x00CC7608;
            DwmSetWindowAttribute(handle, 35, ref color, 4);
            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(handle, 36, ref textColor, 4);
        }
        catch { }
    }

    public void ApplyTheme(bool dark)
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
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
