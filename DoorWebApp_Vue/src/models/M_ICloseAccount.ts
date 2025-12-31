/**
 * 單筆課程簽到狀態
 */
export interface M_IScheduleCheckStatus {
  scheduleId: number;
  studentPermissionId: number;
  studentId: number;
  username: string;
  studentName: string;
  courseName: string;
  classroomName: string;
  scheduleDate: string;  // 格式: "yyyy/MM/dd"
  startTime: string;     // 格式: "HH:mm"
  endTime: string;       // 格式: "HH:mm"
  status: string;        // "已簽到" | "未簽到"
  attendanceId?: number;
  checkedInTime?: string; // ISO 格式
}

/**
 * 關帳 - 日期課表與簽到狀態回應
 */
export interface M_IResDailyScheduleStatus {
  queryDate: string;
  totalSchedules: number;
  checkedInCount: number;
  notCheckedInCount: number;
  scheduleStatuses: M_IScheduleCheckStatus[];
  checkedInSchedules: M_IScheduleCheckStatus[];
  notCheckedInSchedules: M_IScheduleCheckStatus[];
  canCloseAccount: boolean;
}

/**
 * 批量簽到結果
 */
export interface M_ICheckInAllResult {
  date: string;
  totalSchedules: number;
  checkedInCount: number;
  successfulCheckins: number;
}

/**
 * 關帳明細資料
 */
export interface M_ICloseAccountDetail {
  closeDate: string;           // 關帳日期 (datetime)
  yesterdayPettyIncome: number; // 昨日零用金結餘
  businessIncome: number;       // 營業收入（學生學費）
  closeAccountAmount: number;   // 關帳結算金額
  depositAmount: number;        // 提存金額
  pettyIncome: number;          // 零用金結餘
}

/**
 * 儲存關帳請求
 */
export interface M_ISaveCloseAccountRequest {
  date: string;           // 關帳日期 (格式: yyyy-MM-dd)
  depositAmount: number;  // 提存金額
}

/**
 * 建立簽到請求
 */
export interface M_IReqCreateAttendance {
  studentPermissionId: number;  // 學生權限 ID
  attendanceDate: string;       // 簽到日期 (格式: yyyy-MM-dd)
  attendanceType: number;       // 簽到類型 (0=缺席, 1=出席, 2=請假)
  modifiedUserId: number;       // 操作者 ID
}

/**
 * 簽到記錄回應
 */
export interface M_IResAttendance {
  attendanceId: number;
  studentPermissionId: number;
  attendanceDate: string;
  attendanceType: number;       // 0=缺席, 1=出席, 2=請假
  checkInTime?: string;         // HH:mm
}
