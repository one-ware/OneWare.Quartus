using System.Text.RegularExpressions;
using OneWare.Essentials.Extensions;
using OneWare.Essentials.Models;

namespace OneWare.Quartus.Helper;

public partial class QsfFile(string[] lines)
{
    [GeneratedRegex(@"set_location_assignment\s*PIN_(\w+)\s+-to\s+(\w+)")]
    private static partial Regex LocationAssignmentRegex();
    
    [GeneratedRegex(@"set_location_assignment\s")]
    private static partial Regex RemoveLocationAssignmentRegex();
    
    [GeneratedRegex(@"set_global_assignment\s-name\s\w+_FILE\s(.+)")]
    private static partial Regex RemoveFileAssignmentRegex();
    
    public List<string> Lines { get; private set; } = lines.ToList();

    public string? GetGlobalAssignment(string propertyName)
    {
        var regex = new Regex(@"set_global_assignment\s*-name\s" + propertyName + @"\s(.+)");
        foreach (var line in Lines)
        {
            var match = regex.Match(line);
            if (match is { Success: true, Groups.Count: > 1 })
            {
                var value = match.Groups[1].Value;
                if(value.Length > 0 && value[0] == '"' && value[^1] == '"') return value[1..^1];
                return value;
            }
        }
        return null;
    }
    
    public void SetGlobalAssignment(string name, string value)
    {
        var regex = new Regex(@"set_global_assignment\s*-name\s*" + name);
        var line= Lines.FindIndex(x => regex.IsMatch(x));
        var newAssignment = $"set_global_assignment -name {name} \"{value}\"";
        
        if(line != -1)
        {
            Lines[line] = newAssignment;
        }
        else
        {
            Lines.Add(newAssignment);
        }
    }
    
    public void RemoveGlobalAssignment(string name)
    {
        var regex = new Regex(@"set_global_assignment\s*-name\s*" + name);
        Lines = Lines.Where(x => !regex.IsMatch(x)).ToList();
    }

    public IEnumerable<(string,string)> GetLocationAssignments()
    {
        foreach (var line in lines)
        {
            var match = LocationAssignmentRegex().Match(line);
            if (!match.Success) continue;
                    
            var pin = match.Groups[1].Value;
            var node = match.Groups[2].Value;

            yield return (pin, node);
        }
    }
    
    public void AddLocationAssignment(string pin, string node)
    {
        Lines.Add($"set_location_assignment PIN_{pin} -to {node}");
    }
    
    public void RemoveFileAssignments()
    {
        var regex = RemoveFileAssignmentRegex();
        Lines = Lines.Where(x => !regex.IsMatch(x)).ToList();
    }
    
    public void RemoveLocationAssignments()
    {
        var regex = RemoveLocationAssignmentRegex();
        Lines = Lines.Where(x => !regex.IsMatch(x)).ToList();
    }
    
    public void AddFile(IProjectFile file)
    {
        switch (file.Extension)
        {
            case ".vhd" or ".vhdl":
                Lines.Add($"set_global_assignment -name VHDL_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".v":
                Lines.Add($"set_global_assignment -name VERILOG_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".sv":
                Lines.Add($"set_global_assignment -name SYSTEMVERILOG_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".qip":
                Lines.Add($"set_global_assignment -name QIP_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".qsys":
                Lines.Add($"set_global_assignment -name QSYS_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".bdf":
                Lines.Add($"set_global_assignment -name BDF_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".ahdl":
                Lines.Add($"set_global_assignment -name AHDL_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".smf":
                Lines.Add($"set_global_assignment -name SMF_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".tcl":
                Lines.Add($"set_global_assignment -name TCL_SCRIPT_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".hex":
                Lines.Add($"set_global_assignment -name HEX_FILE {file.RelativePath.ToUnixPath()}");
                break;
            case ".mif":
                Lines.Add($"set_global_assignment -name MIF_FILE {file.RelativePath.ToUnixPath()}");
                break;
        }
    }
}