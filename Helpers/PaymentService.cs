using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;

namespace UmbiloRentals.Helpers
{
    public static class PaymentService
    {
        private static readonly string MerchantId =
            ConfigurationManager.AppSettings["PayFastMerchantId"];

        private static readonly string MerchantKey =
            ConfigurationManager.AppSettings["PayFastMerchantKey"];

        private static readonly string Passphrase =
            ConfigurationManager.AppSettings["PayFastPassphrase"];

        private static readonly string ReturnUrl =
            ConfigurationManager.AppSettings["PayFastReturnUrl"];

        private static readonly string CancelUrl =
            ConfigurationManager.AppSettings["PayFastCancelUrl"];

        private static readonly string NotifyUrl =
            ConfigurationManager.AppSettings["PayFastNotifyUrl"];

        private static readonly string Mode =
            ConfigurationManager.AppSettings["PayFastMode"];

        public static string GetPayFastUrl()
        {
            return Mode == "Live"
                ? "https://www.payfast.co.za/eng/process"
                : "https://sandbox.payfast.co.za/eng/process";
        }

        public static Dictionary<string, string> CreatePaymentData(
            string reference,
            decimal amount,
            string itemName,
            string firstName,
            string lastName,
            string email)
        {
            var data = new Dictionary<string, string>();

            data["merchant_id"] = MerchantId;
            data["merchant_key"] = MerchantKey;
            data["return_url"] = ReturnUrl;
            data["cancel_url"] = CancelUrl;
            data["notify_url"] = NotifyUrl;

            data["m_payment_id"] = reference;
            data["amount"] = amount.ToString("0.00");
            data["item_name"] = itemName;

            data["name_first"] = firstName;
            data["name_last"] = lastName;
            data["email_address"] = email;

            return data;
        }

        public static string BuildQueryString(
            Dictionary<string, string> data)
        {
            var list = new List<string>();

            foreach (var item in data)
            {
                list.Add(
                    item.Key + "=" +
                    HttpUtility.UrlEncode(item.Value));
            }

            return string.Join("&", list);
        }
    }
}