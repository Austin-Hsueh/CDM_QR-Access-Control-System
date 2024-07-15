export default interface IPagingDTO<T> {
  totalItems: number;
  totalPages:number;
  pageSize: number;
  pageItems: T[];
}