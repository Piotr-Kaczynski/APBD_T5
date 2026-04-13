using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public class TaxRateProvider
    {
        private const decimal DefaultTaxRate = 0.20m;

        private static readonly Dictionary<string, decimal> RatesByCountry =
            new Dictionary<string, decimal>
            {
                { "Poland",         0.23m },
                { "Germany",        0.19m },
                { "Czech Republic", 0.21m },
                { "Norway",         0.25m },
            };

        public decimal GetRate(string country)
        {
            return RatesByCountry.TryGetValue(country, out decimal rate) ? rate : DefaultTaxRate;
        }
    }
}