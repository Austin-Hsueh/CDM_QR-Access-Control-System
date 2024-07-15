export default interface IKaizenItem {
    id?: number;           
    creator?: string;                  //建立者          
    createDate?: string;               //建立時間
    codePJ?: string;                   //專案代碼
    site?: string;                     //廠區
    factory?: string;                  //工廠
    team?: string;                     //小組
    startMonth?: string;               //開始月份
    endMonth?: string;                 //結束月份
    kaizenPN?: string;                 //改善料號
    bu?: string;                       //BU
    endCustomer?: string;              //終端客戶
    jobNumber?: string;                //作業編號
    kaizenBeforeWorkHours?: number;    //改善前工時
    jobDescription?: string;           //作業說明
    originPPH?: string;                //原PPH
    manufactureMethodId?: number;      //工藝類別Id
    KaizenAfterWorkHours?: number;     //改善後工時
    kaizenPlan?: string;               //改善方案
    kaizenContent?: string;            //對策內容
    kaizen?: string;                   //改善對策
    afterTheAreaIsImproved?: string;   //面積改善後
    areaCost?: string;                 //面積費用(平方米)
    areaBeforeImprovement?: string;    //面積改善前
    supplierAfterImprovement?: string; //改善後供應商L/T
    originalProcess?: string;          //原製程L/T
    originalSystemStandard?: string;   //原系統標工
    improveFormerSuppliers?: string;   //改善前供應商L/T
    hyperLink: string;                //超連結
    hoursAdjustmentDate: string;      //工時調整日期
    applicant?: string;                //申請者       
}
