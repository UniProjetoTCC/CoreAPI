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
        }
    }
}
