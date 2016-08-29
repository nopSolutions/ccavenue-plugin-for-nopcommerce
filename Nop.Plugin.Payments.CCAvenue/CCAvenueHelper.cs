using System;

namespace Nop.Plugin.Payments.CCAvenue
{
    public class CCAvenueHelper
    {
        public string GetCheckSum(string merchantId, string orderId, string amount, string redirectUrl, string workingKey)
        {
            var str = merchantId + "|" + orderId + "|" + amount + "|" + redirectUrl + "|" + workingKey;

            return Adler32(1, str);
        }

        public string VerifyCheckSum(string merchantId, string orderId, string amount, string authDesc, string workingKey, string checksum)
        {
            var str = merchantId + "|" + orderId + "|" + amount + "|" + authDesc + "|" + workingKey;

            var adlerResult = Adler32(1, str);

            var retval = string.Compare(adlerResult, checksum, StringComparison.OrdinalIgnoreCase) == 0 ? "true" : "false";
            return retval;
        }
        
        private string Adler32(long adler, string strPattern)
        {
            const long BASE = 65521;
            var s1 = Andop(adler, 65535);
            var s2 = Andop(Cdec(RightShift(Cbin(adler), 16)), 65535);

            for (var n = 0; n < strPattern.Length; n++)
            {
                var testchar = strPattern.Substring(n, 1).ToCharArray();
                var intTest = (long)testchar[0];
                s1 = (s1 + intTest) % BASE;
                s2 = (s2 + s1) % BASE;
            }
            return (Cdec(LeftShift(Cbin(s2), 16)) + s1).ToString();
        }
        
        private long Andop(long op1, long op2)
        {
            var op = "";

            var op3 = Cbin(op1);
            var op4 = Cbin(op2);

            for (var i = 0; i < 32; i++)
            {
                op = op + "" + (long.Parse(op3.Substring(i, 1)) & long.Parse(op4.Substring(i, 1)));
            }
            return Cdec(op);
        }

        private string Cbin(long num)
        {
            var bin = string.Empty;
            do
            {
                bin = (num % 2) + bin;
                num = (long)(double)Math.Floor((decimal)num / 2);
            } while (num != 0);

            long tempCount = 32 - bin.Length;

            for (var i = 1; i <= tempCount; i++)
            {
                bin = "0" + bin;
            }
            return bin;
        }
        
        private string LeftShift(string str, long num)
        {
            long tempCount = 32 - str.Length;

            for (var i = 1; i <= tempCount; i++)
            {
                str = "0" + str;
            }

            for (var i = 1; i <= num; i++)
            {
                str = str + "0";
                str = str.Substring(1, str.Length - 1);
            }
            return str;
        }
        
        private string RightShift(string str, long num)
        {
            for (var i = 1; i <= num; i++)
            {
                str = "0" + str;
                str = str.Substring(0, str.Length - 1);
            }
            return str;
        }

        private long Cdec(string strNum)
        {
            long dec = 0;
            for (var n = 0; n < strNum.Length; n++)
            {
                dec = dec + (long.Parse(strNum.Substring(n, 1)) * (long)Math.Pow(2, strNum.Length - (n + 1)));
            }
            return dec;
        }

    }	
}
