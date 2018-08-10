using System;
using System.Collections.Specialized;
using System.Text;
using CCA.Util;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.CCAvenue.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.CCAvenue.Controllers
{
    public class PaymentCCAvenueController : BasePaymentController
    {
        private readonly CCAvenuePaymentSettings _ccAvenuePaymentSettings;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        public PaymentCCAvenueController(CCAvenuePaymentSettings ccAvenuePaymentSettings,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            this._ccAvenuePaymentSettings = ccAvenuePaymentSettings;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._paymentService = paymentService;
            this._permissionService = permissionService;
            this._settingService = settingService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                MerchantId = _ccAvenuePaymentSettings.MerchantId,
                Key = _ccAvenuePaymentSettings.Key,
                MerchantParam = _ccAvenuePaymentSettings.MerchantParam,
                PayUri = _ccAvenuePaymentSettings.PayUri,
                AdditionalFee = _ccAvenuePaymentSettings.AdditionalFee,
                AccessCode = _ccAvenuePaymentSettings.AccessCode
            };

            return View("~/Plugins/Payments.CCAvenue/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _ccAvenuePaymentSettings.MerchantId = model.MerchantId;
            _ccAvenuePaymentSettings.Key = model.Key;
            _ccAvenuePaymentSettings.MerchantParam = model.MerchantParam;
            _ccAvenuePaymentSettings.PayUri = model.PayUri;
            _ccAvenuePaymentSettings.AdditionalFee = model.AdditionalFee;
            _ccAvenuePaymentSettings.AccessCode = model.AccessCode;
            _settingService.SaveSetting(_ccAvenuePaymentSettings);

            return Configure();
        }

        public ActionResult Return(IpnModel model)
        {
            var processor =
                _paymentService.LoadPaymentMethodBySystemName("Payments.CCAvenue") as CCAvenuePaymentProcessor;
            if (processor == null || !_paymentService.IsPaymentMethodActive(processor) ||
                !processor.PluginDescriptor.Installed)
                throw new NopException("CCAvenue module cannot be loaded");

            //assign following values to send it to verifychecksum function.
            if (string.IsNullOrWhiteSpace(_ccAvenuePaymentSettings.Key))
                throw new NopException("CCAvenue key is not set");

            var workingKey = _ccAvenuePaymentSettings.Key;
            var ccaCrypto = new CCACrypto();
            var encResponse = ccaCrypto.Decrypt(model.Form["encResp"], workingKey);
            var paramList = new NameValueCollection();
            var segments = encResponse.Split('&');
            foreach (var seg in segments)
            {
                var parts = seg.Split('=');

                if (parts.Length <= 0) continue;

                paramList.Add(parts[0].Trim(), parts[1].Trim());
            }

            var sb = new StringBuilder();
            sb.AppendLine("CCAvenue:");
            for (var i = 0; i < paramList.Count; i++)
            {
                sb.AppendLine(paramList.Keys[i] + " = " + paramList[i]);
            }

            var orderId = paramList["Order_Id"];
            var authDesc = paramList["order_status"];

            var order = _orderService.GetOrderById(Convert.ToInt32(orderId));

            if (order == null)
                return RedirectToAction("Index", "Home", new {area = string.Empty});

            order.OrderNotes.Add(new OrderNote
            {
                Note = sb.ToString(),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            //var merchantId = Params["Merchant_Id"];
            //var Amount = Params["Amount"];
            //var myUtility = new CCAvenueHelper();
            //var checksum = myUtility.verifychecksum(merchantId, orderId, Amount, AuthDesc, _ccAvenuePaymentSettings.Key, checksum);

            if (!authDesc.Equals("Success", StringComparison.InvariantCultureIgnoreCase))
            {
                return RedirectToRoute("OrderDetails", new {orderId = order.Id});
            }

            //here you need to put in the routines for a successful transaction such as sending an email to customer,
            //setting database status, informing logistics etc etc

            if (_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                _orderProcessingService.MarkOrderAsPaid(order);
            }

            //thank you for shopping with us. Your credit card has been charged and your transaction is successful
            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }
    }
}