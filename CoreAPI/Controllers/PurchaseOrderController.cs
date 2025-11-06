using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Models;
using Business.Services.Base;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly IPurchaseOrderService _poService;
        private readonly IPurchaseOrderRepository _poRepository; // Para buscas
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PurchaseOrderController> _logger;

        public PurchaseOrderController(
            IPurchaseOrderService poService,
            IPurchaseOrderRepository poRepository,
            UserManager<IdentityUser> userManager,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository,
            IMapper mapper,
            ILogger<PurchaseOrderController> logger)
        {
            _poService = poService;
            _poRepository = poRepository;
            _userManager = userManager;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _userGroupRepository = userGroupRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Busca Pedidos de Compra com filtros e paginação
        /// </summary>
        [HttpGet("Search")]
        [ProducesResponseType(typeof(PurchaseOrderSearchResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Search([FromQuery] PurchaseOrderSearchRequest request)
        {
            var (hasPermission, groupId) = await CheckPermissionAsync(LinkedUserPermissionsEnum.Product); // Requer permissão de Produto/Estoque
            if (!hasPermission) return Forbid();

            var (items, totalCount) = await _poRepository.SearchAsync(
                groupId, request.StartDate, request.EndDate, request.SupplierId, request.Status, request.Page, request.PageSize);

            var response = new PurchaseOrderSearchResponse
            {
                Items = _mapper.Map<List<PurchaseOrderDto>>(items),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Pages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                TotalAmountSum = items.Sum(po => po.TotalAmount)
            };
            
            return Ok(response);
        }

        /// <summary>
        /// Busca um Pedido de Compra pelo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(string id)
        {
            var (hasPermission, groupId) = await CheckPermissionAsync(LinkedUserPermissionsEnum.Product);
            if (!hasPermission) return Forbid();

            var order = await _poService.GetOrderByIdAsync(id, groupId);
            if (order == null) return NotFound();

            return Ok(_mapper.Map<PurchaseOrderDto>(order));
        }

        /// <summary>
        /// Cria um novo Pedido de Compra
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
        {
            var (hasPermission, groupId, userId) = await CheckPermissionAndGetIdsAsync(LinkedUserPermissionsEnum.Product);
            if (!hasPermission) return Forbid();
            
            // Validar OrderNumber duplicado
            var existing = await _poRepository.GetByOrderNumberAsync(request.OrderNumber, groupId);
            if(existing != null)
                return Conflict($"Um Pedido de Compra com o número '{request.OrderNumber}' já existe.");

            try
            {
                var orderBusinessModel = _mapper.Map<PurchaseOrderBusinessModel>(request);
                orderBusinessModel.GroupId = groupId;

                var createdOrder = await _poService.CreateOrderAsync(orderBusinessModel, userId);
                var dto = _mapper.Map<PurchaseOrderDto>(createdOrder);

                return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar Pedido de Compra");
                return StatusCode(500, "Erro interno ao criar pedido.");
            }
        }

        /// <summary>
        /// Marca um Pedido de Compra como "Completed" e adiciona os itens ao estoque
        /// </summary>
        [HttpPatch("{id}/Complete")]
        [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteOrder(string id)
        {
            var (hasPermission, groupId, userId) = await CheckPermissionAndGetIdsAsync(LinkedUserPermissionsEnum.Stock); // Requer permissão de Estoque
            if (!hasPermission) return Forbid();

            try
            {
                var order = await _poService.CompleteOrderAsync(id, groupId, userId);
                return Ok(_mapper.Map<PurchaseOrderDto>(order));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao completar Pedido de Compra {Id}", id);
                return StatusCode(500, "Erro interno ao completar pedido.");
            }
        }
        
        /// <summary>
        /// Marca um Pedido de Compra como "Cancelled"
        /// </summary>
        [HttpPatch("{id}/Cancel")]
        [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var (hasPermission, groupId, userId) = await CheckPermissionAndGetIdsAsync(LinkedUserPermissionsEnum.Product);
            if (!hasPermission) return Forbid();

            try
            {
                var order = await _poService.CancelOrderAsync(id, groupId, userId);
                return Ok(_mapper.Map<PurchaseOrderDto>(order));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar Pedido de Compra {Id}", id);
                return StatusCode(500, "Erro interno ao cancelar pedido.");
            }
        }

        // --- Métodos de Permissão ---

        private async Task<(bool hasPermission, string groupId)> CheckPermissionAsync(LinkedUserPermissionsEnum permission)
        {
            var (hasPermission, groupId, _) = await CheckPermissionAndGetIdsAsync(permission);
            return (hasPermission, groupId);
        }

        private async Task<(bool hasPermission, string groupId, string userId)> CheckPermissionAndGetIdsAsync(LinkedUserPermissionsEnum permission)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return (false, string.Empty, string.Empty);

            string groupId;
            string userId = currentUser.Id;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool hasPerm = await _linkedUserService.HasPermissionAsync(userId, permission);
                if (!hasPerm)
                    return (false, string.Empty, userId);

                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                groupId = linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(userId);
                groupId = group?.GroupId ?? string.Empty;
            }

            return (!string.IsNullOrEmpty(groupId), groupId, userId);
        }
    }
}