using System.Net.Http.Json;
using StockReplenishment.Application.DTOs;
using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Web.Services;

public class ReplenishmentApiClient(HttpClient httpClient)
{
    public async Task<PaginatedResultDto<ReplenishmentRequestDto>> GetRequestsAsync(
        ReplenishmentStatus? status,
        RequestPriority? priority,
        int? locationId,
        int page = 1,
        int pageSize = 10)
    {
        var query = $"api/replenishment-requests?page={page}&pageSize={pageSize}";

        if (status.HasValue)
            query += $"&status={status}";

        if (priority.HasValue)
            query += $"&priority={priority}";

        if (locationId.HasValue)
            query += $"&locationId={locationId}";

        return await httpClient.GetFromJsonAsync<PaginatedResultDto<ReplenishmentRequestDto>>(query)
            ?? new PaginatedResultDto<ReplenishmentRequestDto>();
    }

    public async Task<ReplenishmentRequestDto?> GetByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<ReplenishmentRequestDto>($"api/replenishment-requests/{id}");
    }

    public async Task<IReadOnlyCollection<StockLocationDto>> GetLocationsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<StockLocationDto>>("api/replenishment-requests/locations")
            ?? [];
    }

    public async Task<HttpResponseMessage> CreateAsync(CreateReplenishmentRequestDto dto)
    {
        return await httpClient.PostAsJsonAsync("api/replenishment-requests", dto);
    }

    public async Task<HttpResponseMessage> UpdateAsync(int id, UpdateReplenishmentRequestDto dto)
    {
        return await httpClient.PutAsJsonAsync($"api/replenishment-requests/{id}", dto);
    }

    public async Task<HttpResponseMessage> SubmitAsync(int id)
    {
        return await httpClient.PostAsync($"api/replenishment-requests/{id}/submit", null);
    }

    public async Task<HttpResponseMessage> ApproveAsync(int id, ApproveReplenishmentRequestDto dto)
    {
        return await httpClient.PostAsJsonAsync($"api/replenishment-requests/{id}/approve", dto);
    }

    public async Task<HttpResponseMessage> RejectAsync(int id, RejectReplenishmentRequestDto dto)
    {
        return await httpClient.PostAsJsonAsync($"api/replenishment-requests/{id}/reject", dto);
    }

    public async Task<HttpResponseMessage> FulfillAsync(int id, FulfillReplenishmentRequestDto dto)
    {
        return await httpClient.PostAsJsonAsync($"api/replenishment-requests/{id}/fulfill", dto);
    }

    public async Task<StockValidationResultDto> GetValidationAsync(int id)
    {
        var response = await httpClient.GetAsync($"api/replenishment-requests/{id}/validation");

        if (!response.IsSuccessStatusCode)
            return new StockValidationResultDto();

        return await response.Content.ReadFromJsonAsync<StockValidationResultDto>() ?? new StockValidationResultDto();
    }
}
