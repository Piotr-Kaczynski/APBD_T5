using System;

namespace LegacyRenewalApp
{
    public class RenewalRequestValidator
    {
        public void Validate(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0)
                throw new ArgumentException("Customer id must be positive");

            if (string.IsNullOrWhiteSpace(planCode))
                throw new ArgumentException("Plan code is required");

            if (seatCount <= 0)
                throw new ArgumentException("Seat count must be positive");

            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required");
        }
    }
}