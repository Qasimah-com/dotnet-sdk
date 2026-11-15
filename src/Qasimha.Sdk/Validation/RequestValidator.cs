namespace Qasimha.Sdk.Validation;

internal static class RequestValidator
{
    public static void Validate(CreateSessionRequest request)
    {
        var errors = new List<ApiFieldError>();
        RequireText(errors, "OrderReferenceId", request.OrderReferenceId, 255);
        var amountsValid = Amount(errors, "TotalAmount", request.TotalAmount);
        amountsValid &= Amount(errors, "TaxAmount", request.TaxAmount);
        amountsValid &= Amount(errors, "ShippingAmount", request.ShippingAmount);
        amountsValid &= Amount(errors, "DiscountAmount", request.DiscountAmount);

#pragma warning disable CS0618
        var paymentType = request.PaymentType;
#pragma warning restore CS0618
        if (paymentType is not null && !CheckoutPaymentType.IsKnown(paymentType))
        {
            errors.Add(new ApiFieldError("PaymentType", "must be one of " + string.Join(", ", CheckoutPaymentType.Known)));
        }

        var itemsValid = Items(errors, request.Items);
        Consumer(errors, request.Consumer, emailRequired: false);

        if (request.ShippingAddress is null)
        {
            errors.Add(new ApiFieldError("ShippingAddress", "is required"));
        }
        else
        {
            Address(errors, "ShippingAddress", request.ShippingAddress);
        }

        if (request.BillingAddress is not null)
        {
            Address(errors, "BillingAddress", request.BillingAddress);
        }

        if (request.MerchantUrl is not null)
        {
            MerchantUrl(errors, request.MerchantUrl);
        }

        Language(errors, request.Language);

        if (amountsValid && itemsValid)
        {
            OrderTotal(errors, request.TotalAmount, request.Items, request.TaxAmount, request.ShippingAmount, request.DiscountAmount);
        }

        Throw(errors);
    }

    public static void Validate(CreatePosSessionRequest request)
    {
        var errors = new List<ApiFieldError>();
        RequireText(errors, "OrderReferenceId", request.OrderReferenceId, 255);
        var amountsValid = Amount(errors, "TotalAmount", request.TotalAmount);
        var itemsValid = Items(errors, request.Items);
        Consumer(errors, request.Consumer, emailRequired: true);

        if (amountsValid && itemsValid)
        {
            OrderTotal(errors, request.TotalAmount, request.Items, 0m, 0m, 0m);
        }

        Throw(errors);
    }

    public static void Validate(CreateQrSessionRequest request)
    {
        var errors = new List<ApiFieldError>();
        RequireText(errors, "OrderReferenceId", request.OrderReferenceId, 255);
        var amountsValid = Amount(errors, "TotalAmount", request.TotalAmount);
        var itemsValid = Items(errors, request.Items);
        Language(errors, request.Language);

        if (amountsValid && itemsValid)
        {
            OrderTotal(errors, request.TotalAmount, request.Items, 0m, 0m, 0m);
        }

        Throw(errors);
    }

    public static void Validate(CreateCheckoutLinkRequest request)
    {
        var errors = new List<ApiFieldError>();
        if (request.Amount is { } amount)
        {
            Amount(errors, "Amount", amount);
        }
        else if (request.Items is null || request.Items.Count == 0)
        {
            errors.Add(new ApiFieldError("Amount", "is required when there are no items"));
        }

        var channel = request.Channel ?? CheckoutLinkChannel.Link;
        if (!CheckoutLinkChannel.IsKnown(channel))
        {
            errors.Add(new ApiFieldError("Channel", "must be Link or Qr"));
        }
        else if (CheckoutLinkChannel.IsLink(channel) && string.IsNullOrWhiteSpace(request.Phone))
        {
            errors.Add(new ApiFieldError("Phone", "is required for the Link channel"));
        }

        Language(errors, request.Language);
        MaxLength(errors, "EjarContractNumber", request.EjarContractNumber, 50);

        if (request.OrderReferenceId is not null)
        {
            RequireText(errors, "OrderReferenceId", request.OrderReferenceId, 255);
        }

        if (request.Items is not null)
        {
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                var prefix = "Items[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "].";
                if (item is null)
                {
                    errors.Add(new ApiFieldError(prefix.TrimEnd('.'), "is null"));
                    continue;
                }

                RequireText(errors, prefix + "Name", item.Name, null);
                if (item.Quantity < 1)
                {
                    errors.Add(new ApiFieldError(prefix + "Quantity", "must be at least 1"));
                }

                Amount(errors, prefix + "UnitPrice", item.UnitPrice);
            }
        }

        Throw(errors);
    }

    public static void ValidateAmount(string field, decimal value)
    {
        var errors = new List<ApiFieldError>();
        Amount(errors, field, value);
        Throw(errors);
    }

    public static void Validate(RefundRequest request)
    {
        var errors = new List<ApiFieldError>();
        Amount(errors, "Amount", request.Amount);
        Throw(errors);
    }

    public static void Validate(FulfillRequest request)
    {
        var errors = new List<ApiFieldError>();
        if (request.ShippedAmount is { } shipped)
        {
            Amount(errors, "ShippedAmount", shipped);
        }

        Throw(errors);
    }

    public static void Validate(CreateWebhookEndpointRequest request)
    {
        var errors = new List<ApiFieldError>();
        HttpsUrl(errors, "Url", request.Url);
        EventNames(errors, request.Events);
        Throw(errors);
    }

    public static void Validate(UpdateWebhookEndpointRequest request)
    {
        var errors = new List<ApiFieldError>();
        if (request.Url is not null)
        {
            HttpsUrl(errors, "Url", request.Url);
        }

        EventNames(errors, request.Events);
        Throw(errors);
    }

    private static void HttpsUrl(List<ApiFieldError> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApiFieldError(field, "is required"));
        }
        else if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ApiFieldError(field, "must be an https:// URL"));
        }
    }

    private static void EventNames(List<ApiFieldError> errors, IReadOnlyList<string>? events)
    {
        if (events is null)
        {
            return;
        }

        for (var i = 0; i < events.Count; i++)
        {
            if (!WebhookEventType.IsKnown(events[i]))
            {
                errors.Add(new ApiFieldError(
                    "Events[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
                    "must be one of " + string.Join(", ", WebhookEventType.Known)));
            }
        }
    }

    private static bool Items(List<ApiFieldError> errors, IReadOnlyList<CheckoutItem>? items)
    {
        if (items is null || items.Count == 0)
        {
            errors.Add(new ApiFieldError("Items", "at least one item is required"));
            return false;
        }

        var allValid = true;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var prefix = "Items[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "].";
            if (item is null)
            {
                errors.Add(new ApiFieldError(prefix.TrimEnd('.'), "is null"));
                allValid = false;
                continue;
            }

            RequireText(errors, prefix + "ReferenceId", item.ReferenceId, null);
            RequireText(errors, prefix + "Name", item.Name, null);
            if (item.Type is not null)
            {
                if (string.IsNullOrWhiteSpace(item.Type))
                {
                    errors.Add(new ApiFieldError(prefix + "Type", "must not be empty"));
                }
                else
                {
                    MaxLength(errors, prefix + "Type", item.Type, 50);
                }
            }

            var quantityValid = item.Quantity >= 1;
            if (!quantityValid)
            {
                errors.Add(new ApiFieldError(prefix + "Quantity", "must be at least 1"));
            }

            var amountsValid = Amount(errors, prefix + "UnitPrice", item.UnitPrice);
            amountsValid &= Amount(errors, prefix + "TotalAmount", item.TotalAmount);
            amountsValid &= Amount(errors, prefix + "TaxAmount", item.TaxAmount);
            amountsValid &= Amount(errors, prefix + "DiscountAmount", item.DiscountAmount);

            if (quantityValid && amountsValid)
            {
                var expected = (item.Quantity * Money.ToHalalas(item.UnitPrice)) - Money.ToHalalas(item.DiscountAmount);
                var actual = Money.ToHalalas(item.TotalAmount);
                if (expected != actual)
                {
                    errors.Add(new ApiFieldError(
                        prefix + "TotalAmount",
                        "expected " + Money.FormatHalalas(expected)
                        + " (" + item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + " x " + Money.Format(item.UnitPrice)
                        + " - " + Money.Format(item.DiscountAmount)
                        + "), got " + Money.Format(item.TotalAmount)));
                    allValid = false;
                }
            }
            else
            {
                allValid = false;
            }
        }

        return allValid;
    }

    private static void OrderTotal(
        List<ApiFieldError> errors,
        decimal totalAmount,
        IReadOnlyList<CheckoutItem> items,
        decimal taxAmount,
        decimal shippingAmount,
        decimal discountAmount)
    {
        long lines = 0;
        foreach (var item in items)
        {
            lines += Money.ToHalalas(item.TotalAmount);
        }

        var expected = lines + Money.ToHalalas(taxAmount) + Money.ToHalalas(shippingAmount) - Money.ToHalalas(discountAmount);
        var actual = Money.ToHalalas(totalAmount);
        if (expected != actual)
        {
            errors.Add(new ApiFieldError(
                "TotalAmount",
                "expected " + Money.FormatHalalas(expected)
                + " (items " + Money.FormatHalalas(lines)
                + " + tax " + Money.Format(taxAmount)
                + " + shipping " + Money.Format(shippingAmount)
                + " - discount " + Money.Format(discountAmount)
                + "), got " + Money.Format(totalAmount)));
        }
    }

    private static void Consumer(List<ApiFieldError> errors, Consumer? consumer, bool emailRequired)
    {
        if (consumer is null)
        {
            errors.Add(new ApiFieldError("Consumer", "is required"));
            return;
        }

        RequireText(errors, "Consumer.FirstName", consumer.FirstName, null);
        RequireText(errors, "Consumer.LastName", consumer.LastName, null);
        RequireText(errors, "Consumer.PhoneNumber", consumer.PhoneNumber, null);

        if (emailRequired)
        {
            RequireText(errors, "Consumer.Email", consumer.Email, null);
        }

        if (!string.IsNullOrEmpty(consumer.NationalId) && !IsNationalId(consumer.NationalId))
        {
            errors.Add(new ApiFieldError("Consumer.NationalId", "must be 10 digits starting with 1 or 2"));
        }
    }

    private static void MerchantUrl(List<ApiFieldError> errors, MerchantUrl merchantUrl)
    {
        RequireText(errors, "MerchantUrl.Success", merchantUrl.Success, null);
        RequireText(errors, "MerchantUrl.Failure", merchantUrl.Failure, null);

#pragma warning disable CS0618
        var notification = merchantUrl.Notification;
#pragma warning restore CS0618
        if (!string.IsNullOrWhiteSpace(notification))
        {
            errors.Add(new ApiFieldError("MerchantUrl.Notification", "is reserved and must not be set"));
        }
    }

    private static void Language(List<ApiFieldError> errors, string? value)
    {
        if (value is not null
            && !string.Equals(value, "ar", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(value, "en", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ApiFieldError("Language", "must be ar or en"));
        }
    }

    private static void MaxLength(List<ApiFieldError> errors, string field, string? value, int max)
    {
        if (value is not null && value.Length > max)
        {
            errors.Add(new ApiFieldError(field, "must be at most " + max.ToString(System.Globalization.CultureInfo.InvariantCulture) + " characters"));
        }
    }

    private static void Address(List<ApiFieldError> errors, string prefix, Address address)
    {
        RequireText(errors, prefix + ".FirstName", address.FirstName, null);
        RequireText(errors, prefix + ".LastName", address.LastName, null);
        RequireText(errors, prefix + ".Line1", address.Line1, null);
        RequireText(errors, prefix + ".City", address.City, null);
        RequireText(errors, prefix + ".CountryCode", address.CountryCode, null);
    }

    private static bool Amount(List<ApiFieldError> errors, string field, decimal value)
    {
        if (value < 0m)
        {
            errors.Add(new ApiFieldError(field, "must not be negative"));
            return false;
        }

        if (!Money.HasAtMostTwoDecimals(value))
        {
            errors.Add(new ApiFieldError(field, "must have at most two decimals"));
            return false;
        }

        return true;
    }

    private static void RequireText(List<ApiFieldError> errors, string field, string? value, int? maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApiFieldError(field, "is required"));
        }
        else if (maxLength is { } max && value.Length > max)
        {
            errors.Add(new ApiFieldError(field, "must be at most " + max.ToString(System.Globalization.CultureInfo.InvariantCulture) + " characters"));
        }
    }

    private static bool IsNationalId(string value)
    {
        if (value.Length != 10 || value[0] is not ('1' or '2'))
        {
            return false;
        }

        foreach (var c in value)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static void Throw(List<ApiFieldError> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        var first = errors[0];
        var message = first.Field + ": " + first.Message;
        if (errors.Count > 1)
        {
            message += " (+" + (errors.Count - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " more)";
        }

        throw new ValidationException(message, errors);
    }
}
