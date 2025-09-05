using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Data.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public CustomerRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<CustomerBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyProgram)
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            return customer == null ? null : _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<(IEnumerable<CustomerBusinessModel> Items, int TotalCount)> SearchByNameAsync(
            string name,
            string groupId,
            int page,
            int pageSize)
        {
            var query = _context.Customers
                .Where(c => c.GroupId == groupId);

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(c => c.Name.Contains(name) || c.Document.Contains(name));
            }

            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(c => c.LoyaltyProgram)
                .ToListAsync();

            return (customers.Select(c => _mapper.Map<CustomerBusinessModel>(c)), totalCount);
        }

        public async Task<CustomerBusinessModel> CreateCustomerAsync(
            string groupId,
            string name,
            string document,
            string? email,
            string? phone,
            string? address)
        {
            var customer = new CustomerModel
            {
                GroupId = groupId,
                Name = name,
                Document = document,
                Email = email,
                Phone = phone,
                Address = address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> UpdateCustomerAsync(
            string id,
            string groupId,
            string name,
            string document,
            string? email,
            string? phone,
            string? address)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            customer.Name = name;
            customer.Document = document;
            customer.Email = email;
            customer.Phone = phone;
            customer.Address = address;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> DeleteCustomerAsync(string id, string groupId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> ActivateCustomerAsync(string id, string groupId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            customer.IsActive = true;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> DeactivateCustomerAsync(string id, string groupId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> LinkToLoyaltyProgramAsync(string id, string groupId, string loyaltyProgramId)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyProgram)
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            customer.LoyaltyProgramId = loyaltyProgramId;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload to get the loyalty program
            await _context.Entry(customer).Reference(c => c.LoyaltyProgram).LoadAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> UnlinkFromLoyaltyProgramAsync(string id, string groupId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            customer.LoyaltyProgramId = null;
            customer.LoyaltyPoints = 0;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> AddLoyaltyPointsAsync(string id, string groupId, float amount)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyProgram)
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null || customer.LoyaltyProgram == null)
            {
                return null;
            }

            // Calculate points based on conversion rate
            int pointsToAdd = (int)((decimal)amount * customer.LoyaltyProgram.CentsToPoints);
            customer.LoyaltyPoints += pointsToAdd;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> RemoveLoyaltyPointsAsync(string id, string groupId, float amount)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyProgram)
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null || customer.LoyaltyProgram == null)
            {
                return null;
            }

            // Calculate points based on conversion rate
            int pointsToRemove = (int)((decimal)amount * customer.LoyaltyProgram.CentsToPoints);
            
            // Check if customer has enough points
            if (customer.LoyaltyPoints < pointsToRemove)
            {
                return null;
            }

            customer.LoyaltyPoints -= pointsToRemove;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> RemoveLoyaltyPointsDirectAsync(string id, string groupId, int points)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            // Check if customer has enough points
            if (customer.LoyaltyPoints < points)
            {
                return null;
            }

            customer.LoyaltyPoints -= points;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<int> GetLoyaltyPointsAsync(string id, string groupId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            return customer?.LoyaltyPoints ?? 0;
        }

        public async Task<CustomerBusinessModel?> GetByDocumentAsync(string document, string groupId)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyProgram)
                .FirstOrDefaultAsync(c => c.Document == document && c.GroupId == groupId);

            return customer == null ? null : _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<(IEnumerable<CustomerBusinessModel> Items, int TotalCount)> SearchAsync(
            string? term,
            string groupId,
            int page,
            int pageSize)
        {
            var query = _context.Customers
                .Where(c => c.GroupId == groupId);

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(c => c.Name.Contains(term) || c.Document.Contains(term));
            }

            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(c => c.LoyaltyProgram)
                .ToListAsync();

            return (customers.Select(c => _mapper.Map<CustomerBusinessModel>(c)), totalCount);
        }

        public async Task<CustomerBusinessModel?> AddPointsAsync(string id, string groupId, decimal points, string description)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyProgram)
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            // Add points directly (controller already did the conversion)
            customer.LoyaltyPoints += Convert.ToInt32(points);
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<CustomerBusinessModel?> RemovePointsAsync(string id, string groupId, decimal points, string description)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyProgram)
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (customer == null)
            {
                return null;
            }

            // Check if customer has enough points
            if (customer.LoyaltyPoints < Convert.ToInt32(points))
            {
                return null;
            }

            // Remove points directly (controller already did the conversion)
            customer.LoyaltyPoints -= Convert.ToInt32(points);
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerBusinessModel>(customer);
        }

        public async Task<(bool HasCustomersWithPoints, int CustomerCount)> HasCustomersWithPointsInLoyaltyProgramAsync(string loyaltyProgramId, string groupId)
        {
            var customersWithPoints = await _context.Customers
                .Where(c => c.LoyaltyProgramId == loyaltyProgramId && 
                       c.GroupId == groupId && 
                       c.LoyaltyPoints > 0)
                .ToListAsync();

            return (customersWithPoints.Any(), customersWithPoints.Count);
        }
        
        public async Task<int> UnlinkAllCustomersFromLoyaltyProgramAsync(string loyaltyProgramId, string groupId)
        {
            var customers = await _context.Customers
                .Where(c => c.LoyaltyProgramId == loyaltyProgramId && 
                       c.GroupId == groupId)
                .ToListAsync();
                
            if (!customers.Any())
            {
                return 0;
            }
            
            foreach (var customer in customers)
            {
                customer.LoyaltyProgramId = null;
                customer.UpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            return customers.Count;
        }
    }
}
