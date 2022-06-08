using AutoMapper;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Resources;

namespace LoyalWalletv2.Mapping;

public class ModelToResourceProfile : Profile
{
    public ModelToResourceProfile()
    {
        CreateMap<Customer, CustomerResource>();
    }
}