export default interface M_IUserinfo {
  token: string | null;
  userId: number;
  username: string;
  displayName: string;
  qrcode: string;
  permissionNames: [];
}
