using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.CCAvenue.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.CCAvenue.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CCAvenue.Key")] //Encryption Key
        public string Key { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CCAvenue.MerchantParam")]
        public string MerchantParam { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CCAvenue.PayUri")] //Payment URI
        public string PayUri { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CCAvenue.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CCAvenue.AccessCode")] //Access Code
        public string AccessCode { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CCAvenue.EnableStatusCheckAndConfirmApi")]
        public bool EnableStatusCheckAndConfirmApi { get; set; }
    }
}