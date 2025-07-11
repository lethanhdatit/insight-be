using System;
using System.Collections.Generic;

public static class PaymentUtils
{
    public static void CalculateFeeAndTaxV2
    (
        decimal total,                   // Giá gốc chưa giảm, chưa VAT, chưa phí
        decimal subtotal,                // Giá sau giảm, chưa VAT, chưa phí
        decimal? feeRate,                // Tỷ lệ phí (0.02 = 2%)
        bool buyerPaysFee,              // Buyer chịu phí không?
        bool includeVAT,                // Có tính VAT không?
        decimal? VATaxRate,             // Thuế VAT (0.1 = 10%)
        string orderId,
        string description,
        out decimal feeAmount,
        out decimal discount,
        out decimal vatAmount,
        out decimal finalAmount,
        out string effectiveDescription
    )
    {
        // Phí giao dịch (trên tổng giá trị gồm VAT)
        feeAmount = buyerPaysFee
            ? subtotal * (1 + (VATaxRate ?? 0)) * feeRate.Value
            : 0;

        // Giá giảm
        discount = Math.Max(total - subtotal, 0);

        // VAT trên (subtotal + fee)
        vatAmount = includeVAT
            ? (subtotal + feeAmount) * VATaxRate.Value
            : 0;

        // Tổng tiền cuối cùng người mua trả
        finalAmount = subtotal + feeAmount + vatAmount;

        // Mô tả chi tiết đơn hàng
        effectiveDescription = description ?? $"Thanh toán đơn hàng #{orderId}";

        var extraNotes = new List<string>();

        if (buyerPaysFee)
        {
            extraNotes.Add($"phí cổng {feeRate.Value.ToPercent()}");
        }

        if (includeVAT && vatAmount > 0)
        {
            extraNotes.Add($"VAT {VATaxRate.Value.ToPercent()}");
        }

        if (extraNotes.Count > 0)
            effectiveDescription += $" (đã bao gồm: {string.Join(", ", extraNotes)})";
    }

    public static void CalculateFeeAndTaxV1
    (
        decimal total,                   // Giá gốc chưa giảm, chưa VAT, chưa phí
        decimal subtotal,                // Giá sau giảm, chưa VAT, chưa phí
        decimal? feeRate,                // Tỷ lệ phí (0.02 = 2%)
        bool buyerPaysFee,              // Buyer chịu phí không?
        bool includeVAT,                // Có tính VAT không?
        decimal? VATaxRate,             // Thuế VAT (0.1 = 10%)
        string orderId,
        string description,
        out decimal feeAmount,
        out decimal discount,
        out decimal vatAmount,
        out decimal finalAmount,
        out string effectiveDescription
    )
    {
        // Xác định giảm giá
        discount = Math.Max(total - subtotal, 0);

        // Nếu không có VAT hoặc buyer không chịu phí → xử lý đơn giản
        if (!includeVAT || !buyerPaysFee)
        {
            feeAmount = buyerPaysFee && feeRate.HasValue ? subtotal * feeRate.Value : 0;
            vatAmount = includeVAT && VATaxRate.HasValue ? (subtotal + feeAmount) * VATaxRate.Value : 0;
            finalAmount = subtotal + feeAmount + vatAmount;
        }
        else
        {
            // Trường hợp buyer chịu phí và cả phí lẫn hàng đều chịu VAT
            var vat = VATaxRate ?? 0;
            var fee = feeRate ?? 0;

            // F = S*(1 + v) / (1 - f*(1 + v))
            var multiplier = 1 + vat;
            var denominator = 1 - fee * multiplier;

            finalAmount = subtotal * multiplier / denominator;
            feeAmount = finalAmount * fee;
            vatAmount = (subtotal + feeAmount) * vat;
        }

        // Mô tả đơn hàng + ghi chú
        effectiveDescription = description ?? $"Thanh toán đơn hàng #{orderId}";
        var notes = new List<string>();

        if (buyerPaysFee && feeRate.HasValue && feeAmount > 0)
            notes.Add($"phí cổng {feeRate.Value.ToPercent()}");

        if (includeVAT && VATaxRate.HasValue && vatAmount > 0)
            notes.Add($"VAT {VATaxRate.Value.ToPercent()}");

        if (notes.Count > 0)
            effectiveDescription += $" (đã bao gồm: {string.Join(", ", notes)})";
    }

    public static decimal RoundAmountByGateProvider(TransactionProvider provider, decimal amount)
    {
        return provider switch
        {
            TransactionProvider.Paypal => Math.Round(amount, 2),
            TransactionProvider.VietQR => Math.Ceiling(amount),
            _ => Math.Ceiling(amount),
        };
    }
}