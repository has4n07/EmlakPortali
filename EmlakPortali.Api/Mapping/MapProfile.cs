using AutoMapper;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models.Entities;

namespace EmlakPortali.Api.Mapping
{
    public class MapProfile : Profile
    {
        public MapProfile() 
        {
            CreateMap<Listing, ListingListItemDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.City.Name))
                .ForMember(dest => dest.DistrictName, opt => opt.MapFrom(src => src.District.Name));
                
            CreateMap<Listing, ListingDetailDto>()
                .IncludeBase<Listing, ListingListItemDto>();

            CreateMap<ListingCreateDto, Listing>();
            CreateMap<ListingUpdateDto, Listing>();
            
            CreateMap<ListingImage, ListingImageDto>();
            CreateMap<ListingImageCreateDto, ListingImage>();
        }
    }
}