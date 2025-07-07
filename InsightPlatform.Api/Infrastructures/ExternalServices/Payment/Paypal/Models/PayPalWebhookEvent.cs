using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class PayPalWebhookEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("event_version")]
    public string EventVersion { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime CreateTime { get; set; }

    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; }

    [JsonPropertyName("resource_version")]
    public string ResourceVersion { get; set; }

    [JsonPropertyName("event_type")]
    public string EventType { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [JsonPropertyName("resource")]
    public PayPalCheckoutOrderResource Resource { get; set; }

    [JsonPropertyName("links")]
    public List<PayPalWebhookLink> Links { get; set; }
}

public class PayPalCheckoutOrderResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("intent")]
    public string Intent { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime CreateTime { get; set; }

    [JsonPropertyName("payer")]
    public PayPalPayer Payer { get; set; }

    [JsonPropertyName("purchase_units")]
    public List<PayPalPurchaseUnit> PurchaseUnits { get; set; }

    [JsonPropertyName("payment_source")]
    public PayPalPaymentSource PaymentSource { get; set; }

    [JsonPropertyName("links")]
    public List<PayPalWebhookLink> Links { get; set; }
}

public class PayPalPurchaseUnit
{
    [JsonPropertyName("reference_id")]
    public string ReferenceId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("custom_id")]
    public string CustomId { get; set; }

    [JsonPropertyName("invoice_id")]
    public string InvoiceId { get; set; }

    [JsonPropertyName("note_to_payer")]
    public string NoteToPayer { get; set; }

    [JsonPropertyName("amount")]
    public PayPalAmountWithBreakdown Amount { get; set; }

    [JsonPropertyName("payee")]
    public PayPalPayee Payee { get; set; }

    [JsonPropertyName("shipping")]
    public PayPalShipping Shipping { get; set; }
}

public class PayPalAmountWithBreakdown
{
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("breakdown")]
    public object Breakdown { get; set; } // chưa có detail trong JSON nên để object
}

public class PayPalPayee
{
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("merchant_id")]
    public string MerchantId { get; set; }
}

public class PayPalShipping
{
    [JsonPropertyName("name")]
    public PayPalName Name { get; set; }

    [JsonPropertyName("address")]
    public PayPalAddress Address { get; set; }
}

public class PayPalPayer
{
    [JsonPropertyName("name")]
    public PayPalName Name { get; set; }

    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("payer_id")]
    public string PayerId { get; set; }

    [JsonPropertyName("address")]
    public PayPalAddress Address { get; set; }
}

public class PayPalPaymentSource
{
    [JsonPropertyName("paypal")]
    public PayPalPaymentSourceDetail PayPal { get; set; }
}

public class PayPalPaymentSourceDetail
{
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("account_id")]
    public string AccountId { get; set; }

    [JsonPropertyName("account_status")]
    public string AccountStatus { get; set; }

    [JsonPropertyName("name")]
    public PayPalName Name { get; set; }

    [JsonPropertyName("address")]
    public PayPalAddress Address { get; set; }
}

public class PayPalName
{
    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }

    [JsonPropertyName("surname")]
    public string Surname { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } // optional – dùng cho shipping.name
}

public class PayPalAddress
{
    [JsonPropertyName("address_line_1")]
    public string AddressLine1 { get; set; }

    [JsonPropertyName("admin_area_2")]
    public string AdminArea2 { get; set; }

    [JsonPropertyName("admin_area_1")]
    public string AdminArea1 { get; set; }

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; }

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; }
}

public class PayPalWebhookLink
{
    [JsonPropertyName("href")]
    public string Href { get; set; }

    [JsonPropertyName("rel")]
    public string Rel { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; }
}


public class PayPalWebhookResource
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("amount")]
    public PayPalAmount Amount { get; set; }

    [JsonPropertyName("final_capture")]
    public bool FinalCapture { get; set; }

    [JsonPropertyName("seller_protection")]
    public PayPalSellerProtection SellerProtection { get; set; }

    [JsonPropertyName("seller_receivable_breakdown")]
    public PayPalSellerReceivableBreakdown SellerReceivableBreakdown { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime CreateTime { get; set; }

    [JsonPropertyName("update_time")]
    public DateTime UpdateTime { get; set; }

    [JsonPropertyName("links")]
    public List<PayPalWebhookLink> Links { get; set; }

    [JsonPropertyName("payer")]
    public PayPalPayer Payer { get; set; }
}

public class PayPalAmount
{
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class PayPalSellerProtection
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("dispute_categories")]
    public List<string> DisputeCategories { get; set; }
}

public class PayPalSellerReceivableBreakdown
{
    [JsonPropertyName("gross_amount")]
    public PayPalAmount GrossAmount { get; set; }

    [JsonPropertyName("paypal_fee")]
    public PayPalAmount PayPalFee { get; set; }

    [JsonPropertyName("net_amount")]
    public PayPalAmount NetAmount { get; set; }
}
