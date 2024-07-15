<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("news") }}</span>
    </div>

    <div class="row row-cols-1 row-cols-sm-2 row-cols-md-4">
      <el-card 
        class="m-3 p-0" 
        style="width:250px; height:230px"   
        :body-style="{ padding: '0px', width: '100%', height:'100%'}"
        @click="onCardClicked">
        <div class="card-wrap"> 
          <!-- <el-image class="image"  :src="require('@/assets/Sinbon_academy_banner.jpg')" :fit="'cover'" /> -->
          <div class="p-3 d-flex flex-column justify-content-center align-content-center">
            <span class="h4 m-0">{{ t("sinbon_academy") }}</span>
          </div>
        </div>
      </el-card>
    </div>
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
