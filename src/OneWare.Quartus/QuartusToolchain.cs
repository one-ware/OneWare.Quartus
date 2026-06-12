using Microsoft.Extensions.Logging;
using OneWare.Essentials.Services;
using OneWare.Quartus.Helper;
using OneWare.Quartus.Services;
using OneWare.UniversalFpgaProjectSystem.Fpga;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;
using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.Quartus;

public class QuartusToolchain(QuartusService quartusService, ILogger logger) : IFpgaToolchain
{
    public const string ToolchainId = "quartus";

    public string Id => ToolchainId;

    public string Name => "Quartus";

    /// <summary>
    /// Per-pin properties exposed in the pin planner for every Quartus project.
    /// </summary>
    public IEnumerable<PinPropertyDefinition> PinProperties =>
    [
        new PinPropertyDefinition(
            "IO_STANDARD",
            "IO Standard",
            PinPropertyType.ComboBox,
            [
                "",
                "1.0 V",
                "1.2 V",
                "1.5 V",
                "1.8 V",
                "2.5 V",
                "3.3-V LVCMOS",
                "LVTTL",
                "LVCMOS",
                "LVDS",
                "LVDS_E_3R",
                "True Differential Signaling",
                "High Speed Differential I/O",
                "Differential 1.8-V SSTL Class I",
                "Differential 1.8-V SSTL Class II"
            ]),
        new PinPropertyDefinition(
            "WEAK_PULL_UP_RESISTOR",
            "Weak Pull-Up",
            PinPropertyType.ComboBox,
            ["", "ON", "OFF"])
    ];

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

            // Step 1: load pin ↔ node location assignments
            foreach (var (pin, node) in qsf.GetLocationAssignments())
            {
                if (!fpga.PinModels.TryGetValue(pin, out var pinModel)) continue;
                if (!fpga.NodeModels.TryGetValue(node, out var nodeModel)) continue;

                fpga.Connect(pinModel, nodeModel);
            }

            // Step 2: load per-pin instance assignments (IO_STANDARD, WEAK_PULL_UP_RESISTOR, …)
            // Keyed by node name → list of (propertyName, value)
            var instanceAssignments = new Dictionary<string, List<(string Name, string Value)>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var (name, value, signal, _) in qsf.GetInstanceAssignments())
            {
                if (!instanceAssignments.TryGetValue(signal, out var list))
                    instanceAssignments[signal] = list = new List<(string, string)>();
                list.Add((name, value));
            }

            // Apply instance assignments to the corresponding pin model (via connected node)
            foreach (var nodeModel in fpga.NodeModels.Values)
            {
                if (nodeModel.ConnectedPin == null) continue;
                if (!instanceAssignments.TryGetValue(nodeModel.Node.Name, out var assignments)) continue;

                foreach (var (name, value) in assignments)
                    nodeModel.ConnectedPin.SetPinPropertyValue(name, value);
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
            var topEntity = project.TopEntity != null
                ? Path.GetFileNameWithoutExtension(project.TopEntity)
                : null;

            var qsfPath = QsfHelper.GetQsfPath(project);
            var qsf = QsfHelper.ReadQsf(qsfPath);

            // ── Location assignments ──────────────────────────────────────────
            qsf.RemoveLocationAssignments();
            foreach (var (_, pinModel) in fpga.PinModels.Where(x => x.Value.ConnectedNode != null))
                qsf.AddLocationAssignment(pinModel.Pin.Name, pinModel.ConnectedNode!.Node.Name);

            // ── Instance assignments (per-pin properties) ─────────────────────
            // Prefer properties declared in hardware JSON; fall back to toolchain's own list
            var effectiveProperties = fpga.Fpga.AllowedPinProperties.Count > 0
                ? (IEnumerable<PinPropertyDefinition>)fpga.Fpga.AllowedPinProperties
                : PinProperties;

            // Remove old managed assignments before re-adding
            foreach (var propDef in effectiveProperties)
                qsf.RemoveInstanceAssignmentsByName(propDef.Key);

            foreach (var (_, pinModel) in fpga.PinModels.Where(x => x.Value.ConnectedNode != null))
            {
                var nodeName = pinModel.ConnectedNode!.Node.Name;
                foreach (var propDef in effectiveProperties)
                {
                    var value = pinModel.GetPinPropertyValue(propDef.Key);
                    if (!string.IsNullOrEmpty(value))
                        qsf.AddInstanceAssignment(propDef.Key, value, nodeName, topEntity);
                }
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
            if(project.TopEntity == null) throw new Exception("No TopEntity set!");

            var topEntity = Path.GetFileNameWithoutExtension(project.TopEntity);
            
            var properties = FpgaSettingsParser.LoadSettings(project, fpga.Fpga.Name);

            var qsfPath = QsfHelper.GetQsfPath(project);
            var qsf = QsfHelper.ReadQsf(qsfPath);

            var family = properties.GetValueOrDefault("quartusToolchainFamily") ?? throw new Exception("No Family set!");
            var device = properties.GetValueOrDefault("quartusToolchainDevice") ?? throw new Exception("No Device set!");
            
            //Add output path
            qsf.SetGlobalAssignment("PROJECT_OUTPUT_DIRECTORY", "output_files");
            
            //Add Family
            qsf.SetGlobalAssignment("FAMILY", family);
            
            //Add Device
            qsf.SetGlobalAssignment("DEVICE", device);
            
            //Add toplevel
            qsf.SetGlobalAssignment("TOP_LEVEL_ENTITY", topEntity);
            
            //Add Files
            qsf.RemoveFileAssignments();
            
            var includedFiles = project.GetFiles()
                .Where(x => !project.IsCompileExcluded(x))
                .Where(x => !project.IsTestBench(x));
            
            foreach (var file in includedFiles)
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