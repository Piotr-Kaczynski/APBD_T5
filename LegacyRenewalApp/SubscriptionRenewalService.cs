using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly RenewalRequestValidator _validator;
        private readonly DiscountCalculator _discountCalculator;
        private readonly TaxRateProvider _taxRateProvider;
        private readonly IBillingGateway _billingGateway;
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;

        public SubscriptionRenewalService()
            : this(
                new RenewalRequestValidator(),
                new DiscountCalculator(),
                new TaxRateProvider(),
                new BillingGatewayClass(),
                new CustomerRepository(),
                new SubscriptionPlanRepository())
        {
        }

        public SubscriptionRenewalService(
            RenewalRequestValidator validator,
            DiscountCalculator discountCalculator,
            TaxRateProvider taxRateProvider,
            IBillingGateway billingGateway,
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository)
        {
            _validator = validator;
            _discountCalculator = discountCalculator;
            _taxRateProvider = taxRateProvider;
            _billingGateway = billingGateway;
            _customerRepository = customerRepository;
            _planRepository = planRepository;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            _validator.Validate(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;

            var (discountAmount, subtotalAfterDiscount, notes) = _discountCalculator.Calculate(
                customer, plan, seatCount, useLoyaltyPoints, baseAmount);

            string allNotes = notes;

            decimal supportFee = 0m;
            if (includePremiumSupport)
            {
                if (normalizedPlanCode == "START")           supportFee = 250m;
                else if (normalizedPlanCode == "PRO")        supportFee = 400m;
                else if (normalizedPlanCode == "ENTERPRISE") supportFee = 700m;

                allNotes += "premium support included; ";
            }

            decimal paymentFee = 0m;
            if (normalizedPaymentMethod == "CARD")
            {
                paymentFee = (subtotalAfterDiscount + supportFee) * 0.02m;
                allNotes += "card payment fee; ";
            }
            else if (normalizedPaymentMethod == "BANK_TRANSFER")
            {
                paymentFee = (subtotalAfterDiscount + supportFee) * 0.01m;
                allNotes += "bank transfer fee; ";
            }
            else if (normalizedPaymentMethod == "PAYPAL")
            {
                paymentFee = (subtotalAfterDiscount + supportFee) * 0.035m;
                allNotes += "paypal fee; ";
            }
            else if (normalizedPaymentMethod == "INVOICE")
            {
                paymentFee = 0m;
                allNotes += "invoice payment; ";
            }
            else
            {
                throw new ArgumentException("Unsupported payment method");
            }

            decimal taxRate = _taxRateProvider.GetRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                allNotes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = allNotes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body =
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                    $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

                _billingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }
    }
}