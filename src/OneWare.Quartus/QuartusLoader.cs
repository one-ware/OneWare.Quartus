using AvaloniaEdit.Utils;
using DynamicData;
using Microsoft.Extensions.Logging;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Services;
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

        if (!longTerm)
        {
            var sofFile = Path.GetFileName(FirstFileInPath(project.FullPath, ".sof") ?? "");

            if (string.IsNullOrEmpty(sofFile))
            {
                logger.Error("no .sof found! compile Design first!");
                return;
            }

            var shortTermMode = properties.GetValueOrDefault("QuartusProgrammer_ShortTermMode") ?? "JTAG";
            var shortTermOperation = properties.GetValueOrDefault("QuartusProgrammer_ShortTermOperation") ?? "P";
            var shortTermArgs = properties.GetValueOrDefault("QuartusProgrammer_ShortTerm_Arguments")?.Split(' ') ?? [];

            List<string> pgmArgs = ["-c", cableName, "-m", shortTermMode];
            pgmArgs.AddRange(shortTermArgs);
            pgmArgs.AddRange(["-o", $"p;{sofFile}"]);

            await childProcessService.ExecuteShellAsync("quartus_pgm", pgmArgs,
                project.FullPath, "Running Quartus programmer (Short-Term)...", AppState.Loading, true);
        }
        else
        {
            var longTermFormat = properties.GetValueOrDefault("QuartusProgrammer_LongTerm_Format") ?? "POF";

            string programFile;

            if (longTermFormat.Equals("pof", StringComparison.OrdinalIgnoreCase))
            {
                var pofFile = Path.GetFileName(FirstFileInPath(project.FullPath, ".pof") ?? "");

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
                var sofFile = Path.GetFileName(FirstFileInPath(project.FullPath, ".sof") ?? "");

                if (string.IsNullOrEmpty(sofFile))
                {
                    logger.Error("no .sof found! compile Design first!");
                    return;
                }

                var configurationMode = properties.GetValueOrDefault("QuartusProgrammer_LongTerm_CpfArguments");

                if (configurationMode == null)
                {
                    logger.Error("Cpf Arguments cannot be empty!");
                    return;
                }

                var convertedFilePath = Path.GetFileNameWithoutExtension(sofFile) + $".{longTermFormat.ToLower()}";

                var cpfArgs = configurationMode.Split(' ').ToList();
                cpfArgs.AddRange(["-c", sofFile, convertedFilePath]);

                var result = await childProcessService.ExecuteShellAsync("quartus_cpf", cpfArgs,
                    project.FullPath, $"Converting .sof to .{longTermFormat.ToLower()}...");

                if (!result.success) return;

                programFile = convertedFilePath;
            }

            var longTermMode = properties.GetValueOrDefault("QuartusProgrammer_LongTermMode") ?? "JTAG";
            var longTermOperation = properties.GetValueOrDefault("QuartusProgrammer_LongTermOperation") ?? "P";
            var longTermArgs = properties.GetValueOrDefault("QuartusProgrammer_LongTerm_Arguments")?.Split(' ') ?? [];
            
            List<string> pgmArgs = ["-c", cableName, "-m", longTermMode];
            pgmArgs.AddRange(longTermArgs);
            pgmArgs.AddRange(["-o", $"{longTermOperation};{programFile}"]);

            await childProcessService.ExecuteShellAsync("quartus_pgm", pgmArgs,
                project.FullPath, "Running Quartus programmer (Long-Term)...", AppState.Loading, true);
        }
    }
}