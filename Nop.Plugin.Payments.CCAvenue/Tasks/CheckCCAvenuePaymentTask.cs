using CCA.Util;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Payments.CCAvenue.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.ScheduleTasks;
using Nop.Web.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CCAvenue
{
    public class CheckCCAvenuePaymentTask : IScheduleTask
    {
        #region Fields
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly IAddressService _addressService;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;

        private readonly CurrencySettings _currencySettings;
        private readonly ICurrencyService _currencyService;

        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly CCACrypto _ccaCrypto;
        private readonly CCAvenuePaymentSettings _ccAvenuePaymentSettings;
        private readonly HttpClient _httpClient;
        #endregion

        #region Ctor
        public CheckCCAvenuePaymentTask(ISettingService settingService,
            IOrderProcessingService orderProcessingService,
            IStoreContext storeContext,
            IOrderService orderService,
            IAddressService addressService,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor,
            IWebHelper webHelper,

            CCAvenuePaymentSettings ccAvenuePaymentSettings,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            CurrencySettings currencySettings,
            ICurrencyService currencyService)
        {
            _ccaCrypto = new CCACrypto();
            _settingService = settingService;
            _orderProcessingService = orderProcessingService;
            _storeContext = storeContext;
            _orderService = orderService;
            _addressService = addressService;
            _logger = logger;

            _httpContextAccessor = httpContextAccessor;
            _webHelper = webHelper;

            _ccAvenuePaymentSettings = ccAvenuePaymentSettings;
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;

            _httpClient = new HttpClient();
        }
        #endregion

        #region Methods
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var ccAvenueSettings = await _settingService.LoadSettingAsync<CCAvenuePaymentSettings>(storeScope);
            if (ccAvenueSettings.EnableStatusCheckAndConfirmApi)
            {
                try
                {
                    var orderList = await _orderService.SearchOrdersAsync(createdFromUtc: DateTime.UtcNow.AddHours(-5), paymentMethodSystemName: "Payments.CCAvenue");
                    if (orderList != null)
                    {
                        foreach (var order in orderList)
                        {
                            if (order.PaymentStatus == Nop.Core.Domain.Payments.PaymentStatus.Pending)
                            {
                                //choosing correct order address
                                //var orderAddress = await _addressService.GetAddressByIdAsync(
                                //    (order.PickupInStore ? order.PickupAddressId : order.BillingAddressId) ?? 0);
                                #region get enc_request
                                OrderStatusRequest orderStatusRequest = new OrderStatusRequest();
                                orderStatusRequest.order_no = order.Id.ToString();
                                string jsonString = JsonConvert.SerializeObject(orderStatusRequest);
                                string enc_request = _ccaCrypto.Encrypt(jsonString, _ccAvenuePaymentSettings.Key);
                                #endregion
                                #region call api

                                string requestUri =
                                    string.Format("https://api.ccavenue.com/apis/servlet/DoWebTrans" +
                                    "?access_code={0}" +
                                    "&command=orderStatusTracker" +
                                    "&request_type=JSON" +
                                    "&response_type=JSON" +
                                    "&version=1.2" +
                                    "&enc_request={1}"
                                    , _ccAvenuePaymentSettings.AccessCode
                                    , enc_request);
                                var request = new HttpRequestMessage
                                {
                                    Method = HttpMethod.Post,
                                    RequestUri = new Uri(requestUri)
                                    //new Uri("https://api.ccavenue.com/apis/servlet/DoWebTrans?access_code=AVPF54KD80CC16FPCC&command=orderStatusTracker&request_type=JSON&response_type=JSON&version=1.2&enc_request=a8b27b7519aac72b6cb6e9aa67a1173619a23cfe3384facc77ae85fed7d41dcc"),
                                };
                                using (var response = await _httpClient.SendAsync(request))
                                {
                                    response.EnsureSuccessStatusCode();
                                    var body = await response.Content.ReadAsStringAsync();
                                    if (!string.IsNullOrEmpty(body))
                                    {
                                        var paramList = new NameValueCollection();
                                        foreach (var seg in body.Split('&'))
                                        {
                                            var parts = seg.Split('=');

                                            if (parts.Length <= 0)
                                                continue;

                                            paramList.Add(parts[0].Trim(), parts[1].Trim());
                                        }

                                        var response_status = paramList["status"];
                                        var enc_response = paramList["enc_response"];
                                        if (response_status == "0")
                                        {
                                            string decryptedResponse = _ccaCrypto.Decrypt(enc_response, _ccAvenuePaymentSettings.Key);
                                            OrderStatusResponse orderStatusResponse
                                                = JsonConvert.DeserializeObject<OrderStatusResponse>(decryptedResponse);
                                            if (orderStatusResponse.status == 0)
                                            {
                                                //Shipped -> means confimed transaction by CCAvenue
                                                if (orderStatusResponse.order_status.Equals("shipped", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                                                    {
                                                        await _orderProcessingService.MarkOrderAsPaidAsync(order);
                                                    }
                                                }
                                                else
                                                {
                                                    OrderNote orderNote = new OrderNote();
                                                    orderNote.Note = decryptedResponse;
                                                    orderNote.OrderId = order.Id;
                                                    orderNote.DisplayToCustomer = false;
                                                    await _orderService.InsertOrderNoteAsync(orderNote);
                                                }
                                            }
                                            else
                                            {
                                                OrderNote orderNote = new OrderNote();
                                                orderNote.Note = decryptedResponse;
                                                orderNote.OrderId = order.Id;
                                                orderNote.DisplayToCustomer = false;
                                                await _orderService.InsertOrderNoteAsync(orderNote);
                                            }
                                        }
                                        else
                                        {
                                            OrderNote orderNote = new OrderNote();
                                            orderNote.Note = enc_response;
                                            orderNote.OrderId = order.Id;
                                            orderNote.DisplayToCustomer = false;
                                            await _orderService.InsertOrderNoteAsync(orderNote);
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Error, ex.Message, ex.StackTrace);
                }
            }
        }
        #endregion

    }
}
