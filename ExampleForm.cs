using System.Data;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using LabTech.Interfaces;

namespace ScriptAnalyzer.ToolBar
{
    public partial class ScriptAnalyzerForm : ILabTechWindowInformation
    {
        // This is used to determine if the application should prompt about unsaved information before closing
        public bool AttentionRequired
        {
            get { return false; }
        }

        public string WindowName
        {
            get { return "LabTech Script Analyzer"; }
        }
    }

}