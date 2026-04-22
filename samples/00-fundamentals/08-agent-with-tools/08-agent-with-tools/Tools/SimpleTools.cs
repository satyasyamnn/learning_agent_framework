using System.ComponentModel;

public class SimpleTools
{
    // Define tools as attributed methods
// The Description attributes help the LLM understand what each tool/parameter does

[Description("Get shipment risk status for a specified port.")]
public string GetPortRiskStatus([Description("The port name to check risk status for")] string port)
    => port.ToLower() switch
    {
        "singapore" => "Low disruption risk, customs throughput normal",
        "rotterdam" => "Moderate disruption risk, berth congestion observed",
        "los angeles" => "High disruption risk, vessel queue extended",
        "dubai" => "Low disruption risk, inspections on schedule",
        _ => "Unable to retrieve risk status for this port"
    };

[Description("Estimate customs duty amount from declared value and duty rate.")]
public double EstimateCustomsDuty(
    [Description("Declared shipment value in USD")] double declaredValue,
    [Description("Duty rate as a percentage (for example, 8.5 for 8.5%)")] double dutyRatePercent)
{
    return declaredValue * (dutyRatePercent / 100);
}

[Description("Get the average customs clearance time for a port.")]
public string GetPortClearanceTime([Description("The port name")] string port)
    => port.ToLower() switch
    {
        "singapore" => "14 hours",
        "rotterdam" => "20 hours",
        "los angeles" => "36 hours",
        "dubai" => "18 hours",
        _ => "Unknown port"
    };
}