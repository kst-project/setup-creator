using System.Collections.Generic;
using System.Linq;

namespace SetupStudio.Models
{
    public class InstallerSlide
    {
        public string Name { get; set; } = "Экран 1";
        public List<DesignerElement> Elements { get; set; } = new();
    }
}
