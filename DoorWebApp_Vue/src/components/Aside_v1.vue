<template>
  <!-- 側邊欄選單 -->
  <el-menu
    class="asideMenu"
    ref="menu"
    :unique-opened="false"
    :default-active="'/' + route.path.split('/')[1]"
    :default-openeds="openList"
    @select="$emit('OnMenuItemSelect')"
    :router="true"
    background-color="#CCA596"
    text-color="#fff"
    active-text-color="#ffd04b"
  >
    <el-menu-item index="/accesscontrol">
        <el-icon><Calendar /></el-icon>
        <span>{{ t("Access Control") }}</span>
    </el-menu-item>
    <el-menu-item index="/qrcode">
        <el-icon><Menu /></el-icon>
        <span>{{ t("QRcode") }}</span>
    </el-menu-item>
    <el-menu-item index="/temporaryqrcode">
        <el-icon><Menu /></el-icon>
        <span>{{ t("Temporary QRcode") }}</span>
    </el-menu-item>
    <el-menu-item index="/accountMgmt">
        <el-icon><Setting /></el-icon>
        <span>{{ t("Account_Mgmt_Music") }}</span>
    </el-menu-item>
  </el-menu>
</template>

<script lang="ts">
import { defineComponent, onMounted, reactive, Ref, ref, toRefs } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import { useI18n } from "vue-i18n";
import i18n from "@/locale";

export default defineComponent({
  name: "aside-component",
  emits: ["OnMenuItemSelect"],
  setup() {
    //const { t, locale } = useI18n();
    const t = i18n.global.t;
    const router = useRouter();
    const route = useRoute();
    const userInfoStore = useUserInfoStore();
    const state = reactive({
      openList: ["1", "2", "3"],
    });

    return {
      ...toRefs(state),
      t,
      route,
      router,
      userInfoStore,
    };
  },
});
</script>

<style scoped>
.asideMenu {
  height: 100%;
  overflow-y: auto;
  overflow-x: hidden;
  /* flex-grow: 1; */
}
.buttonBlock {
  display: flex;
  height: 70px;
  /* background-color: #EFEFEF ; */
  align-items: center;
  justify-content: center;
}

.el-menu-item-group__title {
  padding: 0px !important;
}
.submenu-item {
  padding-left: 20px !important;
}
.submenu-land,
.menu-land {
  color: #ffd700 !important;
}
.submenu-land.is-active {
  color: #409eff !important;
}
.el-menu-vertical:not(.el-menu--collapse) {
  width: 200px;
}

.main-sidebar .el-menu-item.is-active2 {
  color: #fff !important;
}

.main-sidebar .el-menu-item.is-active2 .iconfont {
  color: #fff !important;
}
</style>
