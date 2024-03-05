﻿using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;

namespace OneWare.Quartus.Services;

public class QuartusService
{
    private readonly IChildProcessService _childProcessService;

    public QuartusService(IChildProcessService childProcessService)
    {
        _childProcessService = childProcessService;
    }

    public async Task SynthAsync(UniversalFpgaProjectRoot project)
    {
        var fpga = "ice40"; // project.Properties["Fpga"];
        var top = project.Properties["TopEntity"];

        var verilogFiles = string.Join(" ", project.Files.Where(x => x.Extension == ".v").Select(x => x.RelativePath));
        var yosysFlags = string.Empty;

        var buildDir = Path.Combine(project.FullPath, "build");
        Directory.CreateDirectory(buildDir);

        await _childProcessService.ExecuteShellAsync("yosys",
            $"-q -p \"synth_{fpga} -json {Path.Combine(buildDir, "synth.json")}\" {yosysFlags}{verilogFiles}",
            project.FullPath, "Running Yosys...");

        var nextPnrFlags = "--freq 12 --up5k --package sg48";

        await _childProcessService.ExecuteShellAsync($"nextpnr-{fpga}",
            $"--json ./build/synth.json --pcf project.pcf --asc ./build/nextpnr.asc {nextPnrFlags}",
            project.FullPath, "Running NextPnr...");

        await _childProcessService.ExecuteShellAsync($"icepack",
            $"./build/nextpnr.asc ./build/pack.bin",
            project.FullPath, "Running IcePack...");
    }
}