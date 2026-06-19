using System.Collections.Generic;
using System.Linq;
using OneWare.Quartus.Helper;
using Xunit;

namespace OneWare.Quartus.UnitTests;

public class OneWareQuartusTests
{
    [Fact]
    public void LoadLibrary()
    {
        Assert.True(true);
    }

    // ── QsfFile: instance assignments ─────────────────────────────────────────

    [Fact]
    public void QsfFile_AddAndGetInstanceAssignment_QuotedValue()
    {
        var qsf = new QsfFile([]);
        qsf.AddInstanceAssignment("IO_STANDARD", "3.3-V LVCMOS", "led0", "top");

        var assignments = qsf.GetInstanceAssignments().ToList();
        Assert.Single(assignments);
        Assert.Equal("IO_STANDARD", assignments[0].Name);
        Assert.Equal("3.3-V LVCMOS", assignments[0].Value);
        Assert.Equal("led0", assignments[0].Signal);
        Assert.Equal("top", assignments[0].Entity);
    }

    [Fact]
    public void QsfFile_AddAndGetInstanceAssignment_UnquotedValue()
    {
        var qsf = new QsfFile([]);
        qsf.AddInstanceAssignment("WEAK_PULL_UP_RESISTOR", "ON", "btn0");

        var assignments = qsf.GetInstanceAssignments().ToList();
        Assert.Single(assignments);
        Assert.Equal("WEAK_PULL_UP_RESISTOR", assignments[0].Name);
        Assert.Equal("ON", assignments[0].Value);
        Assert.Equal("btn0", assignments[0].Signal);
        Assert.Null(assignments[0].Entity);
    }

    [Fact]
    public void QsfFile_ParseExistingInstanceAssignmentLines()
    {
        var qsf = new QsfFile([
            "set_instance_assignment -name IO_STANDARD \"1.2 V\" -to clk -entity seg_test",
            "set_instance_assignment -name WEAK_PULL_UP_RESISTOR ON -to io96_3a_pb0 -entity seg_test",
            "set_instance_assignment -name IO_STANDARD \"3.3-V LVCMOS\" -to io96_3a_led0 -entity seg_test"
        ]);

        var assignments = qsf.GetInstanceAssignments().ToList();
        Assert.Equal(3, assignments.Count);

        Assert.Equal(("IO_STANDARD", "1.2 V", "clk", "seg_test"), assignments[0]);
        Assert.Equal(("WEAK_PULL_UP_RESISTOR", "ON", "io96_3a_pb0", "seg_test"), assignments[1]);
        Assert.Equal(("IO_STANDARD", "3.3-V LVCMOS", "io96_3a_led0", "seg_test"), assignments[2]);
    }

    [Fact]
    public void QsfFile_RemoveInstanceAssignmentsByName_RemovesOnlyMatchingName()
    {
        var qsf = new QsfFile([
            "set_instance_assignment -name IO_STANDARD \"3.3-V LVCMOS\" -to led0 -entity top",
            "set_instance_assignment -name WEAK_PULL_UP_RESISTOR ON -to btn0 -entity top",
            "set_instance_assignment -name IO_STANDARD \"1.2 V\" -to clk -entity top"
        ]);

        qsf.RemoveInstanceAssignmentsByName("IO_STANDARD");

        var remaining = qsf.GetInstanceAssignments().ToList();
        Assert.Single(remaining);
        Assert.Equal("WEAK_PULL_UP_RESISTOR", remaining[0].Name);
    }

    [Fact]
    public void QsfFile_RoundTrip_LocationAndInstanceAssignments()
    {
        var qsf = new QsfFile([]);
        // Simulate SaveConnections output
        qsf.AddLocationAssignment("AG21", "led");
        qsf.AddInstanceAssignment("IO_STANDARD", "3.3-V LVCMOS", "led", "top");
        qsf.AddLocationAssignment("AD5", "clk");
        qsf.AddInstanceAssignment("IO_STANDARD", "1.2 V", "clk", "top");

        // Re-read via GetLocationAssignments / GetInstanceAssignments
        var locations = qsf.GetLocationAssignments().ToList();
        Assert.Equal(2, locations.Count);
        Assert.Contains(("AG21", "led"), locations);
        Assert.Contains(("AD5", "clk"), locations);

        var instances = qsf.GetInstanceAssignments().ToList();
        Assert.Equal(2, instances.Count);
        Assert.Contains(instances, i => i.Name == "IO_STANDARD" && i.Value == "3.3-V LVCMOS" && i.Signal == "led");
        Assert.Contains(instances, i => i.Name == "IO_STANDARD" && i.Value == "1.2 V" && i.Signal == "clk");
    }

    [Fact]
    public void QsfFile_GetLocationAssignments_BusSignalWithIndex()
    {
        var qsf = new QsfFile([
            "set_location_assignment PIN_T2  -to HDMI_DATA[1]",
            "set_location_assignment PIN_V1  -to HDMI_DATA[2]",
            "set_location_assignment PIN_AB1 -to HDMI_DATA[10]"
        ]);

        var locations = qsf.GetLocationAssignments().ToList();
        Assert.Equal(3, locations.Count);
        Assert.Contains(("T2",  "HDMI_DATA[1]"),  locations);
        Assert.Contains(("V1",  "HDMI_DATA[2]"),  locations);
        Assert.Contains(("AB1", "HDMI_DATA[10]"), locations);
    }

    [Theory]
    [InlineData("constraints.sdc",  "SDC_FILE")]
    [InlineData("core.ip",          "IP_FILE")]
    [InlineData("stp1.stp",         "SIGNALTAP_FILE")]
    [InlineData("netlist.edf",      "EDIF_FILE")]
    [InlineData("netlist.edif",     "EDIF_FILE")]
    [InlineData("mapped.vqm",       "VQM_FILE")]
    [InlineData("tb.vt",            "VERILOG_TEST_BENCH_FILE")]
    [InlineData("tb.vht",           "VHDL_TEST_BENCH_FILE")]
    [InlineData("top.vhd",          "VHDL_FILE")]
    [InlineData("top.v",            "VERILOG_FILE")]
    [InlineData("top.sv",           "SYSTEMVERILOG_FILE")]
    [InlineData("ip.qip",           "QIP_FILE")]
    [InlineData("sys.qsys",         "QSYS_FILE")]
    [InlineData("sch.bdf",          "BDF_FILE")]
    [InlineData("src.ahdl",         "AHDL_FILE")]
    [InlineData("script.tcl",       "TCL_SCRIPT_FILE")]
    [InlineData("data.hex",         "HEX_FILE")]
    [InlineData("data.mif",         "MIF_FILE")]
    [InlineData("mem.smf",          "SMF_FILE")]
    public void QsfFile_AddFile_ProducesCorrectGlobalAssignment(string filename, string expectedKeyword)
    {
        var qsf = new QsfFile([]);
        qsf.AddFile(filename);

        Assert.Single(qsf.Lines);
        Assert.Contains(expectedKeyword, qsf.Lines[0]);
        Assert.Contains(filename, qsf.Lines[0]);
    }

    [Fact]
    public void QsfFile_AddFile_UnknownExtension_AddsNoLine()
    {
        var qsf = new QsfFile([]);
        qsf.AddFile("readme.txt");
        Assert.Empty(qsf.Lines);
    }
}