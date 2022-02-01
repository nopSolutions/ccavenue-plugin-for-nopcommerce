using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CCA.Util;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.CCAvenue
{
    /// <summary>
    /// CCAvenue payment processor
    /// </summary>
    public class CCAvenuePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CCAvenuePaymentSettings _ccAvenuePaymentSettings;
        private readonly CCACrypto _ccaCrypto;
        private readonly CurrencySettings _currencySettings;
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public CCAvenuePaymentProcessor(
            CCAvenuePaymentSettings ccAvenuePaymentSettings,
            CurrencySettings currencySettings,
            ICurrencyService currencyService,
            IAddressService addressService,
            ICountryService countryService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            ISettingService settingService,
            IStateProvinceService stateProvinceService,
            IWebHelper webHelper)
        {
            _ccAvenuePaymentSettings = ccAvenuePaymentSettings;
            _ccaCrypto = new CCACrypto();
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _addressService = addressService;
            _countryService = countryService;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _settingService = settingService;
            _stateProvinceService = stateProvinceService;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };

            return Task.FromResult(result);
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var remotePostHelperData = new Dictionary<string, string>();
            var remotePostHelper = new RemotePost(_httpContextAccessor, _webHelper)
            {
                FormName = "CCAvenueForm",
                Url = _ccAvenuePaymentSettings.PayUri
            };

            remotePostHelperData.Add("Merchant_Id", _ccAvenuePaymentSettings.MerchantId);
            remotePostHelperData.Add("Amount", postProcessPaymentRequest.Order.OrderTotal.ToString(new CultureInfo("en-US", false).NumberFormat));
            remotePostHelperData.Add("Currency", (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode);
            remotePostHelperData.Add("Order_Id", postProcessPaymentRequest.Order.Id.ToString());
            remotePostHelperData.Add("Redirect_Url", _webHelper.GetStoreLocation() + "Plugins/PaymentCCAvenue/Return");

            remotePostHelperData.Add("cancel_url", _webHelper.GetStoreLocation() + "Plugins/PaymentCCAvenue/Return");
            remotePostHelperData.Add("language", "EN");

            //var myUtility = new CCAvenueHelper();
            //remotePostHelperData.Add("Checksum", myUtility.getchecksum(_ccAvenuePaymentSettings.MerchantId.ToString(), postProcessPaymentRequest.Order.Id.ToString(), postProcessPaymentRequest.Order.OrderTotal.ToString(), _webHelper.GetStoreLocation(false) + "Plugins/PaymentCCAvenue/Return", _ccAvenuePaymentSettings.Key));

            //Billing details
            var billingAddress = await _addressService.GetAddressByIdAsync(postProcessPaymentRequest.Order.BillingAddressId);

            remotePostHelperData.Add("billing_name", billingAddress.FirstName);
            //remotePostHelperData.Add("billing_address", postProcessPaymentRequest.Order.BillingAddress.Address1 + " " + postProcessPaymentRequest.Order.BillingAddress.Address2);

            remotePostHelperData.Add("billing_address", billingAddress.Address1);
            remotePostHelperData.Add("billing_tel", billingAddress.PhoneNumber);
            remotePostHelperData.Add("billing_email", billingAddress.Email);

            remotePostHelperData.Add("billing_city", billingAddress.City);
            var billingStateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(billingAddress);
            remotePostHelperData.Add("billing_state", billingStateProvince != null ? billingStateProvince.Abbreviation : string.Empty);
            remotePostHelperData.Add("billing_zip", billingAddress.ZipPostalCode);
            var billingCountry = await _countryService.GetCountryByAddressAsync(billingAddress);
            remotePostHelperData.Add("billing_country", billingCountry != null ? billingCountry.Name : string.Empty);

            //Delivery details
            var shippingAddress = await _addressService.GetAddressByIdAsync(postProcessPaymentRequest.Order.ShippingAddressId ?? 0);

            if (postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                remotePostHelperData.Add("delivery_name", shippingAddress?.FirstName ?? string.Empty);
                //remotePostHelperData.Add("delivery_address", shippingAddress.Address1 + " " + shippingAddress.Address2);
                remotePostHelperData.Add("delivery_address", shippingAddress?.Address1 ?? string.Empty);
                //   remotePostHelper.Add("delivery_cust_notes", string.Empty);
                remotePostHelperData.Add("delivery_tel", shippingAddress?.PhoneNumber ?? string.Empty);
                remotePostHelperData.Add("delivery_city", shippingAddress?.City ?? string.Empty);
                remotePostHelperData.Add("delivery_state", (await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress))?.Abbreviation ?? string.Empty);
                remotePostHelperData.Add("delivery_zip", shippingAddress?.ZipPostalCode ?? string.Empty);
                remotePostHelperData.Add("delivery_country", (await _countryService.GetCountryByAddressAsync(shippingAddress))?.Name ?? string.Empty);
            }

            remotePostHelperData.Add("Merchant_Param", _ccAvenuePaymentSettings.MerchantParam);

            var strPOSTData = string.Empty;
            foreach (var item in remotePostHelperData)
                //strPOSTData = strPOSTData +  item.Key.ToLower() + "=" + item.Value.ToLower() + "&";
                strPOSTData = strPOSTData + item.Key.ToLower() + "=" + item.Value + "&";

            try
            {
                var strEncPOSTData = _ccaCrypto.Encrypt(strPOSTData, _ccAvenuePaymentSettings.Key);
                remotePostHelper.Add("encRequest", strEncPOSTData);
                remotePostHelper.Add("access_code", _ccAvenuePaymentSettings.AccessCode);

                remotePostHelper.Post();
            }
            catch (Exception ep)
            {
                throw new Exception(ep.Message);
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - hide; false - display.
        /// </returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the additional handling fee
        /// </returns>
        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(_ccAvenuePaymentSettings.AdditionalFee);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the capture payment result
        /// </returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");

            return Task.FromResult(result);
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");

            return Task.FromResult(result);
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");

            return Task.FromResult(result);
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");

            return Task.FromResult(result);
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");

            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //CCAvenue is the redirection payment method
            //It also validates whether order is also paid (after redirection) so customers will not be able to pay twice

            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return Task.FromResult(false);

            //let's ensure that at least 1 minute passed after order is placed
            return Task.FromResult(!((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1));
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentCCAvenue/Configure";
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of validating errors
        /// </returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            var warnings = new List<string>();

            return Task.FromResult<IList<string>>(warnings);
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info holder
        /// </returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();

            return Task.FromResult(paymentInfo);
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return CCAvenueDefaults.VIEW_COMPONENT_NAME;
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            var settings = new CCAvenuePaymentSettings()
            {
                MerchantId = "",
                Key = "",
                AccessCode = "",
                MerchantParam = "",

                //PayUri = "https://www.ccavenue.com/shopzone/cc_details.jsp",
                PayUri = CCAvenueDefaults.PayUri,
                AdditionalFee = 0,
            };
            await _settingService.SaveSettingAsync(settings);

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.CCAvenue.RedirectionTip"] = "You will be redirected to CCAvenue site to complete the order.",
                ["Plugins.Payments.CCAvenue.MerchantId"] = "Merchant ID",
                ["Plugins.Payments.CCAvenue.MerchantId.Hint"] = "Enter merchant ID.",
                ["Plugins.Payments.CCAvenue.Key"] = "Working Key",
                ["Plugins.Payments.CCAvenue.Key.Hint"] = "Enter working key.",
                ["Plugins.Payments.CCAvenue.MerchantParam"] = "Merchant Param",
                ["Plugins.Payments.CCAvenue.MerchantParam.Hint"] = "Enter merchant param.",
                ["Plugins.Payments.CCAvenue.PayUri"] = "Pay URI",
                ["Plugins.Payments.CCAvenue.PayUri.Hint"] = "Enter Pay URI.",
                ["Plugins.Payments.CCAvenue.AdditionalFee"] = "Additional fee",
                ["Plugins.Payments.CCAvenue.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
                ["Plugins.Payments.CCAvenue.AccessCode"] = "Access Code",
                ["Plugins.Payments.CCAvenue.AccessCode.Hint"] = "Enter Access Code.",
                ["Plugins.Payments.CCAvenue.PaymentMethodDescription"] = "For payment you will be redirected to the CCAvenue website."
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<CCAvenuePaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.CCAvenue");
            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.CCAvenue.PaymentMethodDescription");
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => false;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => false;

        #endregion
    }
}
