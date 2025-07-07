using System.Collections.Generic;
using System.Text.Json.Serialization;

public class PaypalPaymentMetaData
{
    public string IpnUrl { get; set; }

    public string CallbackUrl { get; set; }
}