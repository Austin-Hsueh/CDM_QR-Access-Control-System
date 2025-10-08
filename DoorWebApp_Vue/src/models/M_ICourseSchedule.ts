export interface M_ICourseSchedule {
  courseId?: number;
  courseName: string;
  teacherName: string;
  classroomId: number;
  classroomName?: string;
  startTime: string;  // ISO 格式: "2025-10-08T10:00:00"
  endTime: string;    // ISO 格式: "2025-10-08T12:00:00"
  studentCount?: number;
  notes?: string;
}
