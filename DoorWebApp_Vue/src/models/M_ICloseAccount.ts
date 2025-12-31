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
