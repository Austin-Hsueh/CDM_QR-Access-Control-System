<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">關帳管理</span>
    </div>
    <el-tabs type="border-card">
      <el-tab-pane label="簽到狀態" v-if="userInfoStore.userId == 51">
        <ScheduleCheckMgmt />
      </el-tab-pane>

    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { defineComponent } from "vue";
import { useI18n } from "vue-i18n";
import ScheduleCheckMgmt from "@/components/ScheduleCheckMgmt.vue";
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
