<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("add_del_items") }}</span>
    </div>

    <el-tabs v-model="activeTabName" @tab-change="OnTabChanged">
      <!-- Tab: 工藝類別 -->
      <el-tab-pane :label="t('manufacture_method')" name="manufMehtod">
        <ManufMethodMgmt ref="ManufMethodMgmtRef" />
      </el-tab-pane>

      <!-- Tab: 廠區 -->
      <el-tab-pane :label="t('site')" name="sites">
        <SiteMgmt ref="SiteMgmtRef" />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>
<script lang="ts">
import { defineComponent, reactive, ref, toRefs } from "vue";
import { useI18n } from "vue-i18n";
import ManufMethodMgmt from "@/components/ManufMethodMgmt.vue";
import SiteMgmt from "@/components/SiteMgmt.vue";

export default defineComponent({
  components: {
    ManufMethodMgmt,
    SiteMgmt,
  },
  setup() {
    //#region
    const { t } = useI18n();

    const state = reactive({
      activeTabName: "manufMehtod" as "manufMehtod" | "sites",
    });
    //#endregion

    //#region Ref
    /* 工藝類別 Ref */
    const ManufMethodMgmtRef = ref(ManufMethodMgmt);

    /* 廠區 Ref */
    const SiteMgmtRef = ref(SiteMgmt);
    //#endregion

    //#region UI Events
    const OnTabChanged = () => {
      switch (state.activeTabName) {
        case "manufMehtod":
          /* refresh ManufMethodMgmt */
          ManufMethodMgmtRef.value?.onTabSelected();
          break;
        case "sites":
          /* refresh SiteMgmt */
          SiteMgmtRef.value?.onTabSelected();
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
      ManufMethodMgmtRef,
      SiteMgmtRef,

      /* UI Events */
      OnTabChanged,
    };
  },
});
</script>
