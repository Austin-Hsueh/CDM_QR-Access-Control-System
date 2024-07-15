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
    background-color="#005693"
    text-color="#fff"
    active-text-color="#ffd04b"
  >
  <el-menu-item index="/news">
      <el-icon><MagicStick /></el-icon>
      <span>{{ t("news") }}</span>
    </el-menu-item>
    
    <el-sub-menu index="1">
      <template #title>
        <el-icon><Collection class="m-0" /></el-icon>
        <span>{{ t("TPS_Kaizen_Database") }}</span>
      </template>
      <el-menu-item index="/report_manuf">{{ t("search_for_kaizen_strategy") }}</el-menu-item>
      <el-menu-item index="/report_1">{{t('TPS_Performance_Tracking')}}</el-menu-item>
    </el-sub-menu>

    <el-sub-menu
      index="2"
      :disabled="!userInfoStore.permissions.some((x) => [210, 220, 231, 232, 241, 242, 310, 320, 331, 332, 341, 342].includes(x))"
    >
      <template #title>
        <el-icon><Postcard class="m-0" /></el-icon>
        <span>{{ t("database_maintenance") }}</span>
      </template>
      <el-menu-item index="/period_mgmt" :disabled="!userInfoStore.permissions.some((x) => [210, 220, 231, 232, 241, 242].includes(x))">{{
        t("project_code")
      }}</el-menu-item>
      <el-menu-item index="/pjcode_mgmt" :disabled="!userInfoStore.permissions.some((x) => [210, 220, 231, 232, 241, 242].includes(x))">{{
        t("TPS_team_code")
      }}</el-menu-item>
      <el-menu-item index="/kaizen_new" :disabled="!userInfoStore.permissions.some((x) => x === 320)">
        {{ t("create_kaizen_strategy") }}
      </el-menu-item>
      <el-menu-item index="/kaizen_list" :disabled="!userInfoStore.permissions.some((x) => [310, 331, 332, 341, 342].includes(x))">
        {{ t("modify_kaizen_strategy") }}
      </el-menu-item>
      <el-menu-item index="/topPNInfo" :disabled="!userInfoStore.permissions.some((x) => [210, 220, 231, 232, 241, 242].includes(x))">
        {{ t("create_new_part_no") }}
      </el-menu-item>
    </el-sub-menu>

    <el-sub-menu index="3" :disabled="!userInfoStore.permissions.some((x) => [410, 420, 430, 440, 510, 520, 530, 540].includes(x))">
      <template #title>
        <el-icon><Setting class="m-0" /></el-icon>
        <span>{{ t("System_Settings") }}</span>
      </template>
      <el-menu-item index="/ddl_mgmt" :disabled="!userInfoStore.permissions.some((x) => [410, 420, 430, 440].includes(x))">{{
        t("add_del_items")
      }}</el-menu-item>
      <el-menu-item index="/account_mgmt" :disabled="!userInfoStore.permissions.some((x) => [510, 520, 530, 540].includes(x))">{{
        t("administration_authority")
      }}</el-menu-item>
    </el-sub-menu>

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
