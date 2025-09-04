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

            // Supplier mappings
            CreateMap<SupplierModel, SupplierBusinessModel>().ReverseMap();
            CreateMap<SupplierBusinessModel, SupplierDto>().ReverseMap();
            CreateMap<SupplierCreateModel, SupplierBusinessModel>();
            CreateMap<SupplierUpdateModel, SupplierBusinessModel>();

            // Supplier Price mappings
            CreateMap<SupplierPriceModel, SupplierPriceBusinessModel>().ReverseMap();
            CreateMap<SupplierPriceCreateModel, SupplierPriceBusinessModel>();
            CreateMap<SupplierPriceUpdateModel, SupplierPriceBusinessModel>();
            CreateMap<SupplierPriceBusinessModel, SupplierPriceDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));

        }
    }
}
