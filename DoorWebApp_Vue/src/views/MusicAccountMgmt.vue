<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("Account_Mgmt_Music") }}</span>
    </div>
    <el-tabs type="border-card">
      <el-tab-pane label="帳號設定">
        <AccountUserMgmtVue ref="AccountUserMgmtRef" />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { defineComponent } from "vue";
import { useI18n } from "vue-i18n";
import AccountUserMgmtVue from "@/components/AccountUserMgmt.vue";


defineComponent({
  components: {
    AccountUserMgmtVue,
  },
});


const { t } = useI18n();


</script>

<style scoped>

</style>
