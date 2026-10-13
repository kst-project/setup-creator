using System.Collections.Generic;

namespace SetupStudio.Models
{
    public class InstallerProject
    {
        public List<DesignerElement> Elements { get; set; } = new();
        public List<LogicBlock> LogicBlocks { get; set; } = new();
        public List<InstallerSlide> Slides { get; set; } = new();
        public string SetupName { get; set; } = "MyInstaller";
        public string AppExecutableRelative { get; set; } = "";
        public string AppIconRelative { get; set; } = "";
    }
}
