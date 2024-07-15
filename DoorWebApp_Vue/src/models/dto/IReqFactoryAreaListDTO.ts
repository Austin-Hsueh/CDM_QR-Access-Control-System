/** 下拉選單 廠區 列表 */ 
export default interface IReqFactoryAreaListDTO {
  // 搜尋文字
  searchText: string;
  // 每頁資料筆數
  pageSize: number;
  // 目前資料頁碼
  page: number;
}
