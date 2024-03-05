using Microsoft.Extensions.Logging;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using ILogger = OneWare.Essentials.Services.ILogger;

namespace OneWare.Quartus;

public class QuartusLoader(IChildProcessService childProcessService, ILogger logger) : IFpgaLoader
{
    public string Name => "Quartus";

    private string? FirstFileInPath(string path, string extension)
    {
        try
        {
            return Directory
                .GetFiles(path)
                .FirstOrDefault(x => Path.GetExtension(x).Equals(extension, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e)
        { 
            logger.Error(e.Message, e);
            return null;
        }
    }
    
    public async Task DownloadAsync(UniversalFpgaProjectRoot project)
    {
        var fpga = project.Properties["Fpga"];
        if (fpga == null) return;
        var fpgaModel = fpga.ToString();
        
        var cableName = "Auto";
        var sofFile = Path.GetFileName(FirstFileInPath(project.FullPath, ".sof") ?? "");
        
        if (string.IsNullOrEmpty(sofFile))
        {
            logger.Error("no .sof found! compile Design first!");
            return;
        }
        
        await childProcessService.ExecuteShellAsync("quartus_pgm", $"-c {cableName} -m JTAG -o P;{sofFile}",
            project.FullPath, "Running OpenFPGALoader");
    }
}