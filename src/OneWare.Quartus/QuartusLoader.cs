using AvaloniaEdit.Utils;
using DynamicData;
using Microsoft.Extensions.Logging;
using OneWare.Essentials.Behaviors;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Services;
using OneWare.Quartus.Helper;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;
using OneWare.UniversalFpgaProjectSystem.Services;
using ILogger = OneWare.Essentials.Services.ILogger;

namespace OneWare.Quartus;

public class QuartusLoader(IChildProcessService childProcessService, ISettingsService settingsService, ILogger logger)
    : IFpgaLoader
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
        var fpga = project.GetProjectProperty("Fpga");
        if (fpga == null) return;

        var longTerm = settingsService.GetSettingValue<bool>("UniversalFpgaProjectSystem_LongTermProgramming");

        var properties = FpgaSettingsParser.LoadSettings(project, fpga);

        var cableSetting = "Auto";
        var cableName = cableSetting == "Auto"
            ? "1"
            : "\"" + cableSetting + "\"";

        var qsfPath = QsfHelper.GetQsfPath(project);
        var qsf = QsfHelper.ReadQsf(qsfPath);

        var outputDirRelative = qsf.GetGlobalAssignment("PROJECT_OUTPUT_DIRECTORY");
        
        var outputDir = project.FullPath;

        if (outputDirRelative != null)
        {
            outputDir = Path.Combine(outputDir, outputDirRelative);
        }
        
        if (!longTerm)
        {
            var sofFile = Path.GetFileName(FirstFileInPath(outputDir,".sof"));

            if (string.IsNullOrEmpty(sofFile))
            {
                logger.Error("no .sof found! compile Design first!");
                return;
            }

            var shortTermMode = properties.GetValueOrDefault("quartusProgrammerShortTermMode") ?? "JTAG";
            var shortTermOperation = properties.GetValueOrDefault("quartusProgrammerShortTermOperation") ?? "P";
            var shortTermArgs = properties.GetValueOrDefault("quartusProgrammerShortTermArguments")?.Split(' ') ?? [];

            List<string> pgmArgs = ["-c", cableName, "-m", shortTermMode];
            pgmArgs.AddRange(shortTermArgs);
            pgmArgs.AddRange(["-o", $"{shortTermOperation};{sofFile}"]);

            await childProcessService.ExecuteShellAsync("quartus_pgm", pgmArgs,
                outputDir, "Running Quartus programmer (Short-Term)...", AppState.Loading, true);
        }
        else
        {
            var longTermFormat = properties.GetValueOrDefault("quartusProgrammerLongTermFormat") ?? "POF";

            string programFile;

            if (longTermFormat.Equals("pof", StringComparison.OrdinalIgnoreCase))
            {
                var pofFile = Path.GetFileName(FirstFileInPath(outputDir, ".pof") ?? "");

                if (string.IsNullOrEmpty(pofFile))
                {
                    logger.Error("no .pof found! compile Design first!");
                    return;
                }

                programFile = pofFile;
            }
            else
            {
                //Use CPF to convert SOF in given format
                var sofFile = Path.GetFileName(FirstFileInPath(outputDir, ".sof") ?? "");

                if (string.IsNullOrEmpty(sofFile))
                {
                    logger.Error("no .sof found! compile Design first!");
                    return;
                }

                var configurationMode = properties.GetValueOrDefault("quartusProgrammerLongTermCpfArguments");

                if (configurationMode == null)
                {
                    logger.Error("Cpf Arguments cannot be empty!");
                    return;
                }

                var convertedFilePath = Path.GetFileNameWithoutExtension(sofFile) + $".{longTermFormat.ToLower()}";

                var cpfArgs = configurationMode.Split(' ').ToList();
                cpfArgs.AddRange(["-c", sofFile, convertedFilePath]);

                var result = await childProcessService.ExecuteShellAsync("quartus_cpf", cpfArgs,
                    outputDir, $"Converting .sof to .{longTermFormat.ToLower()}...");

                if (!result.success) return;

                programFile = convertedFilePath;
            }

            var longTermMode = properties.GetValueOrDefault("quartusProgrammerLongTermMode") ?? "JTAG";
            var longTermOperation = properties.GetValueOrDefault("quartusProgrammerLongTermOperation") ?? "P";
            var longTermArgs = properties.GetValueOrDefault("quartusProgrammerLongTermArguments")?.Split(' ') ?? [];
            
            List<string> pgmArgs = ["-c", cableName, "-m", longTermMode];
            pgmArgs.AddRange(longTermArgs);
            pgmArgs.AddRange(["-o", $"{longTermOperation};{programFile}"]);

            await childProcessService.ExecuteShellAsync("quartus_pgm", pgmArgs,
                outputDir, "Running Quartus programmer (Long-Term)...", AppState.Loading, true);
        }
    }
}