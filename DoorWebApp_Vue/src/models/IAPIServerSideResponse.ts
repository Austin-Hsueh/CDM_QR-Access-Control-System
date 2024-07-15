export default interface IAPIServerSideResponse<T> {
  result: number,
  msg: string,
  content?: T,

  //Serverside 參數
  currentPage: number,              //目前頁數        
  pageSize: number,                 //一頁顯示幾筆
  filterdListLength: number,        //資料總比數   
}
