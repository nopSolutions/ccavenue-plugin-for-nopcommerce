namespace Nop.Plugin.Payments.CCAvenue
{
    /// <summary>
    /// Represents constants of the CCAvenue plugin
    /// </summary>
    public static class CCAvenueDefaults
    {
        /// <summary>
        /// Gets pay link url
        /// </summary>
        public static string PayUri => "https://secure.ccavenue.com/transaction/transaction.do?command=initiateTransaction";

        /// <summary>
        /// Gets the return route name
        /// </summary>
        public static string ReturnRouteName => "Plugin.Payments.CCAvenue.Return";
    }
}
