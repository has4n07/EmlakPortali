using Microsoft.AspNetCore.OutputCaching;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LookupsController : ControllerBase
{
    private readonly LookupRepository _lookupRepository;

    public LookupsController(LookupRepository lookupRepository)
    {
        _lookupRepository = lookupRepository;
    }

    [HttpGet("cities")]
    [OutputCache(PolicyName = "Lookups")]
    public async Task<DataResult<object>> Cities()
    {
        var data = await _lookupRepository.GetCitiesAsync();
        return new DataResult<object> { Status = true, Message = "OK", Data = data.Select(x => new { x.Id, x.Name }) };
    }

    [HttpGet("cities/{cityId:int}/districts")]
    [OutputCache(PolicyName = "Lookups")]
    public async Task<DataResult<object>> Districts(int cityId)
    {
        var data = await _lookupRepository.GetDistrictsByCityAsync(cityId);
        return new DataResult<object> { Status = true, Message = "OK", Data = data.Select(x => new { x.Id, x.CityId, x.Name }) };
    }
}

