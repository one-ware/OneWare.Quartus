using OneWare.Essentials.Services;
using OneWare.Quartus.Helper;
using OneWare.Quartus.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Quartus;

public class QuartusToolchain(QuartusService quartusService, ILogger logger) : IFpgaToolchain
{

    public string Name => "Quartus";

    public void OnProjectCreated(UniversalFpgaProjectRoot project)
    {
        //TODO Add gitignore defaults
    }

    public void LoadConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var qsfPath = QsfHelper.GetQsfPath(project);
            var qsf = QsfHelper.ReadQsf(qsfPath);
            
            foreach (var (pin, node) in qsf.GetLocationAssignments())
            {

                if(!fpga.PinModels.TryGetValue(pin, out var pinModel)) return;
                if(!fpga.NodeModels.TryGetValue(node, out var nodeModel)) return;
                    
                fpga.Connect(pinModel, nodeModel);
            }
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
        }
    }

    public void SaveConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var qsfPath = QsfHelper.GetQsfPath(project);
            var qsf = QsfHelper.ReadQsf(qsfPath);

            //Add Connections
            qsf.RemoveLocationAssignments();
            foreach (var (_, pinModel) in fpga.PinModels.Where(x => x.Value.Connection != null))
            {
                qsf.AddLocationAssignment(pinModel.Pin.Name, pinModel.Connection!.Node.Name);
            }
                
            QsfHelper.WriteQsf(qsfPath, qsf);
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
        }
    }

    public Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var topEntity = project.TopEntity?.Header ?? throw new Exception("No TopEntity set!");
            topEntity = Path.GetFileNameWithoutExtension(topEntity);
            
            var properties = FpgaSettingsParser.LoadSettings(project, fpga.Fpga.Name);

            var qsfPath = QsfHelper.GetQsfPath(project);
            var qsf = QsfHelper.ReadQsf(qsfPath);

            var family = properties.GetValueOrDefault("QuartusToolchain_Family") ?? throw new Exception("No Family set!");
            var device = properties.GetValueOrDefault("QuartusToolchain_Device") ?? throw new Exception("No Device set!");
            
            //Add Family
            qsf.SetGlobalAssignment("FAMILY", family);
            
            //Add Device
            qsf.SetGlobalAssignment("DEVICE", device);
            
            //Add toplevel
            qsf.SetGlobalAssignment("TOP_LEVEL_ENTITY", topEntity);
            
            //Add Files
            qsf.RemoveFileAssignments();
            foreach (var file in project.Files)
            {
                qsf.AddFile(file);
            }
            
            QsfHelper.WriteQsf(qsfPath, qsf);
            
            return quartusService.CompileAsync(project, fpga);
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
            return Task.FromResult(false);
        }
    }
}