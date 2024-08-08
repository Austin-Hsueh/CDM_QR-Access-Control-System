import StudentPermission from "./M_IStudentPermission"

export interface M_IUserList_MTI {
  userId: number;
  username: string;
  displayName: string;
  studentPermissions: StudentPermission[];
}
