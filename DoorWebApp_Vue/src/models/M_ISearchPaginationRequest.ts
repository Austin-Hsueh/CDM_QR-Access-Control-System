export default interface SearchPaginationRequest {
  SearchText: string;
  SearchPage: number;
  Page: number;
  type?: number;
  Role: number; // (0=全部, 1=管理者, 2=老師, 3=學生, 4=值班人員)
}
