using System;
using System.Collections.Generic;

public static class PaymentUtils
{
    public static void CalculateFeeAndTax(decimal total
        , decimal subtotal
        , decimal? feeRate
        , bool buyerPaysFee
        , bool includeVAT
        , decimal? VATaxRate
        , string orderId
        , string description
        , out decimal feeAmount
        , out decimal discount
        , out decimal vatAmount
        , out decimal finalAmount
        , out string effectiveDescription)
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
            extraNotes.Add($"phí cổng {feeRate.Value:P2}");
        }

        if (includeVAT && vatAmount > 0)
        {
            extraNotes.Add($"VAT {VATaxRate.Value:P2}");
        }

        if (extraNotes.Count > 0)
            effectiveDescription += $" (đã bao gồm: {string.Join(", ", extraNotes)})";
    }
}