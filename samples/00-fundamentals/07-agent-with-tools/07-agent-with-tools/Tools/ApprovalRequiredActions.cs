using System.ComponentModel;

/// <summary>
/// Customs officer actions that carry legal consequences and require explicit
/// human approval before execution.
///
/// In Program.cs each method here is wrapped in ApprovalRequiredAIFunction,
/// so the Microsoft Agent Framework will pause and surface a
/// ToolApprovalRequestContent for the operator to confirm before the method
/// body is ever invoked.
/// </summary>
public class ApprovalRequiredActions
{
    [Description(
        "Flag a shipment for physical detention by customs authorities. " +
        "This is a legally binding order that cannot be reversed without senior officer review. " +
        "Requires explicit officer approval before it is recorded.")]
    public static string FlagShipmentForDetention(
        [Description("ID of the shipment to detain, e.g. CSH-3004")] string shipmentId,
        [Description("Legal or compliance reason justifying the detention order")] string reason)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine("  *** DETENTION ORDER CREATED ***");
        Console.WriteLine($"  Shipment : {shipmentId}");
        Console.WriteLine($"  Reason   : {reason}");
        Console.ResetColor();

        return $"DETENTION ORDER issued for shipment {shipmentId}. " +
               $"Reason recorded: \"{reason}\". " +
               $"Shipment has been referred to the duty officer for physical inspection.";
    }
}
