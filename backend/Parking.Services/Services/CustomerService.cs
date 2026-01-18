using System;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Parking.Services.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ICustomerRepository customerRepo, ILogger<CustomerService> logger)
        {
            _customerRepo = customerRepo;
            _logger = logger;
        }

        public async Task<Customer> GetOrCreateCustomerAsync(Customer customerInfo)
        {
            var existing = await _customerRepo.FindByPhoneAsync(customerInfo.Phone);
            if (existing != null)
            {
                // Optional: Update name if changed? For now, we reuse existing.
                return existing;
            }

            var newCustomer = new Customer
            {
                CustomerId = Guid.NewGuid().ToString(),
                Name = customerInfo.Name,
                Phone = customerInfo.Phone,
                IdentityNumber = customerInfo.IdentityNumber
            };

            await _customerRepo.AddAsync(newCustomer);
            _logger.LogInformation("New customer created: {Name} ({Phone})", newCustomer.Name, newCustomer.Phone);
            
            return newCustomer;
        }

        public async Task<Customer?> GetCustomerByIdAsync(string customerId)
        {
            return await _customerRepo.GetByIdAsync(customerId);
        }

        public async Task<Customer?> GetCustomerByPhoneAsync(string phone)
        {
            return await _customerRepo.FindByPhoneAsync(phone);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _customerRepo.UpdateAsync(customer);
        }
    }
}
