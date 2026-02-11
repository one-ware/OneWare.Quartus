using OneWare.UniversalFpgaProjectSystem.Models;

namespace OneWare.Quartus.Helper;

public static partial class QsfHelper
{
    public static string GetQsfPath(UniversalFpgaProjectRoot project)
    {
        return Path.Combine(project.RootFolderPath, Path.GetFileNameWithoutExtension(project.TopEntity ?? throw new Exception("TopEntity not set!")) + ".qsf");
    }

    public static QsfFile ReadQsf(string path)
    {
        var qsf = File.Exists(path) ? File.ReadAllText(path) : string.Empty;

        return new QsfFile(qsf.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
    
    public static void WriteQsf(string path, QsfFile file)
    {
        File.WriteAllLines(path, file.Lines);
    }
}