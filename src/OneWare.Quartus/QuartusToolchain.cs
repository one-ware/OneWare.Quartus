using OneWare.Essentials.Services;
using OneWare.Quartus.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Quartus;

public class QuartusToolchain(QuartusService quartusService, ILogger logger) : IFpgaToolchain
{
    public string Name => "Quartus";

    public void LoadConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var files = Directory.GetFiles(project.RootFolderPath);
            var pcfPath = files.FirstOrDefault(x => Path.GetExtension(x) == ".qsf");
            if (pcfPath != null)
            {
                var pcf = File.ReadAllText(pcfPath);
                var lines = pcf.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("set_io"))
                    {
                        var parts = trimmedLine.Split(' ');
                        if (parts.Length != 3)
                        {
                            logger.Warning("PCF Line invalid: " + trimmedLine);
                            continue;
                        }

                        var signal = parts[1];
                        var pin = parts[2];

                        if (fpga.PinModels.TryGetValue(pin, out var pinModel) && fpga.NodeModels.TryGetValue(signal, out var signalModel))
                        {
                            fpga.Connect(pinModel, signalModel);
                        } 
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
        }
    }

    public void SaveConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        throw new NotImplementedException();
    }

    public void StartCompile(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        throw new NotImplementedException();
    }
}