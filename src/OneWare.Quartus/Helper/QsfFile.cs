using System.Text.RegularExpressions;
using OneWare.Essentials.Models;

namespace OneWare.Quartus.Helper;

public partial class QsfFile(string[] lines)
{
    [GeneratedRegex(@"set_location_assignment\s*PIN_(\w+)\s+-to\s+([\w\[\]]+(?:\(n\))?)")]
    private static partial Regex LocationAssignmentRegex();
    
    [GeneratedRegex(@"set_location_assignment\s")]
    private static partial Regex RemoveLocationAssignmentRegex();
    
    [GeneratedRegex(@"set_global_assignment\s-name\s\w+_FILE\s(.+)")]
    private static partial Regex RemoveFileAssignmentRegex();

    /// <summary>
    /// Matches: set_instance_assignment -name NAME "quoted value" -to signal [-entity entity]
    ///      or: set_instance_assignment -name NAME unquoted -to signal [-entity entity]
    /// Groups: 1=name, 2=quoted-value (may be empty), 3=unquoted-value, 4=signal, 5=entity
    /// </summary>
    [GeneratedRegex(@"set_instance_assignment\s+-name\s+(\w+)\s+(?:""([^""]*)""|(\S+))\s+-to\s+([\w\[\]]+(?:\(n\))?)(?:\s+-entity\s+(\w+))?",
        RegexOptions.IgnoreCase)]
    private static partial Regex InstanceAssignmentRegex();
    
    public List<string> Lines { get; private set; } = lines.ToList();

    /// <summary>Normalises path separators to forward-slash for Quartus TCL compatibility.</summary>
    private static string ToUnixPath(string path) => path.Replace('\\', '/');

    public string? GetQsfProperty(string propertyName)
    {
        var regex = new Regex(propertyName + @"\s(.+)");
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
    
    public void SetQsfProperty(string propertyName, string value)
    {
        var regex = new Regex(propertyName + @"\s(.+)");
        var line= Lines.FindIndex(x => regex.IsMatch(x));
        var newAssignment = $"{propertyName} {value}";
        
        if(line != -1)
        {
            Lines[line] = newAssignment;
        }
        else
        {
            Lines.Add(newAssignment);
        }
    }
    
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
        foreach (var line in Lines)
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

    // ── Instance assignments (set_instance_assignment) ────────────────────────

    /// <summary>
    /// Returns all <c>set_instance_assignment</c> lines parsed as
    /// (Name, Value, Signal, Entity?).
    /// </summary>
    public IEnumerable<(string Name, string Value, string Signal, string? Entity)> GetInstanceAssignments()
    {
        foreach (var line in Lines)
        {
            var match = InstanceAssignmentRegex().Match(line);
            if (!match.Success) continue;

            var name = match.Groups[1].Value;
            // Group 2 = quoted value, group 3 = unquoted value
            var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
            var signal = match.Groups[4].Value;
            var entity = match.Groups[5].Success ? match.Groups[5].Value : (string?)null;

            yield return (name, value, signal, entity);
        }
    }

    /// <summary>
    /// Writes a <c>set_instance_assignment</c> line.
    /// Values that contain spaces are automatically quoted.
    /// </summary>
    public void AddInstanceAssignment(string name, string value, string signal, string? entity = null)
    {
        var quotedValue = value.Contains(' ') ? $"\"{value}\"" : value;
        var line = $"set_instance_assignment -name {name} {quotedValue} -to {signal}";
        if (entity != null) line += $" -entity {entity}";
        Lines.Add(line);
    }

    /// <summary>
    /// Removes all <c>set_instance_assignment</c> lines whose <c>-name</c> matches
    /// <paramref name="name"/> (case-insensitive).
    /// </summary>
    public void RemoveInstanceAssignmentsByName(string name)
    {
        // Match the property name as a whole word after -name
        var regex = new Regex(
            @$"set_instance_assignment\s.*-name\s+{Regex.Escape(name)}(\s|$)",
            RegexOptions.IgnoreCase);
        Lines = Lines.Where(x => !regex.IsMatch(x)).ToList();
    }
    
    public void AddFile(string relativePath)
    {
        var unixPath = ToUnixPath(relativePath);
        switch (Path.GetExtension(relativePath).ToLowerInvariant())
        {
            case ".vhd" or ".vhdl":
                Lines.Add($"set_global_assignment -name VHDL_FILE {unixPath}");
                break;
            case ".v":
                Lines.Add($"set_global_assignment -name VERILOG_FILE {unixPath}");
                break;
            case ".sv":
                Lines.Add($"set_global_assignment -name SYSTEMVERILOG_FILE {unixPath}");
                break;
            case ".vt":
                Lines.Add($"set_global_assignment -name VERILOG_TEST_BENCH_FILE {unixPath}");
                break;
            case ".vht":
                Lines.Add($"set_global_assignment -name VHDL_TEST_BENCH_FILE {unixPath}");
                break;
            case ".qip":
                Lines.Add($"set_global_assignment -name QIP_FILE {unixPath}");
                break;
            case ".ip":
                Lines.Add($"set_global_assignment -name IP_FILE {unixPath}");
                break;
            case ".qsys":
                Lines.Add($"set_global_assignment -name QSYS_FILE {unixPath}");
                break;
            case ".bdf":
                Lines.Add($"set_global_assignment -name BDF_FILE {unixPath}");
                break;
            case ".ahdl":
                Lines.Add($"set_global_assignment -name AHDL_FILE {unixPath}");
                break;
            case ".sdc":
                Lines.Add($"set_global_assignment -name SDC_FILE {unixPath}");
                break;
            case ".stp":
                Lines.Add($"set_global_assignment -name SIGNALTAP_FILE {unixPath}");
                break;
            case ".edf" or ".edif":
                Lines.Add($"set_global_assignment -name EDIF_FILE {unixPath}");
                break;
            case ".vqm":
                Lines.Add($"set_global_assignment -name VQM_FILE {unixPath}");
                break;
            case ".smf":
                Lines.Add($"set_global_assignment -name SMF_FILE {unixPath}");
                break;
            case ".tcl":
                Lines.Add($"set_global_assignment -name TCL_SCRIPT_FILE {unixPath}");
                break;
            case ".hex":
                Lines.Add($"set_global_assignment -name HEX_FILE {unixPath}");
                break;
            case ".mif":
                Lines.Add($"set_global_assignment -name MIF_FILE {unixPath}");
                break;
        }
    }
}