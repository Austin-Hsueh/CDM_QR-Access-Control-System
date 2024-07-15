export default interface IResUserAuthInfoDTO {
  token: string;
  userId: number;
  username: string;
  displayName: string;
  locale: string;
  permissionIds: number[];
  qrcode: string;
}
