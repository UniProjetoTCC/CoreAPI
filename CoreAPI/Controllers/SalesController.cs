using AutoMapper;
using Business.DataRepositories;
using Business.Enums;
using Business.Models;
using Business.Services.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;
        private readonly ISaleRepository _saleRepository;
        private readonly ILinkedUserService _linkedUserService;
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IDistributedCache _distributedCache;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<SalesController> _logger;

        public SalesController(
            ISaleService saleService,
            ISaleRepository saleRepository,
            ILinkedUserService linkedUserService,
            ILinkedUserRepository linkedUserRepository,
            IUserGroupRepository userGroupRepository,
            UserManager<IdentityUser> userManager,
            IDistributedCache distributedCache,
            IMapper mapper,
            ILogger<SalesController> logger)
        {
            _saleService = saleService;
            _saleRepository = saleRepository;
            _linkedUserService = linkedUserService;
            _linkedUserRepository = linkedUserRepository;
            _userGroupRepository = userGroupRepository;
            _distributedCache = distributedCache;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Searches for sales based on a set of filters with caching.
        /// </summary>
        /// <remarks>
        /// This endpoint requires a date range and supports optional filtering by CustomerId, UserId, or PaymentMethodId.
        /// Results are cached for 2 minutes to improve performance on repeated queries.
        /// </remarks>
        /// <param name="request">The search parameters, including a mandatory date range and optional filters.</param>
        /// <response code="200">A paginated list of sales matching the criteria.</response>
        /// <response code="401">Unauthorized if the user is not authenticated.</response>
        /// <response code="403">Forbidden if the user lacks read permissions.</response>
        [HttpGet("Search")]
        [ProducesResponseType(typeof(SaleSearchResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SaleSearchResponse>> Search([FromQuery] SaleSearchRequest request)
        {
            const int cacheDurationMinutes = 2;
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }
        
            string cacheKey = $"sales_search:{groupId}:{request.StartDate:yyyyMMdd}:{request.EndDate:yyyyMMdd}:{request.CustomerId}:{request.UserId}:{request.PaymentMethodId}:{request.Page}:{request.PageSize}";
        
            try
            {
                var cachedResultBytes = await _distributedCache.GetAsync(cacheKey);
                if (cachedResultBytes != null)
                {
                    var cachedResultJson = Encoding.UTF8.GetString(cachedResultBytes);
                    var cachedResponse = JsonSerializer.Deserialize<SaleSearchResponse>(cachedResultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cachedResponse != null) return Ok(cachedResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing cached sales search for key {CacheKey}", cacheKey);
            }
        
            var (sales, totalCount, totalAmount) = await _saleRepository.SearchSalesAsync(
                groupId, request.StartDate, request.EndDate, request.CustomerId,
                request.UserId, request.PaymentMethodId, request.Page, request.PageSize);
        
            var response = new SaleSearchResponse
            {
                Items = _mapper.Map<List<SaleDto>>(sales),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Pages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                TotalAmount = totalAmount
            };
        
            try
            {
                var serializedResponse = JsonSerializer.Serialize(response);
                var cacheOptions = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(cacheDurationMinutes));
                await _distributedCache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(serializedResponse), cacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching sales search result for key {CacheKey}", cacheKey);
            }
        
            return Ok(response);
        }

        /// <summary>
        /// Gets a specific sale by its ID.
        /// </summary>
        /// <remarks>
        /// Retrieves the full details of a single sale, including all its items.
        /// The sale must belong to the user's group.
        /// </remarks>
        /// <param name="id">The unique identifier of the sale.</param>
        /// <response code="200">Returns the requested sale details.</response>
        /// <response code="401">Unauthorized if the user is not authenticated.</response>
        /// <response code="403">Forbidden if the user lacks read permissions.</response>
        /// <response code="404">Not Found if the sale does not exist or does not belong to the user's group.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleDto>> GetById(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var sale = await _saleService.GetByIdAsync(id, groupId);
            if (sale == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<SaleDto>(sale));
        }

        /// <summary>
        /// Simulates a sale to calculate totals, check stock, and list available promotions.
        /// </summary>
        /// <remarks>
        /// Use this endpoint to get a preview of a sale before processing payment. No data is saved.
        /// You can optionally pass an `appliedPromotionId` for any item to see its specific impact.
        /// If no promotion is specified for an item, the best available one is applied by default.
        /// </remarks>
        /// <param name="request">The object containing cart items, customer, payment method, and optional promotion choices.</param>
        /// <response code="200">A detailed checkout response with totals, promotions, and stock status.</response>
        /// <response code="400">Bad Request if input data is invalid (e.g., product not found).</response>
        /// <response code="401">Unauthorized if the user is not authenticated.</response>
        /// <response code="403">Forbidden if the user lacks transaction permissions.</response>
        [HttpPost("Checkout")]
        [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                var (hasPermission, groupId) = await CheckSalesPermissionAsync(currentUser.Id);
                if (!hasPermission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }

                var checkoutResponse = await _saleService.CheckoutAsync(request, currentUser.Id, groupId);
                return Ok(checkoutResponse);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument during checkout.");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during checkout.");
                return StatusCode(500, new { message = "An internal error occurred. Please try again later." });
            }
        }

        /// <summary>
        /// Completes and saves a new sale in the system.
        /// </summary>
        /// <remarks>
        /// Call this endpoint AFTER the checkout has been validated and payment has been processed.
        /// It creates the sale record, decreases stock, and adds loyalty points based on the final, validated data.
        /// </remarks>
        /// <param name="request">The final sale object, including the chosen promotion ID for each item.</param>
        /// <response code="201">Returns the created sale object.</response>
        /// <response code="400">Bad Request if there is insufficient stock or other invalid data.</response>
        /// <response code="401">Unauthorized if the user is not authenticated.</response>
        /// <response code="403">Forbidden if the user lacks transaction permissions.</response>
        [HttpPost]
        [ProducesResponseType(typeof(SaleBusinessModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompleteSale([FromBody] SaleRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                var (hasPermission, groupId) = await CheckSalesPermissionAsync(currentUser.Id);
                if (!hasPermission)
                {
                    return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
                }

                var sale = await _saleService.CompleteSaleAsync(request, currentUser.Id, groupId);
                
                return StatusCode(StatusCodes.Status201Created, sale);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while completing the sale (likely stock issue).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while completing the sale.");
                return StatusCode(500, new { message = "An internal error occurred. Please try again later." });
            }
        }

        /// <summary>
        /// Deletes a sale, restores stock, and reverses loyalty points.
        /// </summary>
        /// <remarks>
        /// This action is irreversible. It restores the stock quantity of all items in the sale
        /// and attempts to remove any loyalty points that were awarded.
        /// </remarks>
        /// <param name="id">The ID of the sale to delete.</param>
        /// <response code="200">Returns the data of the deleted sale.</response>
        /// <response code="401">Unauthorized if the user is not authenticated.</response>
        /// <response code="403">Forbidden if the user lacks transaction permissions.</response>
        /// <response code="404">Not Found if the sale does not exist.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(SaleBusinessModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteSale(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var (hasPermission, groupId) = await CheckSalesPermissionAsync(currentUser.Id);
            if (!hasPermission)
            {
                return StatusCode(403, "You don't have permission to access this resource. Talk to your access manager to get the necessary permissions.");
            }

            var deletedSale = await _saleService.DeleteSaleAsync(id, groupId);
            if (deletedSale == null)
            {
                return NotFound();
            }

            return Ok(deletedSale);
        }

        private async Task<(bool hasPermission, string groupId)> CheckPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                groupId = linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                var group = await _userGroupRepository.GetByUserIdAsync(userId);
                groupId = group?.GroupId ?? string.Empty;
            }

            return (true, groupId);
        }

        private async Task<(bool hasPermission, string groupId)> CheckSalesPermissionAsync(string userId)
        {
            string groupId;

            if (await _linkedUserService.IsLinkedUserAsync(userId))
            {
                bool permission = await _linkedUserService.HasPermissionAsync(userId, LinkedUserPermissionsEnum.Transaction);

                if (!permission)
                {
                    return (false, string.Empty);
                }

                // Get the group ID from the linked user's main account
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                groupId = linkedUser?.GroupId ?? string.Empty;
            }
            else
            {
                // It's a main user, get the group ID directly
                var group = await _userGroupRepository.GetByUserIdAsync(userId);
                groupId = group?.GroupId ?? string.Empty;
            }

            return (true, groupId);
        }
    }
}