using AutoMapper;
using Business.Models;
using CoreAPI.Models;
using Data.Models;

namespace CoreAPI.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserGroupModel, UserGroup>().ReverseMap();
            CreateMap<SubscriptionPlanModel, SubscriptionPlan>().ReverseMap();
            CreateMap<LinkedUserModel, LinkedUser>().ReverseMap();
            CreateMap<BackgroundJobsModel, HangJob>().ReverseMap();

            // Product mappings
            CreateMap<ProductModel, ProductBusinessModel>().ReverseMap();
            CreateMap<ProductBusinessModel, ProductCreateModel>().ReverseMap();
            CreateMap<ProductBusinessModel, ProductUpdateModel>().ReverseMap();

            // Product DTO mapping
            CreateMap<ProductModel, ProductDto>();
            CreateMap<ProductBusinessModel, ProductDto>();

            // Category mappings
            CreateMap<CategoryModel, CategoryBusinessModel>().ReverseMap();

            // Category DTO mappings
            CreateMap<CategoryModel, CategoryDto>();
            CreateMap<CategoryBusinessModel, CategoryDto>();

            // Stock mappings - proper layering: Business to DTO only
            CreateMap<StockBusinessModel, StockDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));

            // Data to Business Model mappings            
            CreateMap<StockModel, StockBusinessModel>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
                .ReverseMap();
            CreateMap<StockMovementModel, StockMovementBusinessModel>()
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
                .ReverseMap();

            // Stock movement mappings - proper layering: Business to DTO only
            CreateMap<StockMovementBusinessModel, StockMovementDto>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Stock != null ? src.Stock.ProductId : string.Empty))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Stock != null && src.Stock.Product != null ? src.Stock.Product.Name : string.Empty));

            // Product Expiration mappings
            CreateMap<ProductExpirationModel, ProductExpirationBusinessModel>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.Stock))
                .ReverseMap();

            CreateMap<ProductExpirationBusinessModel, ProductExpirationDto>();
            CreateMap<CreateProductExpirationModel, ProductExpirationBusinessModel>();
            CreateMap<UpdateProductExpirationModel, ProductExpirationBusinessModel>();

            // Payment Method mappings
            CreateMap<PaymentMethodModel, PaymentMethodBusinessModel>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Active))
                .ReverseMap()
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.IsActive));
            
            CreateMap<PaymentMethodBusinessModel, PaymentMethodResponse>();
            CreateMap<PaymentMethodRequest, PaymentMethodBusinessModel>();

            // Loyalty Program mappings
            CreateMap<LoyaltyProgramModel, LoyaltyProgramBusinessModel>()
                .ReverseMap();
            
            CreateMap<LoyaltyProgramBusinessModel, LoyaltyProgramResponse>();
            CreateMap<LoyaltyProgramRequest, LoyaltyProgramBusinessModel>();

            // Customer mappings
            CreateMap<CustomerModel, CustomerBusinessModel>()
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.IsActive))
                .ReverseMap()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Active));
                
            // Customer DTO mappings
            CreateMap<CustomerBusinessModel, CustomerDto>();
            CreateMap<CustomerModel, CustomerDto>();
            
            // Promotion mappings
            CreateMap<PromotionModel, PromotionBusinessModel>().ReverseMap();
            CreateMap<PromotionBusinessModel, PromotionDto>();
            CreateMap<PromotionModel, PromotionDto>();
            
            // Product in Promotion mapping
            CreateMap<ProductBusinessModel, ProductInPromotionDto>()
                .ForMember(dest => dest.PromotionalPrice, opt => opt.Ignore()); // Will be calculated in controller
                
            // ProductPromotion mappings
            CreateMap<ProductPromotionModel, ProductPromotionBusinessModel>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
                .ForMember(dest => dest.Promotion, opt => opt.MapFrom(src => src.Promotion))
                .ReverseMap();

            // Sale mappings
            CreateMap<SaleItemModel, SaleItemBusinessModel>().ReverseMap();
            CreateMap<SaleModel, SaleBusinessModel>()
                .ForMember(dest => dest.SaleItems, opt => opt.MapFrom(src => src.SaleItems)); // Garante o mapeamento da coleção

            // Mapeamento da Camada de Negócio -> DTO (API)
            CreateMap<SaleItemBusinessModel, SaleItemDto>();
            CreateMap<SaleBusinessModel, SaleDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.SaleItems)); // Mapeia a propriedade SaleItems para Items

            // Supplier mappings
            CreateMap<SupplierModel, SupplierBusinessModel>().ReverseMap();
            CreateMap<SupplierBusinessModel, SupplierDto>();
            CreateMap<CreateSupplierRequest, SupplierBusinessModel>();
            CreateMap<UpdateSupplierRequest, SupplierBusinessModel>();

            // Supplier Price mappings
            CreateMap<SupplierPriceModel, SupplierPriceBusinessModel>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
                .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => src.Supplier))
                .ReverseMap();
            

            CreateMap<SupplierPriceBusinessModel, SupplierPriceDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty));
            
            CreateMap<AddSupplierToProductRequest, SupplierPriceBusinessModel>();
            CreateMap<UpdateSupplierPriceRequest, SupplierPriceBusinessModel>();
        }
    }
}
