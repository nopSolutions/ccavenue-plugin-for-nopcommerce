namespace Nop.Plugin.Payments.CCAvenue.Models
{
    public class OrderStatusResponse
    {
        public string reference_no { get; set; }
        public string order_no { get; set; }
        public string order_currncy { get; set; }
        public double order_amt { get; set; }
        public string order_date_time { get; set; }
        public string order_bill_name { get; set; }
        public string order_bill_address { get; set; }
        public string order_bill_zip { get; set; }
        public string order_bill_tel { get; set; }
        public string order_bill_email { get; set; }
        public string order_bill_country { get; set; }
        public string order_ship_name { get; set; }
        public string order_ship_address { get; set; }
        public string order_ship_country { get; set; }
        public string order_ship_tel { get; set; }
        public string order_bill_city { get; set; }
        public string order_bill_state { get; set; }
        public string order_ship_city { get; set; }
        public string order_ship_state { get; set; }
        public string order_ship_zip { get; set; }
        public string order_ship_email { get; set; }
        public string order_notes { get; set; }
        public string order_ip { get; set; }
        public string order_status { get; set; }
        public string order_fraud_status { get; set; }
        public string order_status_date_time { get; set; }
        public double order_capt_amt { get; set; }
        public string order_card_name { get; set; }
        public string order_delivery_details { get; set; }
        public double order_fee_perc { get; set; }
        public double order_fee_perc_value { get; set; }
        public double order_fee_flat { get; set; }
        public double order_gross_amt { get; set; }
        public double order_discount { get; set; }
        public double order_tax { get; set; }
        public string order_bank_ref_no { get; set; }
        public string order_gtw_id { get; set; }
        public string order_bank_response { get; set; }
        public string order_option_type { get; set; }
        public double order_TDS { get; set; }
        public string order_device_type { get; set; }
        public string param_value1 { get; set; }
        public string param_value2 { get; set; }
        public string param_value3 { get; set; }
        public string param_value4 { get; set; }
        public string param_value5 { get; set; }
        public string error_desc { get; set; }
        public int status { get; set; }
        public string error_code { get; set; }
    }
}