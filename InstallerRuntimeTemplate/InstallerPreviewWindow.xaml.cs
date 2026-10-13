using SetupStudio.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace SetupStudio
{
    public partial class InstallerPreviewWindow : Window
    {
        private readonly List<InstallerSlide> _slides;
        private readonly List<LogicBlock> _logicBlocks;
        private int _index;
        private readonly List<TextBox> _installPathInputs = new();
        private readonly System.Collections.Generic.HashSet<int> _activeScreenEvents = new();
        private readonly bool _installerMode;
        private readonly string? _payloadDirectory;
        private readonly string _appName;
        private readonly string? _appIconPath;
        private readonly string? _appExecutableRelative;
        private string _selectedInstallDirectory = "";

        public InstallerPreviewWindow(IEnumerable<InstallerSlide> slides, IEnumerable<LogicBlock>? logicBlocks = null, bool installerMode = false, string? payloadDirectory = null, string appName = "Setup", string? appIconPath = null, string? appExecutableRelative = null)
        {
            InitializeComponent();
            _slides = slides.ToList();
            _logicBlocks = logicBlocks?.ToList() ?? new List<LogicBlock>();
            _installerMode = installerMode; _payloadDirectory = payloadDirectory; _appName = string.IsNullOrWhiteSpace(appName) ? "Setup" : appName; _appIconPath = appIconPath; _appExecutableRelative = appExecutableRelative;
            Title = _installerMode ? _appName : "Предпросмотр установщика";
            if (!string.IsNullOrWhiteSpace(_appIconPath) && File.Exists(_appIconPath)) { try { Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(Path.GetFullPath(_appIconPath))); } catch { } }
            if (_slides.Count == 0) _slides.Add(new InstallerSlide { Name = "Экран 1" });
            RenderSlide();
            Loaded += async (_, _) => await RunScreenOpenedEventAsync(_index + 1);
        }

        private async Task RunScreenOpenedEventAsync(int screenNumber)
        {
            if (!_activeScreenEvents.Add(screenNumber)) return;
            try
            {
                var eventBlock = _logicBlocks.FirstOrDefault(b => b.Type == "SlideOpened" && b.ScreenNumber == screenNumber);
                if (eventBlock == null) return;
                await LogicRunner.RunAsync(_logicBlocks, eventBlock.Id, message => { }, this, CurrentInstallDirectory(), OpenPageByName);
            }
            finally { _activeScreenEvents.Remove(screenNumber); }
        }

        private static Brush BrushFrom(string value)
        {
            try { return (Brush)new BrushConverter().ConvertFromString(value)!; }
            catch { return Brushes.Transparent; }
        }

        private void RenderSlide()
        {
            PreviewCanvas.Children.Clear();
            _installPathInputs.Clear();
            var slide = _slides[_index];
            SlideTitle.Text = $"{slide.Name} ({_index + 1}/{_slides.Count})";
            foreach (var el in slide.Elements)
            {
                FrameworkElement child;
                if (el.Type == DesignerElementType.Video)
                {
                    if (!string.IsNullOrWhiteSpace(el.VideoPath) && File.Exists(el.VideoPath))
                    {
                        var media = new MediaElement { Source = new Uri(Path.GetFullPath(el.VideoPath)), LoadedBehavior = MediaState.Manual, UnloadedBehavior = MediaState.Stop, Stretch = Stretch.Uniform, Volume = 0.25 };
                        media.Loaded += (_, _) => { try { media.Play(); } catch { } };
                        child = new Border { Background = BrushFrom(el.Background), BorderBrush = BrushFrom(el.BorderColor), BorderThickness = new Thickness(el.BorderThickness), CornerRadius = new CornerRadius(el.CornerRadius), ClipToBounds = true, Child = media };
                    }
                    else child = new Border { Background = BrushFrom(el.Background), BorderBrush = BrushFrom(el.BorderColor), BorderThickness = new Thickness(el.BorderThickness), CornerRadius = new CornerRadius(el.CornerRadius), Child = new TextBlock { Text = "Видео MP4 не выбрано", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center } };
                }
                else if (el.Type == DesignerElementType.Image)
                {
                    if (!string.IsNullOrWhiteSpace(el.ImagePath) && File.Exists(el.ImagePath))
                    {
                        try { var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.UriSource = new Uri(Path.GetFullPath(el.ImagePath)); bitmap.EndInit(); bitmap.Freeze(); child = new Image { Source = bitmap, Stretch = Stretch.Uniform }; }
                        catch { child = new TextBlock { Text = "Изображение не найдено" }; }
                    }
                    else child = new Border { Background = BrushFrom(el.Background), BorderBrush = BrushFrom(el.BorderColor), BorderThickness = new Thickness(el.BorderThickness), CornerRadius = new CornerRadius(el.CornerRadius), Child = new TextBlock { Text = "Изображение не выбрано", Foreground = BrushFrom(el.Foreground), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
                }
                else if (el.Type == DesignerElementType.Divider) child = new Border { Background = BrushFrom(el.Foreground), CornerRadius = new CornerRadius(el.CornerRadius) };
                else if (el.Type == DesignerElementType.Button) child = new Button { Content = el.Text, Background = BrushFrom(el.Background), Foreground = BrushFrom(el.Foreground), FontSize = el.FontSize, BorderThickness = new Thickness(el.BorderThickness), BorderBrush = BrushFrom(el.BorderColor), Padding = new Thickness(8), IsEnabled = true, Template = CreateButtonTemplate(el.CornerRadius) };
                else if (el.Type == DesignerElementType.InstallPath)
                {
                    var tb = new TextBox { Text = string.IsNullOrWhiteSpace(_selectedInstallDirectory) ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\" + _appName : _selectedInstallDirectory, Padding = new Thickness(7), BorderBrush = BrushFrom(el.BorderColor), Background = BrushFrom(el.Background), Foreground = BrushFrom(el.Foreground), BorderThickness = new Thickness(el.BorderThickness) };
                    _installPathInputs.Add(tb);
                    if (string.IsNullOrWhiteSpace(_selectedInstallDirectory)) _selectedInstallDirectory = tb.Text;
                    tb.TextChanged += (_, _) => _selectedInstallDirectory = tb.Text;
                    var pathRow = new DockPanel();
                    var browse = new Button { Content = "…", Width = 34, Padding = new Thickness(2), Margin = new Thickness(4, 0, 0, 0), ToolTip = "Выбрать папку установки" };
                    browse.Click += (_, _) => { var dialog = new OpenFolderDialog { Title = "Выбери папку установки", Multiselect = false }; if (dialog.ShowDialog(this) == true) tb.Text = dialog.FolderName; };
                    DockPanel.SetDock(browse, Dock.Right); pathRow.Children.Add(browse); pathRow.Children.Add(tb);
                    var pathPanel = new StackPanel();
                    pathPanel.Children.Add(new TextBlock { Text = el.Text, Foreground = BrushFrom(el.Foreground), FontSize = 12, Margin = new Thickness(0,0,0,4) });
                    pathPanel.Children.Add(pathRow); child = pathPanel;
                }
                else child = new TextBlock { Text = el.Text, FontSize = el.FontSize, FontWeight = el.Type == DesignerElementType.Heading ? FontWeights.Bold : FontWeights.Normal, Foreground = BrushFrom(el.Foreground), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                child.Width = Math.Max(1, el.Width); child.Height = Math.Max(1, el.Height);
                if (el.Type is DesignerElementType.Text or DesignerElementType.Heading)
                    child = new Border { Background = BrushFrom(el.Background), BorderBrush = BrushFrom(el.BorderColor), BorderThickness = new Thickness(el.BorderThickness), CornerRadius = new CornerRadius(el.CornerRadius), Padding = new Thickness(2), Child = child, Width = Math.Max(1, el.Width), Height = Math.Max(1, el.Height) };
                else if (el.Type is DesignerElementType.Image or DesignerElementType.Video)
                    child = new Border { Background = BrushFrom(el.Background), BorderBrush = BrushFrom(el.BorderColor), BorderThickness = new Thickness(el.BorderThickness), CornerRadius = new CornerRadius(el.CornerRadius), ClipToBounds = true, Child = child, Width = Math.Max(1, el.Width), Height = Math.Max(1, el.Height) };
                if (el.Type != DesignerElementType.Image && el.Type != DesignerElementType.Video && el.Type != DesignerElementType.InstallPath && el.Type != DesignerElementType.Divider && el.Type != DesignerElementType.Button && child is TextBlock text)
                    text.TextWrapping = TextWrapping.Wrap;
                if (el.Type == DesignerElementType.Button && child is Button previewButton)
                {
                    var buttonEvent = _logicBlocks.FirstOrDefault(b => b.Type == "ButtonClick" && b.EventButtonId == el.Id);
                    if (buttonEvent != null) previewButton.Click += async (_, _) => await LogicRunner.RunAsync(_logicBlocks, buttonEvent.Id, _ => { }, this, CurrentInstallDirectory(), OpenPageByName);
                }
                PreviewCanvas.Children.Add(child); Canvas.SetLeft(child, el.X); Canvas.SetTop(child, el.Y);
            }
            BackButton.IsEnabled = _index > 0;
            NextButton.Content = _index == _slides.Count - 1 ? "Завершить ✓" : "Далее →";
        }

        private static ControlTemplate CreateButtonTemplate(double radius)
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new System.Windows.TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new System.Windows.TemplateBindingExtension(Control.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(Math.Max(0, radius)));
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            return new ControlTemplate(typeof(Button)) { VisualTree = border };
        }

        private async void Back_Click(object sender, RoutedEventArgs e) { if (_index > 0) { _index--; RenderSlide(); await RunScreenOpenedEventAsync(_index + 1); } }
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_index < _slides.Count - 1) { _index++; RenderSlide(); await RunScreenOpenedEventAsync(_index + 1); return; }
            if (_installerMode) { CompleteInstallation(); return; }
            Close();
        }

        private void OpenPageByName(string pageName)
        {
            int target = _slides.FindIndex(slide => string.Equals(slide.Name, pageName, StringComparison.OrdinalIgnoreCase));
            if (target < 0 || target == _index) return;
            _index = target; RenderSlide();
            _ = RunScreenOpenedEventAsync(_index + 1);
        }

        private string CurrentInstallDirectory() => _installPathInputs.FirstOrDefault()?.Text ?? _selectedInstallDirectory;

        private void CompleteInstallation()
        {
            try
            {
                string destination = CurrentInstallDirectory().Trim();
                if (string.IsNullOrWhiteSpace(destination)) destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), _appName);
                destination = Environment.ExpandEnvironmentVariables(destination);
                if (_payloadDirectory == null || !Directory.Exists(_payloadDirectory)) throw new DirectoryNotFoundException("В установщике отсутствуют файлы программы.");
                Directory.CreateDirectory(destination);
                foreach (string file in Directory.GetFiles(_payloadDirectory, "*", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(_payloadDirectory, file);
                    string target = Path.Combine(destination, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target, true);
                }
                string installedIcon = "";
                if (!string.IsNullOrWhiteSpace(_appIconPath) && File.Exists(_appIconPath))
                {
                    installedIcon = Path.Combine(destination, "app-icon.ico");
                    File.Copy(_appIconPath, installedIcon, true);
                }
                if (!string.IsNullOrWhiteSpace(_appExecutableRelative))
                {
                    string targetExe = Path.GetFullPath(Path.Combine(destination, _appExecutableRelative));
                    if (File.Exists(targetExe))
                    {
                        TryCreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), _appName + ".lnk"), targetExe, destination, installedIcon);
                        string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", _appName);
                        Directory.CreateDirectory(startMenu);
                        TryCreateShortcut(Path.Combine(startMenu, _appName + ".lnk"), targetExe, destination, installedIcon);
                    }
                }
                MessageBox.Show(this, "Спасибо за использование KST Setup Creator!

Программа установлена в:
" + destination, "Установка завершена", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex) { MessageBox.Show(this, "Не удалось завершить установку.

" + ex.Message, "Ошибка установки", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private static void TryCreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDirectory;
                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath)) shortcut.IconLocation = iconPath;
                shortcut.Save();
            }
            catch { }
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
