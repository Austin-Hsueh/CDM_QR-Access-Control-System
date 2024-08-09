import StudentPermission from "./M_IStudentPermission"

export interface M_IUserList_MTI {
  userId: number;
  username: string;
  displayName: string;
  studentPermissions: StudentPermission[];
}

export interface M_IUsersContent_MTI {
  totalItems: number;
  totalPages: number;
  pageSize: number;
  pageItems: M_IUserList_MTI[];
}
