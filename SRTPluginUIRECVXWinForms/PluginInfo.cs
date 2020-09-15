using SRTPluginBase;
using System;

namespace SRTPluginUIRECVXWinForms
{
    public class PluginInfo : IPluginInfo
    {
        public string Name => "WinForms UI (Resident Evil: Code: Veronica)";

        public string Description => "A WinForms-based User Interface for displaying Resident Evil: Code: Veronica game memory values.";

        public string Author => "Kapdap";

        public Uri MoreInfoURL => new Uri("https://github.com/kapdap/re-cvx-srt-ui-winforms");

        public int VersionMajor => assemblyFileVersion.ProductMajorPart;

        public int VersionMinor => assemblyFileVersion.ProductMinorPart;

        public int VersionBuild => assemblyFileVersion.ProductBuildPart;

        public int VersionRevision => assemblyFileVersion.ProductPrivatePart;

        private System.Diagnostics.FileVersionInfo assemblyFileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
    }
}