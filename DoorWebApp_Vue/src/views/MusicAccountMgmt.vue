<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("Account_Mgmt_Music") }}</span>
    </div>
    <el-tabs type="border-card">
      <el-tab-pane :label="t('Account Settings')" >
        <AccountUserMgmtVue ref="AccountUserMgmtRef" v-if="userInfoStore.userId == 51"/>
        <AccountOnerUserMgmt v-else/>
      </el-tab-pane>
      <el-tab-pane :label="t('Courses Settings')" >
        <CoursesMgmt v-if="userInfoStore.userId == 51"/>
      </el-tab-pane>

    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { defineComponent } from "vue";
import { useI18n } from "vue-i18n";
import AccountUserMgmtVue from "@/components/AccountUserMgmt.vue";
import AccountOnerUserMgmt from "@/components/AccountOnerUserMgmt.vue";
import CoursesMgmt from "@/components/CoursesMgmt.vue";
import { useUserInfoStore } from "@/stores/UserInfoStore";

const userInfoStore = useUserInfoStore();

defineComponent({
  components: {
    AccountUserMgmtVue,
    AccountOnerUserMgmt
  },
});


const { t } = useI18n();


</script>

<style scoped>

</style>
