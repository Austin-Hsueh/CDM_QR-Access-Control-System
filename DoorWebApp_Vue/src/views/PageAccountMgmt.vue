<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("administration_authority") }}</span>
    </div>

    <el-tabs v-model="activeTabName" @tab-change="OnTabChanged">
      <!-- Tab: 使用者管理 -->
      <el-tab-pane :label="t('user')" name="userAccount">
        <AccountUserMgmtVue ref="AccountUserMgmtRef" />
      </el-tab-pane>

      <!-- Tab: 角色管理 -->
      <el-tab-pane :label="t('role')" name="userRole">
        <AccountRoleMgmtVue ref="AccountRoleMgmtRef" />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<script lang="ts">
import { defineComponent, reactive, ref, toRefs } from "vue";
import { useI18n } from "vue-i18n";
import AccountUserMgmtVue from "@/components/AccountUserMgmt.vue";
import AccountRoleMgmtVue from "@/components/AccountRoleMgmt.vue";

export default defineComponent({
  components: {
    AccountUserMgmtVue,
    AccountRoleMgmtVue,
  },
  setup() {
    const { t } = useI18n();

    const state = reactive({
      activeTabName: "userAccount" as "userAccount" | "userRole",
    });

    //#region Ref
    /* 使用者管理 Ref */
    const AccountUserMgmtRef = ref(AccountUserMgmtVue);

    /* 角色管理 Ref */
    const AccountRoleMgmtRef = ref(AccountRoleMgmtVue);
    //#endregion

    //#region UI Events
    const OnTabChanged = () => {
      switch (state.activeTabName) {
        case "userAccount":
          AccountUserMgmtRef.value?.refreshUserList();
          break;
        case "userRole":
          AccountRoleMgmtRef.value?.refreshRoleList();
          break;
        default:
          break;
      }
    };
    //#endregion

    return {
      ...toRefs(state),
      t,

      /* Ref */
      AccountUserMgmtRef,
      AccountRoleMgmtRef,

      /* UI Events */
      OnTabChanged,
    };
  },
});
</script>
