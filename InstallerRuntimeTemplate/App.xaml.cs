using SetupStudio.Models;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace SetupStudio
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                string runtimeRoot = Path.Combine(Path.GetTempPath(), "KSTSetupCreator", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(runtimeRoot);
                Assembly assembly = Assembly.GetExecutingAssembly();
                foreach (string resourceName in assembly.GetManifestResourceNames().Where(n => n.StartsWith("Payload/", StringComparison.OrdinalIgnoreCase)))
                {
                    string relative = resourceName.Substring("Payload/".Length).Replace('/', Path.DirectorySeparatorChar);
                    string outputPath = Path.GetFullPath(Path.Combine(runtimeRoot, "Payload", relative));
                    if (!outputPath.StartsWith(Path.GetFullPath(Path.Combine(runtimeRoot, "Payload")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    using Stream? input = assembly.GetManifestResourceStream(resourceName);
                    if (input == null) continue;
                    using var output = File.Create(outputPath); input.CopyTo(output);
                }
                using Stream projectStream = assembly.GetManifestResourceStream("InstallerProject.json") ?? throw new InvalidDataException("В установщике отсутствует описание экранов.");
                InstallerProject project;
                using (var reader = new StreamReader(projectStream)) project = JsonSerializer.Deserialize<InstallerProject>(reader.ReadToEnd()) ?? throw new InvalidDataException("Не удалось прочитать проект установщика.");
                string payloadRoot = Path.Combine(runtimeRoot, "Payload");
                foreach (var slide in project.Slides)
                foreach (var element in slide.Elements)
                {
                    element.ImagePath = Resolve(element.ImagePath, payloadRoot);
                    element.VideoPath = Resolve(element.VideoPath, payloadRoot);
                }
                foreach (var block in project.LogicBlocks)
                {
                    block.ProgramPath = Resolve(block.ProgramPath, payloadRoot);
                }
                string appIcon = Resolve(project.AppIconRelative, payloadRoot);
                var window = new InstallerPreviewWindow(project.Slides, project.LogicBlocks, true, Path.Combine(payloadRoot, "Program"), project.SetupName, appIcon, project.AppExecutableRelative)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                MainWindow = window;
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось запустить установщик.\n\n" + ex.Message, "Ошибка установщика", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private static string Resolve(string? path, string payloadRoot)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            if (!path.StartsWith("Payload/", StringComparison.OrdinalIgnoreCase)) return path;
            return Path.Combine(payloadRoot, path.Substring("Payload/".Length).Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
