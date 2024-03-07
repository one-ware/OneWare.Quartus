using System.Text.RegularExpressions;
using DynamicData;
using OneWare.Essentials.Extensions;
using OneWare.Essentials.Services;
using OneWare.Quartus.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Quartus;

public partial class QuartusToolchain(QuartusService quartusService, ILogger logger) : IFpgaToolchain
{
    public string Name => "Quartus";
    
    
    [GeneratedRegex(@"set_location_assignment\s*PIN_(\w+)\s+-to\s+(\w+)")]
    private static partial Regex AssignmentRegex();
    
    [GeneratedRegex(@"(set_global_assignment\s*-name\s*((VHDL|VERILOG|SYSTEMVERILOG|QIP|QSYS|BDF|AHDL|SMF|TCL_SCRIPT|HEX|MIF)_FILE|FAMILY|DEVICE|TOP_LEVEL_ENTITY)|set_location_assignment)")]
    private static partial Regex RemoveLinesFromQsfRegex();

    public void LoadConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var files = Directory.GetFiles(project.RootFolderPath);
            var qsfPath = files.FirstOrDefault(x => Path.GetExtension(x) == ".qsf");
            if (qsfPath != null)
            {
                var pcf = File.ReadAllText(qsfPath);
                var lines = pcf.Split('\n');
                foreach (var line in lines)
                {
                    var regex = AssignmentRegex();
                    
                    var match = regex.Match(line);
                    if (!match.Success) continue;
                    
                    var pin = match.Groups[1].Value;
                    var node = match.Groups[2].Value;

                    if(!fpga.PinModels.TryGetValue(pin, out var pinModel)) return;
                    if(!fpga.NodeModels.TryGetValue(node, out var nodeModel)) return;
                    
                    fpga.Connect(pinModel, nodeModel);
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
        try
        {
            var topEntity = project.TopEntity?.Header ?? throw new Exception("No TopEntity set!");
            topEntity = Path.GetFileNameWithoutExtension(topEntity);
            
            var qsfPath = Path.Combine(project.RootFolderPath, topEntity + ".qsf");
            
            var qsf = File.Exists(qsfPath) ? File.ReadAllText(qsfPath) : string.Empty;
                
            var lines = qsf.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !RemoveLinesFromQsfRegex().IsMatch(x))
                .ToList();
            
            //Add Family
            lines.Add($"set_global_assignment -name FAMILY \"{fpga.Fpga.Family}\"");
            
            //Add Device
            lines.Add($"set_global_assignment -name DEVICE {fpga.Fpga.Model}");
            
            //Add toplevel
            lines.Add($"set_global_assignment -name TOP_LEVEL_ENTITY {topEntity}");
            
            //Add Files
            foreach (var file in project.Files)
            {
                switch (file.Extension)
                {
                    case ".vhd" or ".vhdl":
                        lines.Add($"set_global_assignment -name VHDL_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".v":
                        lines.Add($"set_global_assignment -name VERILOG_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".sv":
                        lines.Add($"set_global_assignment -name SYSTEMVERILOG_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".qip":
                        lines.Add($"set_global_assignment -name QIP_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".qsys":
                        lines.Add($"set_global_assignment -name QSYS_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".bdf":
                        lines.Add($"set_global_assignment -name BDF_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".ahdl":
                        lines.Add($"set_global_assignment -name AHDL_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".smf":
                        lines.Add($"set_global_assignment -name SMF_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".tcl":
                        lines.Add($"set_global_assignment -name TCL_SCRIPT_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".hex":
                        lines.Add($"set_global_assignment -name HEX_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                    case ".mif":
                        lines.Add($"set_global_assignment -name MIF_FILE {file.RelativePath.ToLinuxPath()}");
                        break;
                }
            }

            foreach (var (key, pinModel) in fpga.PinModels)
            {
                if(pinModel.Connection == null) continue;
                    
                lines.Add($"set_location_assignment PIN_{pinModel.Pin.Name} -to {pinModel.Connection.Node.Name}");
            }
                
            File.WriteAllLines(qsfPath, lines);
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
        }
    }

    public void StartCompile(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        _ = quartusService.SynthAsync(project, fpga);
    }
}