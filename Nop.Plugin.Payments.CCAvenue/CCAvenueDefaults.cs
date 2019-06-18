namespace Nop.Plugin.Payments.CCAvenue
{
    /// <summary>
    /// Represents constants of the CCAvenue plugin
    /// </summary>
    public static class CCAvenueDefaults
    {
        /// <summary>
        /// Name of the view component to display seal in public store
        /// </summary>
        public const string VIEW_COMPONENT_NAME = "PaymentCCAvenue";

        /// <summary>
        /// Gets pay link url
        /// </summary>
        public static string PayUri => "https://secure.ccavenue.com/transaction/transaction.do?command=initiateTransaction";
    }
}
