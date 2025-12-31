<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">帳務管理</span>
    </div>
    <el-tabs type="border-card">
      <el-tab-pane label="簽到&關帳" v-if="userInfoStore.userId == 51">
        <ScheduleCheckMgmt />
      </el-tab-pane>
      <el-tab-pane label="查詢所有關帳記錄" v-if="userInfoStore.userId == 51">
        <CloseAccountDataMgmt />
      </el-tab-pane>
      <el-tab-pane label="報表下載" v-if="userInfoStore.userId == 51">
        <ReportMgmt />
      </el-tab-pane>

    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { defineComponent } from "vue";
import { useI18n } from "vue-i18n";
import ScheduleCheckMgmt from "@/components/ScheduleCheckMgmt.vue";
import CloseAccountDataMgmt from "@/components/CloseAccountDataMgmt.vue";
import ReportMgmt from "@/components/ReportMgmt.vue";
import { useUserInfoStore } from "@/stores/UserInfoStore";

const userInfoStore = useUserInfoStore();

defineComponent({
  components: {

  },
});


const { t } = useI18n();


</script>

<style scoped>

</style>
