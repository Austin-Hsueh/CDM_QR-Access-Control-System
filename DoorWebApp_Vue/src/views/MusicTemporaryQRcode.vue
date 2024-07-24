<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("Temporary QRcode") }}</span>
    </div>

    <el-tabs type="border-card">
      <el-tab-pane label="臨時門禁-大門">臨時門禁-大門</el-tab-pane>
    </el-tabs>
  </div>
</template>
<script lang="ts">
import { defineComponent, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";

export default defineComponent({
  setup() {
    const { t, locale } = useI18n();
    const router = useRouter();
    const userInfoStore = useUserInfoStore();

    const onCardClicked = async () => {
      console.log("click");
      const url = "https://academy.sinbon.com/eHRD/eHRDOrg";
      window.open(url, "_blank");
    };

    return {
      t,
      onCardClicked,
    };
  },
});
</script>

<style scoped>
.card-wrap {
  display: grid;
  grid-template-rows: 2fr 1fr;
  width:100%;
  height: 100%;
}

/* .image {
  width: 100%;
  display: block;
} */
.card-wrap:hover {
  cursor: pointer;
}

.card-wrap:hover .image {
  transition: transform 0.2s; /* Animation */
  transform: scale(var(--card-hover-scale));
}

.card-wrap .image {
  grid-row: 1 / 2;
  grid-column: 1 / 1;
  display: block;
}

.card-wrap .content {
  grid-row: 2 / 3;
  grid-column: 1 / 1;
}
</style>
