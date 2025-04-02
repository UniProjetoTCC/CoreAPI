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
            CreateMap<ProductModel, ProductBusinessModel>().ReverseMap();
            
            // Product mappings
            CreateMap<ProductBusinessModel, ProductCreateModel>().ReverseMap();
            CreateMap<ProductBusinessModel, ProductUpdateModel>().ReverseMap();
        }
    }
}
