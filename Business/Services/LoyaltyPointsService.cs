using Business.DataRepositories;
using Business.Models;
using Business.Services.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services
{
    public class LoyaltyPointsService : ILoyaltyPointsService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILoyaltyProgramRepository _loyaltyProgramRepository;
        private readonly ILogger<LoyaltyPointsService> _logger;

        public LoyaltyPointsService(
            ICustomerRepository customerRepository,
            ILoyaltyProgramRepository loyaltyProgramRepository,
            ILogger<LoyaltyPointsService> logger)
        {
            _customerRepository = customerRepository;
            _loyaltyProgramRepository = loyaltyProgramRepository;
            _logger = logger;
        }

        public async Task<(CustomerBusinessModel Customer, int PointsAdded, decimal ConversionRate)> AddPointsAsync(
            string customerId, 
            decimal amount, 
            string groupId, 
            string? description = null)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, groupId);
            if (customer == null)
            {
                throw new ArgumentException("Cliente não encontrado", nameof(customerId));
            }

            if (!customer.Active)
            {
                throw new InvalidOperationException("Não é possível adicionar pontos a um cliente inativo");
            }

            if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
            {
                throw new InvalidOperationException("Cliente não está vinculado a nenhum programa de fidelidade");
            }

            var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
            if (loyaltyProgram == null)
            {
                throw new InvalidOperationException("O programa de fidelidade vinculado a este cliente não existe mais");
            }

            int pointsToAdd = (int)Math.Floor(amount * 100 / loyaltyProgram.CentsToPoints);

            if (pointsToAdd <= 0)
            {
                throw new ArgumentException($"O valor {amount:C} é muito pequeno para gerar pontos com a taxa de conversão atual (1 ponto por {loyaltyProgram.CentsToPoints/100} unidades monetárias)");
            }

            var updatedCustomer = await _customerRepository.AddPointsAsync(
                id: customerId,
                groupId: groupId,
                points: pointsToAdd,
                description: description ?? $"Compra: {amount:C}");

            if (updatedCustomer == null)
            {
                throw new InvalidOperationException("Falha ao adicionar pontos ao cliente");
            }

            return (updatedCustomer, pointsToAdd, loyaltyProgram.CentsToPoints);
        }

        public async Task<(CustomerBusinessModel Customer, int PointsRemoved, decimal ConversionRate)> RemovePointsAsync(
            string customerId, 
            decimal amount, 
            string groupId, 
            string? description = null)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, groupId);
            if (customer == null)
            {
                throw new ArgumentException("Cliente não encontrado", nameof(customerId));
            }

            if (!customer.Active)
            {
                throw new InvalidOperationException("Não é possível remover pontos de um cliente inativo");
            }

            if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
            {
                throw new InvalidOperationException("Cliente não está vinculado a nenhum programa de fidelidade");
            }

            var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
            if (loyaltyProgram == null)
            {
                throw new InvalidOperationException("O programa de fidelidade vinculado a este cliente não existe mais");
            }

            int pointsToRemove = (int)Math.Floor(amount * 100 / loyaltyProgram.CentsToPoints);

            if (pointsToRemove <= 0)
            {
                throw new ArgumentException($"O valor {amount:C} é muito pequeno para calcular pontos com a taxa de conversão atual (1 ponto por {loyaltyProgram.CentsToPoints/100} unidades monetárias)");
            }

            if (customer.LoyaltyPoints < pointsToRemove)
            {
                throw new InvalidOperationException($"Cliente não possui pontos suficientes. Necessário: {pointsToRemove}, Disponível: {customer.LoyaltyPoints}");
            }

            var updatedCustomer = await _customerRepository.RemovePointsAsync(
                id: customerId,
                groupId: groupId,
                points: pointsToRemove,
                description: description ?? $"Resgate: {amount:C}");

            if (updatedCustomer == null)
            {
                throw new InvalidOperationException("Falha ao remover pontos do cliente");
            }

            return (updatedCustomer, pointsToRemove, loyaltyProgram.CentsToPoints);
        }

        public async Task<(CustomerBusinessModel Customer, int PointsRemoved, decimal ConversionRate)> AdminRemovePointsAsync(
            string customerId, 
            int points, 
            string groupId, 
            string description)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, groupId);
            if (customer == null)
            {
                throw new ArgumentException("Cliente não encontrado", nameof(customerId));
            }

            if (!customer.Active)
            {
                throw new InvalidOperationException("Não é possível remover pontos de um cliente inativo");
            }

            if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
            {
                throw new InvalidOperationException("Cliente não está vinculado a nenhum programa de fidelidade");
            }

            var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
            if (loyaltyProgram == null)
            {
                throw new InvalidOperationException("O programa de fidelidade vinculado a este cliente não existe mais");
            }

            if (customer.LoyaltyPoints < points)
            {
                throw new InvalidOperationException($"Cliente não possui pontos suficientes. Necessário: {points}, Disponível: {customer.LoyaltyPoints}");
            }

            var updatedCustomer = await _customerRepository.RemovePointsAsync(
                id: customerId,
                groupId: groupId,
                points: points,
                description: $"Remoção administrativa: {description}");

            if (updatedCustomer == null)
            {
                throw new InvalidOperationException("Falha ao remover pontos do cliente");
            }

            return (updatedCustomer, points, loyaltyProgram.CentsToPoints);
        }

        public async Task<(CustomerBusinessModel Customer, decimal ConversionRate)> GetPointsBalanceAsync(
            string customerId, 
            string groupId)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, groupId);
            if (customer == null)
            {
                throw new ArgumentException("Cliente não encontrado", nameof(customerId));
            }

            if (!customer.Active)
            {
                throw new InvalidOperationException("Não é possível consultar pontos de um cliente inativo");
            }

            if (string.IsNullOrEmpty(customer.LoyaltyProgramId))
            {
                throw new InvalidOperationException("Cliente não está vinculado a nenhum programa de fidelidade");
            }

            var loyaltyProgram = await _loyaltyProgramRepository.GetByIdAsync(customer.LoyaltyProgramId, groupId);
            if (loyaltyProgram == null)
            {
                throw new InvalidOperationException("O programa de fidelidade vinculado a este cliente não existe mais");
            }

            return (customer, loyaltyProgram.CentsToPoints);
        }
    }
}
