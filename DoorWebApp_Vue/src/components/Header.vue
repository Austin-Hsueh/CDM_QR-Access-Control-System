<template>
  <div class="d-flex flex-row justify-content-between align-items-center h-100">
    <!-- 左側LOGO區 -->
    <div class="d-flex flex-row align-items-center">
      <div class="hamburger icon-btn" @click="$emit('OnHamburgerClicked')">
        <ion-icon :icon="menuOutline" style="font-size: 1.6rem"></ion-icon>
      </div>

      <!-- <img class="nav-logo mx-2" src="../assets/logo.png" alt="Logo" /> -->
      <span class="nav-title d-none d-sm-inline">門禁</span>
    </div>

    <!-- 右側按鈕區 -->
    <div class="d-flex flex-row align-items-center h-100 me-sm-0 me-md-2">
      <div class="icon-wrap mx-1" @click="onLogoutClicked">
        <ion-icon :icon="personCircleOutline" style="font-size: 26px" title="account"></ion-icon>
      </div>

      <span class="d-none d-sm-inline me-3">{{ userInfoStore.displayName }}</span>
      <span class="d-none d-sm-inline me-3">{{ userInfoStore.qrcode }}</span>

      <el-tooltip content="Language" placement="bottom" effect="light" :disabled="langTooltipDisabled">
        <el-dropdown trigger="click" class="d-flex align-items-center mx-1">
          <div class="icon-wrap icon-btn">
            <ion-icon :icon="globeOutline" style="font-size: 25px"></ion-icon>
          </div>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="onLangSelectChanged(LocaleType.en_us)"> English </el-dropdown-item>
              <el-dropdown-item @click="onLangSelectChanged(LocaleType.zh_tw)"> 繁體中文 </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-tooltip>

      <el-tooltip :content="t('sign_out')" placement="bottom" effect="light">
        <div class="icon-wrap icon-btn mx-1" @click="onLogoutClicked">
          <ion-icon :icon="logOutOutline" style="font-size: 28px"></ion-icon>
        </div>
      </el-tooltip>
    </div>
  </div>
</template>

<script lang="ts">
//import * as api from '@/api/index.js';
import { defineComponent, onMounted, reactive, Ref, ref, toRefs } from "vue";
import { useRouter } from "vue-router";
import i18n from "@/locale";
import API from "@/apis/TPSAPI";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import { IonIcon } from "@ionic/vue";
import { personCircleOutline, globeOutline, logOutOutline, menuOutline } from "ionicons/icons";
import { LocaleType } from "@/models/enums/LocaleType";

//import { useStore} from 'vuex';
export default defineComponent({
  name: "header-component",
  components: { IonIcon },
  emits: ["OnHamburgerClicked"],
  setup() {
    const t = i18n.global.t;
    const router = useRouter();
    const userInfoStore = useUserInfoStore();
    const state = reactive({
      username: "",
      langTooltipDisabled: true,
    });

    //#region 建立表單ref與Validator
    //#endregion

    //#region Hook functions
    onMounted(async () => {
      //套用顯示語系
      let lang = userInfoStore.getLocale();
      userInfoStore.setLocale(lang);
    });
    //#endregion

    //#region UI Events
    /** 按下登出 */
    const onLogoutClicked = async () => {
      try {
        await API.logout();
      } catch (error) {
        console.log(error);
        return;
      }
      userInfoStore.clearToken();

      router.replace({ path: "/Login" });
    };

    /** 選擇語言 */
    const onLangSelectChanged = async (lang: LocaleType) => {
      userInfoStore.setLocale(lang);
    };

    //#endregion

    //#region Private Functions
    //#endregion

    return {
      ...toRefs(state),
      t,
      LocaleType,
      userInfoStore,
      personCircleOutline,
      globeOutline,
      logOutOutline,
      menuOutline,
      onLangSelectChanged,
      onLogoutClicked,
    };
  },
});
</script>

<style scoped>
.nav-logo {
  height: 30px;
}

.nav-title {
  font-size: 1.3rem;
  font-weight: bold;
  color: #606266;
  align-self: flex-end;
}
</style>
