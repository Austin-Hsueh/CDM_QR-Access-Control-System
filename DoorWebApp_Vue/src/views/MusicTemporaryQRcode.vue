<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title"></span>
    </div>

    <el-tabs type="border-card">
      <el-tab-pane label="臨時門禁設定-大門">
        <div class="d-flex flex-column">
          <div class="col-12 text-start">
            <el-form :inline="true" label-width="100px"  ref="createaddRoleForm" label-position="top" style="margin-top: 15px" v-if="false">
              <el-form-item label="通行日期" prop="username"  >
                <el-date-picker
                  type="daterange"
                  range-separator="To"
                  start-placeholder="Start date"
                  end-placeholder="End date"
                />
              </el-form-item>
              <el-form-item label="通行時間" prop="displayName" >
                <el-time-picker
                  is-range
                  range-separator="至"
                  start-placeholder="開始時間"
                  end-placeholder="結束時間"
                />
              </el-form-item>
              <el-form-item style="margin-top: auto;">
                <el-button type="primary" >{{ t("search") }}</el-button>
              </el-form-item>
            </el-form>
          </div>
        </div>
        <el-divider />
        <div class="d-flex flex-column">
          <div class="col-md-4  col-xs-12 col-sm-12">
            <el-image :src="imageSrc" alt="Base64 Image" />
          </div>
        </div>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<script lang="ts">
import { defineComponent, onMounted, ref, reactive } from "vue";
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

    const state = reactive({
      isShowErrorMsg: false,
    });

    const onCardClicked = async () => {
      console.log("click");
      const url = "https://academy.sinbon.com/eHRD/eHRDOrg";
      window.open(url, "_blank");
    };

    const qrcode = ref('');
    const imageSrc = ref('');

    // Fetch the QR code and set image source
    const getTempQRcode = async () => {
      try {
        const tempQRcodeResponse = await API.getTempDoorCode();
        const tempQRcodeResult = tempQRcodeResponse.data.content;
        qrcode.value = tempQRcodeResult.qrcode || '';
        imageSrc.value = `data:image/png;base64,${qrcode.value}`;
        console.log(qrcode.value);
      } catch (error) {
        console.log(error);
        state.isShowErrorMsg = true;
      }
    };

    // Hook functions
    onMounted(() => {
      getTempQRcode();
    });

    return {
      t,
      onCardClicked,
      imageSrc, // Make sure imageSrc is returned here
      state,
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
