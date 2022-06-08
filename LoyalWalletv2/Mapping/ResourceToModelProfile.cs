using AutoMapper;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Resources;

namespace LoyalWalletv2.Mapping;

public class ResourceToModelProfile : Profile
{
    public ResourceToModelProfile()
    {
        CreateMap<SaveCustomerResource, Customer>();
    }
}