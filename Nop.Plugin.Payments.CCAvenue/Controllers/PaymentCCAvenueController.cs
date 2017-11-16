using System;
using System.Collections.Specialized;
using CCA.Util;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.CCAvenue.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System.Text;
using Nop.Services.Security;

namespace Nop.Plugin.Payments.CCAvenue.Controllers
{
    public class PaymentCCAvenueController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly CCAvenuePaymentSettings _ccAvenuePaymentSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly IPermissionService _permissionService;

        public PaymentCCAvenueController(ISettingService settingService,
            IPaymentService paymentService, IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            CCAvenuePaymentSettings ccAvenuePaymentSettings,
            PaymentSettings paymentSettings,
            IPermissionService permissionService)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._ccAvenuePaymentSettings = ccAvenuePaymentSettings;
            this._paymentSettings = paymentSettings;
            this._permissionService = permissionService;
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
        
        public ActionResult Return()
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.CCAvenue") as CCAvenuePaymentProcessor;
            if (processor == null || !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("CCAvenue module cannot be loaded");

            //assign following values to send it to verifychecksum function.
            if (string.IsNullOrWhiteSpace(_ccAvenuePaymentSettings.Key))
                throw new NopException("CCAvenue key is not set");

            var workingKey = _ccAvenuePaymentSettings.Key;
            var ccaCrypto = new CCACrypto();
            var encResponse = ccaCrypto.Decrypt(Request.Form["encResp"], workingKey);
            var paramList = new NameValueCollection();
            var segments = encResponse.Split('&');
            foreach (var seg in segments)
            {
                var parts = seg.Split('=');

                if (parts.Length <= 0) continue;
                
                paramList.Add(parts[0].Trim(), parts[1].Trim());
            }

            for (var i = 0; i < paramList.Count; i++)
            {
                var data = Encoding.UTF8.GetBytes(paramList.Keys[i] + " = " + paramList[i] + "<br>");
                Response.Body.Write(data, 0, data.Length);
            }
            
            var orderId = paramList["Order_Id"];
            var authDesc = paramList["order_status"];

            //var merchantId = Params["Merchant_Id"];
            //var Amount = Params["Amount"];
            //var myUtility = new CCAvenueHelper();
            //var checksum = myUtility.verifychecksum(merchantId, orderId, Amount, AuthDesc, _ccAvenuePaymentSettings.Key, checksum);

            switch (authDesc)
            {
                case "Success":
                    //here you need to put in the routines for a successful transaction such as sending an email to customer,
                    //setting database status, informing logistics etc etc
                    var order = _orderService.GetOrderById(Convert.ToInt32(orderId));
                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        _orderProcessingService.MarkOrderAsPaid(order);
                    }

                    //thank you for shopping with us. Your credit card has been charged and your transaction is successful
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                case "Failure":
                    //here you need to put in the routines for a failed transaction such as sending an email to customer
                    //setting database status etc etc
                    return Content("Thank you for shopping with us. However, the transaction has been declined");
            }

            //Commented this block as their is no Batch Processing in New CCAvenue API

            //if ((checksum == "true") && (AuthDesc == "B"))
            //{
            //    //here you need to put in the routines/e-mail for a  "Batch Processing" order.
            //    //This is only if payment for this transaction has been made by an American Express Card
            //    //since American Express authorisation status is available only after 5-6 hours by mail from ccavenue and at the "View Pending Orders"
            
            //    return Content("Thank you for shopping with us. We will keep you posted regarding the status of your order through e-mail");
            //}
            //else
            //{
            //    //here you need to simply ignore this and dont need to perform any operation in this condition

            //    return Content("Security Error. Illegal access detected");
            //}

            return Content("Security Error. Illegal access detected");
        }

        public ActionResult ReturnOLD()
        {
            var form = Request.Form;
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.CCAvenue") as CCAvenuePaymentProcessor;
            if (processor == null || !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("CCAvenue module cannot be loaded");

            var myUtility = new CCAvenueHelper();

            //assign following values to send it to verifychecksum function.
            if (string.IsNullOrWhiteSpace(_ccAvenuePaymentSettings.Key))
                throw new NopException("CCAvenue key is not set");

            var merchantId = form["Merchant_Id"];
            var orderId = form["Order_Id"];
            var amount = form["Amount"];
            var authDesc = form["AuthDesc"];
            var checksum = form["Checksum"];

            checksum = myUtility.VerifyCheckSum(merchantId, orderId, amount, authDesc, _ccAvenuePaymentSettings.Key,
                checksum);

            if (checksum == "true" && authDesc == "Y")
            {
                //here you need to put in the routines for a successful transaction such as sending an email to customer,
                //setting database status, informing logistics etc etc

                var order = _orderService.GetOrderById(Convert.ToInt32(orderId));
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    _orderProcessingService.MarkOrderAsPaid(order);
                }

                //thank you for shopping with us. Your credit card has been charged and your transaction is successful
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }

            if (checksum == "true" && authDesc == "N")
            {
                //here you need to put in the routines for a failed transaction such as sending an email to customer
                //setting database status etc etc

                return Content("Thank you for shopping with us. However, the transaction has been declined");
            }

            if (checksum == "true" && authDesc == "B")
            {
                //here you need to put in the routines/e-mail for a  "Batch Processing" order.
                //This is only if payment for this transaction has been made by an American Express Card
                //since American Express authorisation status is available only after 5-6 hours by mail from ccavenue and at the "View Pending Orders"

                return Content("Thank you for shopping with us. We will keep you posted regarding the status of your order through e-mail");
            }

            //here you need to simply ignore this and dont need to perform any operation in this condition
            return Content("Security Error. Illegal access detected");
        }
    }
}