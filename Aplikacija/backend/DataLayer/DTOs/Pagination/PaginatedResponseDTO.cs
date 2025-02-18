namespace DataLayer.DTOs.Pagination;

public class PaginatedResponseDTO<T>
{
    public List<T>? Data { get; set; }
    public long TotalLength { get; set; }
}