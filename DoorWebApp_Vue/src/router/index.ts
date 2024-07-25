import { createRouter, createWebHistory, RouteRecordRaw } from "vue-router";
import MainLayout from "@/views/MainLayout.vue";
import PageLogin from "@/views/PageLogin.vue";
import PageReportManufKaizenList from "@/views/PageReportManufKaizenList.vue";
import PageReportPerformance from "@/views/PageReportPerformance.vue";
import PageReportSheet2 from "@/views/PageReportSheet_2.vue";
import PagePeriodMgmt from "@/views/PagePeriodMgmt.vue";
import PageProjectMgmt from "@/views/PageProjectMgmt.vue";
import PageKaizenNew from "@/views/PageKaizenNew_v4.vue";
import PageKaizenList from "@/views/PageKaizenList_v3.vue";
import PageDDLMgmt from "@/views/PageDDLMgmt.vue";
// import PageAccountMgmt from "@/views/PageAccountMgmt.vue";
import PageBillboard from "@/views/PageBillboard.vue";
import PageTopPNInfo from "@/views/PageTopPNInfo.vue";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";

import AccessControl from '@/views/MusicAccessControl.vue';
import QRcode from '@/views/MusicQRcode.vue';
import MusicTemporaryQRcode from '@/views/MusicTemporaryQRcode.vue';
import MusicAccountMgmt from '@/views/MusicAccountMgmt.vue';

const routes: Array<RouteRecordRaw> = [
  {
    path: "/Login",
    name: "login",
    component: PageLogin,
  },
  {
    path: "/",
    name: "root",
    redirect: "/qrcode",
    component: MainLayout,
    children: [
      {
        path: "report_manuf",
        name: "WFSNew",
        component: PageReportManufKaizenList,
      },
      {
        path: "report_performance",
        name: "report_performance",
        component: PageReportPerformance,
      },
      {
        path: "period_mgmt",
        name: "period_mgmt",
        component: PagePeriodMgmt,
      },
      {
        path: "pjcode_mgmt",
        name: "pjcode_mgmt",
        component: PageProjectMgmt,
      },
      {
        path: "kaizen_new",
        name: "kaizen_new",
        component: PageKaizenNew,
      },
      {
        path: "kaizen_list",
        name: "kaizen_list",
        component: PageKaizenList,
        props: true,
        children: [
          {
            path: ":pjcode",
            name: "kaizen_list2",
            component: PageKaizenList,
            props: true,
          },
        ],
      },
      {
        path: "topPNInfo",
        name: "topPNInfo",
        component: PageTopPNInfo,
      },
      {
        path: "ddl_mgmt",
        name: "ddl_mgmt",
        component: PageDDLMgmt,
      },
      // {
      //   path: "account_mgmt",
      //   name: "account_mgmt",
      //   component: PageAccountMgmt,
      // },
      {
        path: "news",
        name: "news",
        component: PageBillboard,
      },
      {
        path: "accesscontrol",
        name: "accesscontrol",
        component: AccessControl,
      },
      {
        path: "qrcode",
        name: "qrcode",
        component: QRcode,
      },
      {
        path: "temporaryqrcode",
        name: "temporaryqrcode",
        component: MusicTemporaryQRcode,
      },
      {
        path: "accountMgmt",
        name: "accountMgmt",
        component: MusicAccountMgmt,
      }
    ],
  },
  // {
  //   path: "/:pathMatch(.*)*",
  //   name: "PageNotFound",
  //   redirect: "/kaizen/list",
  //   component: MainLayout,
  // }
];

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes,
});


async function updateToken() {
  const userInfoStore = useUserInfoStore();

  const tokenFromLocalStorage = localStorage.getItem("accessToken");
  const tokenFromUserInfoStore = userInfoStore.token;
  
  //判斷有無token，若LocalStorage跟memory內皆沒有token > 跳至Login頁
  if(!tokenFromLocalStorage) console.log('tokenFromLocalStorage is false');
  if(!tokenFromUserInfoStore) console.log('tokenFromUserInfoStore is false');
  
  if (!tokenFromLocalStorage && !tokenFromUserInfoStore) {
    router.replace({ path: "/Login" });
    return;
  }

  //若LocalStorage有token, 則優先使用
  if(tokenFromLocalStorage) {
    userInfoStore.token = tokenFromLocalStorage;
  }


  try {
    //更新Token(如果token過期或是原token的使用者被停用的話這邊就會再導回至登入頁)
    console.log(`refresh token`);
    const refreshTokenRes = await API.refreshToken();
    console.log(`refresh token complete`);
    if (refreshTokenRes.data.result !== APIResultCode.success) {
      console.log("refresh token failed");
      router.replace({ path: "/Login" });
      return;
    }

    userInfoStore.setToken(refreshTokenRes.data.content.token, false);
    userInfoStore.userId = refreshTokenRes.data.content.userId;
    userInfoStore.username = refreshTokenRes.data.content.username;
    userInfoStore.displayName = refreshTokenRes.data.content.displayName;
    userInfoStore.permissions = refreshTokenRes.data.content.permissionIds;

    //console.log(`permission setup complete : ${userInfoStore.permissions}`);
  } catch (error) {
    router.replace({ path: "/Login" });
    return;
  }

}


router.beforeEach(async (to, from) => {

  //若路由來源或目的是Login者，則不需做路由權限檢查
  if(to.path === "/Login" || from.path === "/Login") {
    return true;
  }

  //刷新token以檢查路由權限
  await updateToken()
  const userInfoStore = useUserInfoStore();
  const userPermission = userInfoStore.permissions;
  console.log(`router beforeEach:   to:${to.fullPath}, from:${from.fullPath}`);
  console.log(`user : ${userInfoStore.username}`);
  console.log(`permission : ${userInfoStore.permissions}`);
  
  let IsGrant = true;
  switch (to.path) {
    case "/report_performance": //TPS改善成效 - 效益追蹤表
      IsGrant = true;
      break;

    case "/report_manuf": // TPS改善資料庫 - 改善對策查詢
      IsGrant = true;
      break;

    case "/kaizen_new": //資料庫維護 - 新增改善方案
      IsGrant = true;
      break;
    case "/kaizen_list": //資料庫維護 - 改善對策修改
      IsGrant = true;
      break;
    case "/topPNInfo": //資料庫維護 - 水平展開料號
      IsGrant = true;
      break;

    case "/ddl_mgmt": //系統設定 - 新增/刪除項目
      IsGrant = true;
      break;
    case "/period_mgmt": //系統設定 - 帳號設定頁
      IsGrant = true;
      break;
    case "/pjcode_mgmt": //系統設定 - 組別代碼建立
      IsGrant = true;
      break;
    case "/account_mgmt": //系統設定 - 帳號權限管理
      IsGrant = true;
      break;
    case "/news":
      IsGrant = true;
      break;
  }

  if (!IsGrant) {
    router.replace("/Login");
    return false;
  } 
  return true;
});

export default router;
