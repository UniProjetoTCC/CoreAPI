using Business.DataRepositories;
using Business.Models;
using Business.Services.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly IStockRepository _stockRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<PurchaseOrderService> _logger;
        // private readonly AutoMapper.IMapper _mapper; // Removido - Não é necessário aqui

        public PurchaseOrderService(
            IPurchaseOrderRepository poRepository,
            IStockRepository stockRepository,
            ISupplierRepository supplierRepository,
            IProductRepository productRepository,
            ILogger<PurchaseOrderService> logger)
            // AutoMapper.IMapper mapper) // Removido
        {
            _poRepository = poRepository;
            _stockRepository = stockRepository;
            _supplierRepository = supplierRepository;
            _productRepository = productRepository;
            _logger = logger;
            // _mapper = mapper; // Removido
        }

        public async Task<PurchaseOrderBusinessModel?> CreateOrderAsync(PurchaseOrderBusinessModel order, string userId)
        {
            // 1. Validar Fornecedor
            var supplier = await _supplierRepository.GetByIdAsync(order.SupplierId, order.GroupId);
            if (supplier == null || !supplier.IsActive)
                throw new ArgumentException("Fornecedor inválido ou inativo.");

            // 2. Validar Itens e Calcular Total
            decimal totalAmount = 0;
            if (order.Items == null || !order.Items.Any())
                throw new ArgumentException("O pedido deve conter pelo menos um item.");

            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetById(item.ProductId, order.GroupId);
                if (product == null)
                    throw new ArgumentException($"Produto com ID {item.ProductId} não encontrado.");
                
                if(item.Quantity <= 0)
                    throw new ArgumentException($"Quantidade para o produto {product.Name} deve ser positiva.");
                    
                if(item.UnitPrice <= 0)
                    throw new ArgumentException($"Preço unitário para o produto {product.Name} deve ser positivo.");

                item.GroupId = order.GroupId;
                totalAmount += item.Quantity * item.UnitPrice;
            }

            order.TotalAmount = totalAmount;
            order.Status = "Pending"; // Status Inicial
            order.OrderDate = DateTime.UtcNow;

            // 3. Criar Pedido
            return await _poRepository.CreateAsync(order);
        }

        public async Task<PurchaseOrderBusinessModel?> GetOrderByIdAsync(string id, string groupId)
        {
            return await _poRepository.GetByIdAsync(id, groupId);
        }

        public async Task<PurchaseOrderBusinessModel?> CompleteOrderAsync(string id, string groupId, string userId)
        {
            var order = await _poRepository.GetByIdAsync(id, groupId);
            if (order == null) 
                throw new InvalidOperationException("Pedido de compra não encontrado.");

            if (order.Status != "Pending")
                throw new InvalidOperationException($"Não é possível completar um pedido com status '{order.Status}'.");

            if (order.Items == null)
                 throw new InvalidOperationException("Pedido de compra não contém itens.");

            // 1. Adicionar itens ao estoque
            foreach (var item in order.Items)
            {
                await _stockRepository.AddStockAsync(
                    item.ProductId,
                    groupId,
                    item.Quantity,
                    userId,
                    $"Recebimento do Pedido de Compra #{order.OrderNumber}"
                );
            }

            // 2. Atualizar Status do Pedido
            order.Status = "Completed";
            order.DeliveryDate = DateTime.UtcNow;
            
            // CORREÇÃO: Passar 'order' diretamente, sem mapeamento
            return await _poRepository.UpdateAsync(order);
        }

        public async Task<PurchaseOrderBusinessModel?> CancelOrderAsync(string id, string groupId, string userId)
        {
            var order = await _poRepository.GetByIdAsync(id, groupId);
            if (order == null)
                throw new InvalidOperationException("Pedido de compra não encontrado.");
                
            if (order.Status == "Completed" || order.Status == "Cancelled")
                throw new InvalidOperationException($"Não é possível cancelar um pedido com status '{order.Status}'.");

            // 2. Atualizar Status do Pedido
            order.Status = "Cancelled";
            
            // CORREÇÃO: Passar 'order' diretamente, sem mapeamento
            return await _poRepository.UpdateAsync(order);
        }
    }
}