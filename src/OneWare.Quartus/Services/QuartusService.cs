using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;

namespace OneWare.Quartus.Services;

public class QuartusService(IChildProcessService childProcessService, ILogger logger)
{
    public async Task SynthAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        if (project.TopEntity == null)
        {
            logger.Error("No TopEntity set");
            return;
        }
        
        await childProcessService.ExecuteShellAsync("quartus_sh",
            $"--flow compile {Path.GetFileNameWithoutExtension(project.TopEntity.Header)}",
            project.FullPath, "Starting Quartus Prime Shell...");
    }
}