export interface PaginatedResponseDTO<T> {
  data: T[];
  totalLength: number;
}