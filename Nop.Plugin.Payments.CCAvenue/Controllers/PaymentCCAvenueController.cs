using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.CCAvenue.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

using CCA.Util;
using System.Collections.Specialized;

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

        public PaymentCCAvenueController(ISettingService settingService,
            IPaymentService paymentService, IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            CCAvenuePaymentSettings ccAvenuePaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._ccAvenuePaymentSettings = ccAvenuePaymentSettings;
            this._paymentSettings = paymentSettings;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.MerchantId = _ccAvenuePaymentSettings.MerchantId;
            model.Key = _ccAvenuePaymentSettings.Key;
            model.MerchantParam = _ccAvenuePaymentSettings.MerchantParam;
            model.PayUri = _ccAvenuePaymentSettings.PayUri;
            model.AdditionalFee = _ccAvenuePaymentSettings.AdditionalFee;
            model.AccessCode = _ccAvenuePaymentSettings.AccessCode;
            return View("~/Plugins/Payments.CCAvenue/Views/PaymentCCAvenue/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
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

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            return View("~/Plugins/Payments.CCAvenue/Views/PaymentCCAvenue/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult Return(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.CCAvenue") as CCAvenuePaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("CCAvenue module cannot be loaded");


            var myUtility = new CCAvenueHelper();
            string orderId, merchantId, Amount, AuthDesc, checksum;

            //Assign following values to send it to verifychecksum function.
            if (String.IsNullOrWhiteSpace(_ccAvenuePaymentSettings.Key))
                throw new NopException("CCAvenue key is not set");

            string workingKey = _ccAvenuePaymentSettings.Key;
            CCACrypto ccaCrypto = new CCACrypto();
            string encResponse = ccaCrypto.Decrypt(Request.Form["encResp"], workingKey);
            NameValueCollection Params = new NameValueCollection();
            string[] segments = encResponse.Split('&');
            foreach (string seg in segments)
            {
                string[] parts = seg.Split('=');
                if (parts.Length > 0)
                {
                    string Key = parts[0].Trim();
                    string Value = parts[1].Trim();
                    Params.Add(Key, Value);
                }
            }

            for (int i = 0; i < Params.Count; i++)
            {
                Response.Write(Params.Keys[i] + " = " + Params[i] + "<br>");
            }

            /*
            merchantId = form["Merchant_Id"];
            orderId = form["Order_Id"];
            Amount = form["Amount"];
            AuthDesc = form["AuthDesc"];
            checksum = form["Checksum"];
            */

            merchantId = Params["Merchant_Id"];
            orderId = Params["Order_Id"];
            Amount = Params["Amount"];
            AuthDesc = Params["order_status"];
            // checksum = form["Checksum"];
            //checksum = myUtility.verifychecksum(merchantId, orderId, Amount, AuthDesc, _ccAvenuePaymentSettings.Key, checksum);

            if (AuthDesc == "Success")
            {

                /* 
                    Here you need to put in the routines for a successful 
                     transaction such as sending an email to customer,
                     setting database status, informing logistics etc etc
                */

                var order = _orderService.GetOrderById(Convert.ToInt32(orderId));
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    _orderProcessingService.MarkOrderAsPaid(order);
                }

                //Thank you for shopping with us. Your credit card has been charged and your transaction is successful
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

            }
            else if (AuthDesc == "Failure")
            {
                /*
                    Here you need to put in the routines for a failed
                    transaction such as sending an email to customer
                    setting database status etc etc
                */

                return Content("Thank you for shopping with us. However, the transaction has been declined");

            }

            //Commented this block as their is no Batch Processing in New CCAvenue API

            //else if ((checksum == "true") && (AuthDesc == "B"))
            //{
            //    /*
            //        Here you need to put in the routines/e-mail for a  "Batch Processing" order
            //        This is only if payment for this transaction has been made by an American Express Card
            //        since American Express authorisation status is available only after 5-6 hours by mail from ccavenue and at the "View Pending Orders"
            // */

            //    return Content("Thank you for shopping with us. We will keep you posted regarding the status of your order through e-mail");
            //}
            else
            {
                /*
                    Here you need to simply ignore this and dont need
                    to perform any operation in this condition
                */

                return Content("Security Error. Illegal access detected");
            }
        }

        [ValidateInput(false)]
        public ActionResult ReturnOLD(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.CCAvenue") as CCAvenuePaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("CCAvenue module cannot be loaded");


            var myUtility = new CCAvenueHelper();
            string orderId, merchantId, Amount, AuthDesc, checksum;

            //Assign following values to send it to verifychecksum function.
            if (String.IsNullOrWhiteSpace(_ccAvenuePaymentSettings.Key))
                throw new NopException("CCAvenue key is not set");

            merchantId = form["Merchant_Id"];
            orderId = form["Order_Id"];
            Amount = form["Amount"];
            AuthDesc = form["AuthDesc"];
            checksum = form["Checksum"];

            checksum = myUtility.verifychecksum(merchantId, orderId, Amount, AuthDesc, _ccAvenuePaymentSettings.Key, checksum);

            if ((checksum == "true") && (AuthDesc == "Y"))
            {

                /* 
                    Here you need to put in the routines for a successful 
                     transaction such as sending an email to customer,
                     setting database status, informing logistics etc etc
                */

                var order = _orderService.GetOrderById(Convert.ToInt32(orderId));
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    _orderProcessingService.MarkOrderAsPaid(order);
                }

                //Thank you for shopping with us. Your credit card has been charged and your transaction is successful
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

            }
            else if ((checksum == "true") && (AuthDesc == "N"))
            {
                /*
                    Here you need to put in the routines for a failed
                    transaction such as sending an email to customer
                    setting database status etc etc
                */

                return Content("Thank you for shopping with us. However, the transaction has been declined");

            }
            else if ((checksum == "true") && (AuthDesc == "B"))
            {
                /*
                    Here you need to put in the routines/e-mail for a  "Batch Processing" order
                    This is only if payment for this transaction has been made by an American Express Card
                    since American Express authorisation status is available only after 5-6 hours by mail from ccavenue and at the "View Pending Orders"
             */

                return Content("Thank you for shopping with us. We will keep you posted regarding the status of your order through e-mail");
            }
            else
            {
                /*
                    Here you need to simply ignore this and dont need
                    to perform any operation in this condition
                */

                return Content("Security Error. Illegal access detected");
            }
        }
    }
}