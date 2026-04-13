using System;

namespace LegacyRenewalApp
{
    public class DiscountCalculator
    {
        private const decimal MinimumSubtotal = 300m;
        private const int MaxLoyaltyPointsToRedeem = 200;

        public (decimal DiscountAmount, decimal Subtotal, string Notes) Calculate(
            Customer customer,
            SubscriptionPlan plan,
            int seatCount,
            bool useLoyaltyPoints,
            decimal baseAmount)
        {
            decimal discountAmount = 0m;
            string notes = string.Empty;

            discountAmount += CalculateSegmentDiscount(customer, plan, baseAmount, ref notes);
            discountAmount += CalculateLoyaltyYearsDiscount(customer, baseAmount, ref notes);
            discountAmount += CalculateTeamSizeDiscount(seatCount, baseAmount, ref notes);
            discountAmount += CalculateLoyaltyPointsDiscount(customer, useLoyaltyPoints, ref notes);

            decimal subtotal = baseAmount - discountAmount;
            if (subtotal < MinimumSubtotal)
            {
                subtotal = MinimumSubtotal;
                notes += "minimum discounted subtotal applied; ";
            }

            return (discountAmount, subtotal, notes);
        }

        private decimal CalculateSegmentDiscount(Customer customer, SubscriptionPlan plan, decimal baseAmount, ref string notes)
        {
            if (customer.Segment == "Silver")  { notes += "silver discount; ";    return baseAmount * 0.05m; }
            if (customer.Segment == "Gold")    { notes += "gold discount; ";      return baseAmount * 0.10m; }
            if (customer.Segment == "Platinum"){ notes += "platinum discount; ";  return baseAmount * 0.15m; }
            if (customer.Segment == "Education" && plan.IsEducationEligible)
                                               { notes += "education discount; "; return baseAmount * 0.20m; }
            return 0m;
        }

        private decimal CalculateLoyaltyYearsDiscount(Customer customer, decimal baseAmount, ref string notes)
        {
            if (customer.YearsWithCompany >= 5) { notes += "long-term loyalty discount; "; return baseAmount * 0.07m; }
            if (customer.YearsWithCompany >= 2) { notes += "basic loyalty discount; ";     return baseAmount * 0.03m; }
            return 0m;
        }

        private decimal CalculateTeamSizeDiscount(int seatCount, decimal baseAmount, ref string notes)
        {
            if (seatCount >= 50) { notes += "large team discount; ";  return baseAmount * 0.12m; }
            if (seatCount >= 20) { notes += "medium team discount; "; return baseAmount * 0.08m; }
            if (seatCount >= 10) { notes += "small team discount; ";  return baseAmount * 0.04m; }
            return 0m;
        }

        private decimal CalculateLoyaltyPointsDiscount(Customer customer, bool useLoyaltyPoints, ref string notes)
        {
            if (!useLoyaltyPoints || customer.LoyaltyPoints <= 0)
                return 0m;

            int pointsToUse = Math.Min(customer.LoyaltyPoints, MaxLoyaltyPointsToRedeem);
            notes += $"loyalty points used: {pointsToUse}; ";
            return pointsToUse;
        }
    }
}