using EmlakPortali.Api.Data;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Repositories;

public class ListingRepository
{
    private readonly AppDbContext _db;

    public ListingRepository(AppDbContext db)
    {
        _db = db;
    }

    private static IQueryable<Listing> ApplySearch(IQueryable<Listing> query, ListingSearchQueryDto q, bool onlyPublicApprovedActive)
    {
        if (onlyPublicApprovedActive)
        {
            query = query.Where(x => x.IsActive && x.IsApproved);
        }

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var text = q.Q.Trim();
            query = query.Where(x =>
                x.Title.Contains(text) ||
                x.Description.Contains(text));
        }

        if (q.CityId.HasValue) query = query.Where(x => x.CityId == q.CityId.Value);
        if (q.DistrictId.HasValue) query = query.Where(x => x.DistrictId == q.DistrictId.Value);
        if (q.ListingType.HasValue) query = query.Where(x => x.ListingType == q.ListingType.Value);
        if (q.CategoryId.HasValue) query = query.Where(x => x.CategoryId == q.CategoryId.Value);
        if (q.MinPrice.HasValue) query = query.Where(x => x.Price >= q.MinPrice.Value);
        if (q.MaxPrice.HasValue) query = query.Where(x => x.Price <= q.MaxPrice.Value);

        query = q.OrderBy switch
        {
            ListingOrderBy.PriceAsc => query.OrderBy(x => x.Price).ThenByDescending(x => x.Created),
            ListingOrderBy.PriceDesc => query.OrderByDescending(x => x.Price).ThenByDescending(x => x.Created),
            _ => query.OrderByDescending(x => x.Created)
        };

        return query;
    }

    public async Task<List<ListingListItemDto>> GetPublicListAsync(int take = 50)
    {
        take = Math.Clamp(take, 1, 200);

        return await _db.Listings
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsApproved)
            .OrderByDescending(x => x.Created)
            .Select(x => new ListingListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.Price,
                ListingType = x.ListingType,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CityName = x.City.Name,
                DistrictName = x.District.Name,
                CoverImageUrl = x.CoverImageUrl,
                Created = x.Created,
                IsActive = x.IsActive,
                IsApproved = x.IsApproved
            })
            .Take(take)
            .ToListAsync();
    }

    public async Task<PagedResponseDto<ListingListItemDto>> SearchPublicAsync(ListingSearchQueryDto q)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 60);

        var baseQuery = _db.Listings.AsNoTracking();
        baseQuery = ApplySearch(baseQuery, q, onlyPublicApprovedActive: true);

        var total = await baseQuery.CountAsync();

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ListingListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.Price,
                ListingType = x.ListingType,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CityName = x.City.Name,
                DistrictName = x.District.Name,
                CoverImageUrl = x.CoverImageUrl,
                Created = x.Created,
                IsActive = x.IsActive,
                IsApproved = x.IsApproved
            })
            .ToListAsync();

        return new PagedResponseDto<ListingListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<PagedResponseDto<ListingListItemDto>> SearchAdminAsync(ListingSearchQueryDto q, bool? approved = null, bool? isActive = null, Guid? ownerUserId = null)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = _db.Listings.AsNoTracking();
        
        if (ownerUserId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.OwnerUserId == ownerUserId.Value);
        }

        baseQuery = ApplySearch(baseQuery, q, onlyPublicApprovedActive: false);

        if (approved.HasValue) baseQuery = baseQuery.Where(x => x.IsApproved == approved.Value);
        if (isActive.HasValue) baseQuery = baseQuery.Where(x => x.IsActive == isActive.Value);

        var total = await baseQuery.CountAsync();

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ListingListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.Price,
                ListingType = x.ListingType,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CityName = x.City.Name,
                DistrictName = x.District.Name,
                CoverImageUrl = x.CoverImageUrl,
                Created = x.Created,
                IsActive = x.IsActive,
                IsApproved = x.IsApproved
            })
            .ToListAsync();

        return new PagedResponseDto<ListingListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<PagedResponseDto<ListingListItemDto>> SearchMyAsync(Guid ownerUserId, ListingSearchQueryDto q)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 60);

        var baseQuery = _db.Listings
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId);

        baseQuery = ApplySearch(baseQuery, q, onlyPublicApprovedActive: false);

        var total = await baseQuery.CountAsync();

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ListingListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.Price,
                ListingType = x.ListingType,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CityName = x.City.Name,
                DistrictName = x.District.Name,
                CoverImageUrl = x.CoverImageUrl,
                Created = x.Created,
                IsActive = x.IsActive,
                IsApproved = x.IsApproved
            })
            .ToListAsync();

        return new PagedResponseDto<ListingListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public async Task<ListingDetailDto?> GetPublicDetailAsync(int id)
    {
        return await _db.Listings
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsActive && x.IsApproved)
            .Select(x => new ListingDetailDto
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.Price,
                ListingType = x.ListingType,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CityId = x.CityId,
                DistrictId = x.DistrictId,
                CityName = x.City.Name,
                DistrictName = x.District.Name,
                CoverImageUrl = x.CoverImageUrl,
                Created = x.Created,
                Description = x.Description,
                IsApproved = x.IsApproved,
                IsActive = x.IsActive,
                RoomCount = x.RoomCount,
                BathCount = x.BathCount,
                Floor = x.Floor,
                TotalFloors = x.TotalFloors,
                AreaGrossM2 = x.AreaGrossM2,
                AreaNetM2 = x.AreaNetM2,
                AddressLine = x.AddressLine,
                Images = x.Images.Where(i => i.IsActive).OrderBy(i => i.SortOrder).Select(i => new ListingImageDto {
                    Id = i.Id,
                    Url = i.Url,
                    SortOrder = i.SortOrder
                }).ToList(),
                OwnerUserId = x.OwnerUserId,
                OwnerName = x.OwnerUser.FullName,
                OwnerPhone = x.OwnerUser.PhoneNumber ?? "0555 555 5555" // Fallback dummy phone for seed users
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ListingDetailDto?> GetAdminDetailAsync(int id)
    {
        return await _db.Listings
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ListingDetailDto
            {
                Id = x.Id,
                Title = x.Title,
                Price = x.Price,
                ListingType = x.ListingType,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CityId = x.CityId,
                DistrictId = x.DistrictId,
                CityName = x.City.Name,
                DistrictName = x.District.Name,
                CoverImageUrl = x.CoverImageUrl,
                Created = x.Created,
                Description = x.Description,
                IsApproved = x.IsApproved,
                IsActive = x.IsActive,
                RoomCount = x.RoomCount,
                BathCount = x.BathCount,
                Floor = x.Floor,
                TotalFloors = x.TotalFloors,
                AreaGrossM2 = x.AreaGrossM2,
                AreaNetM2 = x.AreaNetM2,
                AddressLine = x.AddressLine,
                Images = x.Images.OrderBy(i => i.SortOrder).Select(i => new ListingImageDto {
                    Id = i.Id,
                    Url = i.Url,
                    SortOrder = i.SortOrder
                }).ToList(),
                OwnerUserId = x.OwnerUserId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Listing?> GetByIdAsync(int id)
    {
        return await _db.Listings
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Listing> CreateAsync(Guid ownerUserId, ListingCreateDto dto)
    {
        var entity = new Listing
        {
            OwnerUserId = ownerUserId,
            ListingType = dto.ListingType,
            CategoryId = dto.CategoryId,
            CityId = dto.CityId,
            DistrictId = dto.DistrictId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            RoomCount = dto.RoomCount,
            BathCount = dto.BathCount,
            Floor = dto.Floor,
            TotalFloors = dto.TotalFloors,
            AreaGrossM2 = dto.AreaGrossM2,
            AreaNetM2 = dto.AreaNetM2,
            AddressLine = dto.AddressLine,
            CoverImageUrl = dto.CoverImageUrl,
            IsApproved = true,
            IsActive = true,
            Created = DateTime.UtcNow
        };

        _db.Listings.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Listing entity, ListingUpdateDto dto, bool isAdmin)
    {
        entity.ListingType = dto.ListingType;
        entity.CategoryId = dto.CategoryId;
        entity.CityId = dto.CityId;
        entity.DistrictId = dto.DistrictId;
        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description.Trim();
        entity.Price = dto.Price;
        entity.RoomCount = dto.RoomCount;
        entity.BathCount = dto.BathCount;
        entity.Floor = dto.Floor;
        entity.TotalFloors = dto.TotalFloors;
        entity.AreaGrossM2 = dto.AreaGrossM2;
        entity.AreaNetM2 = dto.AreaNetM2;
        entity.AddressLine = dto.AddressLine;
        entity.CoverImageUrl = dto.CoverImageUrl;
        entity.IsActive = dto.IsActive;
        entity.Updated = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task ApproveAsync(Listing entity, bool approved)
    {
        entity.IsApproved = approved;
        entity.Updated = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Listing entity)
    {
        _db.Listings.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(Listing entity, bool isActive)
    {
        entity.IsActive = isActive;
        entity.Updated = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<ListingImage> AddImageAsync(Listing listing, ListingImageCreateDto dto)
    {
        var nextSort = dto.SortOrder;
        if (nextSort <= 0)
        {
            nextSort = (listing.Images.Count == 0) ? 1 : listing.Images.Max(x => x.SortOrder) + 1;
        }

        var entity = new ListingImage
        {
            ListingId = listing.Id,
            Url = dto.Url.Trim(),
            SortOrder = nextSort,
            IsActive = true,
            Created = DateTime.UtcNow
        };

        _db.ListingImages.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<ListingImage?> GetImageByIdAsync(int imageId)
    {
        return await _db.ListingImages.FirstOrDefaultAsync(x => x.Id == imageId);
    }

    public async Task UpdateImageAsync(ListingImage image, ListingImageUpdateDto dto)
    {
        image.SortOrder = dto.SortOrder;
        image.IsActive = dto.IsActive;
        image.Updated = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteImageAsync(ListingImage image)
    {
        _db.ListingImages.Remove(image);
        await _db.SaveChangesAsync();
    }

    public async Task IncrementViewCountAsync(int id)
    {
        await _db.Listings
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ViewCount, x => x.ViewCount + 1));
    }

    public async Task<int> GetUserListingCountAsync(Guid userId)
    {
        return await _db.Listings.CountAsync(x => x.OwnerUserId == userId);
    }
}

