<template>
  <div v-loading.fullscreen.lock="isPageLoading" class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("search_for_kaizen_strategy") }}</span>
    </div>

    <!-- Search Form -->
    <el-form @submit.prevent class="mt-2" :label-position="'right'" :label-width="'auto'">
      <div class="row row-cols-1">
        <!-- 查詢區間 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('form.label.period')">
            <el-radio-group v-model="searchCriteria.lastPeriodCnt">
              <el-radio :label="1">{{ t("last_1_period") }}</el-radio>
              <el-radio :label="2">{{ t("last_2_period") }}</el-radio>
              <el-radio :label="4">{{ t("last_4_period") }}</el-radio>
              <el-radio :label="6">{{ t("last_6_period") }}</el-radio>
              <el-radio :label="10">{{ t("last_10_period") }}</el-radio>
            </el-radio-group>
          </el-form-item>
        </div>

        <!-- 工藝類別 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('form.label.manufacture_method')">
            <el-checkbox class="w-100" v-model="isCheckAll" :indeterminate="isIndeterminate" @change="onCheckAllChanged">{{
              t("select_all")
            }}</el-checkbox>
            <el-checkbox-group class="row row-cols-6" v-model="searchCriteria.manufMethodIds" @change="onManufMethodCheckChanged">
              <el-checkbox v-for="manufMethod in manufMethodList" :key="manufMethod.id" :label="manufMethod.id" name="method">{{
                manufMethod.methodName
              }}</el-checkbox>
            </el-checkbox-group>
          </el-form-item>
        </div>
      </div>
    </el-form>

    <!-- Btn: 清除 搜尋 -->
    <div class="d-flex flex-row justify-content-end">
      <el-button type="" @click="onBtnClearSearchClicked()">{{ t("clear") }}</el-button>
      <el-button type="primary" @click="onBtnSearchTableListClicked()">{{ t("search") }}</el-button>
    </div>

    <hr />

    <!-- Btn Group -->
    <div class="d-flex mb-2 pl-2 justify-content-end" v-if="userInfoStore.permissions.some(x=> [220].includes(x))">
      <el-button @click="onExportReportClicked" :disabled="displayList.length == 0">{{ t("export") }}</el-button>
    </div>

    <!-- Table -->
    <div class="d-flex flex-column">
      <div class="align-self-end">
        <el-popover placement="bottom" @hide="onColDisplayCtrlPanelHidded" trigger="click" :Title="t('column')">
          <template #reference>
            <el-button link>
              <el-icon :size="20"><Operation /></el-icon>
            </el-button>
          </template>
          <div>
            <el-checkbox v-model="colDisplayCtrl[0]">{{ t("period") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[1]">{{ t("site") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[2]">{{ t("WFS") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[3]">{{ t("Kaizen") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[4]">{{ t("Kaizen_Plan") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[5]">{{ t("Kaizen_Content") }}</el-checkbox>
          </div>
        </el-popover>
      </div>

      <el-table class="w-100" v-loading.fullscreen.lock="isTableLoading" :data="displayList" :stripe="true" @sort-change="onSortChanged">
        <el-table-column width="60" v-if="colDisplayCtrl[0]" prop="periodNum" :label="t('period')" />
        <el-table-column width="80" v-if="colDisplayCtrl[1]" prop="siteName" :label="t('site')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[2]" prop="manufMethodName" :label="t('WFS')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[3]" prop="impPlan" :label="t('Kaizen')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[4]" prop="impMethod" :label="t('Kaizen_Plan')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[5]" prop="impMethodDetail" :label="t('Kaizen_Content')" />
      </el-table>
    </div>

    <!-- pagination -->
    <el-row justify="end" class="mt-3">
      <el-col>
        <div class="demo-pagination-block mt-3 d-flex justify-content-end">
          <el-pagination
            v-model:currentPage="searchCriteria.page"
            v-model:page-size="searchCriteria.pageSize"
            :pager-count="paginatorSetup.pagerCount"
            :layout="paginatorSetup.layout"
            :small="paginatorSetup.small"
            :page-sizes="[20, 50, 100]"
            :total="filteredTotal"
            @size-change="onPagesizeChanged"
            @current-change="onCurrentPageChanged"
            justify="end"
          />
        </div>
      </el-col>
    </el-row>
  </div>
</template>
<script lang="ts">
import { defineComponent, reactive, toRefs, onMounted, onActivated } from "vue";
import { useI18n } from "vue-i18n";
import _ from "lodash";
import { ElMessage, ElNotification } from "element-plus";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import IReqReportManufKaizenListSearchCriteriaDTO from "@/models/dto/IReqReportManufKaizenListSearchCriteriaDTO";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import { DataTableType } from "@/models/enums/DataTableType";
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IResKaizenItemDTO from "@/models/dto/IResKaizenItemDTO";
import IResKaizenItemsWithManufMethodDTO from "@/models/dto/IResKaizenItemsWithManufMethodDTO";
import { useUserInfoStore } from "@/stores/UserInfoStore";

export default defineComponent({
  setup() {
    //#region
    const { t } = useI18n();
    const paginatorSetup = usePaginatorSetup();
    const userInfoStore = useUserInfoStore();
    const tableType = DataTableType.kaizenItemsByManufMethod;

    const state = reactive({
      isTableLoading: false,
      isPageLoading: false,
      isCheckAll: true,
      isIndeterminate: false,

      // pagination
      filteredTotal: 0,

      /* Component Data */
      displayList: [] as IResKaizenItemsWithManufMethodDTO[],
      filterdListLength: 0,

      /* 搜尋條件 Search Condition */
      searchCriteria: {
        page: 1,
        pageSize: 20,
      } as IReqReportManufKaizenListSearchCriteriaDTO,

      // 匯出用的搜尋條件
      exportCriteria: {} as IReqReportManufKaizenListSearchCriteriaDTO,

      //工藝類別清單
      manufMethodList: [] as IResManufMethodDTO[],

      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });

    //#endregion
    //#region 建立表單ref與Validator

    //#region Hook functions
    onMounted(async () => {
      PageInit();
    });

    onActivated(async () => {
      PageInit();
    });

    const PageInit = _.throttle(async function () {
      try {
        state.isTableLoading = true;
        await updateUserPreference();
        await updateManufMethodList();
        state.searchCriteria.lastPeriodCnt = 1;
        state.searchCriteria.manufMethodIds = state.manufMethodList.map((x) => x.id);
        await searchKaizenItem();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isTableLoading = false;
      }
    }, 100);
    //#endregion

    //#region UI Events
    /** 是否選擇所有工藝類別 */
    const onCheckAllChanged = (isCheckAll: boolean) => {
      state.searchCriteria.manufMethodIds = isCheckAll ? state.manufMethodList.map((x) => x.id) : [];
      state.isIndeterminate = false;
    };

    /** 異動所選的工藝類別 */
    const onManufMethodCheckChanged = (value: string[]) => {
      const checkedCount = value.length;
      state.isCheckAll = checkedCount === state.manufMethodList.length;
      state.isIndeterminate = checkedCount > 0 && checkedCount < state.manufMethodList.length;
    };
    /** 按下 清理 搜尋條件 按鈕 */
    const onBtnClearSearchClicked = () => {
      state.searchCriteria.lastPeriodCnt = 1;
      state.searchCriteria.manufMethodIds.length = 0;
    };

    /** 按下 搜尋 按鈕 */
    const onBtnSearchTableListClicked = async () => {
      try {
        /* 刷新資料 */
        await searchKaizenItem();
      } catch (error) {
        console.error(error);
      }
    };

    /** 按下匯出按鈕 */
    const onExportReportClicked = async () => {
      try {
        state.isPageLoading = true;
        const exportResponse = await API.exportKaizenItemsByManufMethods(state.exportCriteria);
        if (exportResponse.data.result !== 1) {
          ElNotification({
            title: "錯誤",
            dangerouslyUseHTMLString: true,
            message: t("export_failed") + `((:<br>${exportResponse.data.msg}`,
            type: "error",
          });
          return;
        }

        const FileName = exportResponse.data.content;
        const DownloadReportResponse = await API.downloadReportFile(FileName);

        var blob = new Blob([DownloadReportResponse.data], { type: "application/" + DownloadReportResponse.headers["content-type"] });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement("a");
        document.body.appendChild(a);
        a.setAttribute("style", "display: none");
        a.href = url;
        a.download = FileName;
        a.click();
        window.URL.revokeObjectURL(url);
        a.remove(); // remove the element
      } catch (error) {
        console.log(error);
      } finally {
        state.isPageLoading = false;
      }
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
    };

    const onPagesizeChanged = async () => {
      await searchKaizenItem();
      window.scrollTo(0, 0);
    };

    const onCurrentPageChanged = async () => {
      await searchKaizenItem();
      window.scrollTo(0, 0);
    };

    /** 改變排序 */
    const onSortChanged = async (sort: any) => {
      console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      state.searchCriteria.sortColName = sort.prop;
      state.searchCriteria.sortOrder = sort.order;
      await searchKaizenItem();
      window.scrollTo(0, 0);
    };

    //#endregion

    //#region Private Functions
    /** 更新使用者設置偏好 */
    const updateUserPreference = async () => {
      const getPrefResponse = await API.getTableDisplayPreference(tableType);
      if (getPrefResponse.data.result !== 1) {
        console.log(`updateUserPreference : ${getPrefResponse.data.msg}`);
      }
      state.colDisplayCtrl = getPrefResponse.data.content;
    };

    /** 呼叫API, 取得所有工藝類別 */
    const updateManufMethodList = async () => {
      try {
        const getManufMethodsResponse = await API.getManufMethods();

        if (getManufMethodsResponse.data.result !== APIResultCode.success) {
          throw new Error(getManufMethodsResponse.data.msg);
        }

        state.manufMethodList = getManufMethodsResponse.data.content;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: t("errorMessage"),
        });
      }
    };

    /** 呼叫API, 取得所有改善事項清單 */
    async function searchKaizenItem() {
      try {
        /* Table Loading Flag 開啟 */
        state.isTableLoading = true;

        copySearchCriteria();

        const searchResult = await API.searchKaizenItemByManufMethods(state.searchCriteria);
        if (searchResult.data.result !== APIResultCode.success) {
          throw new Error(searchResult.data.msg);
        }

        //取得改善事項清單
        state.displayList = searchResult.data.content.pageItems;

        //更新分頁資料總數
        state.filteredTotal = searchResult.data.content.totalItems;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: t("errorMessage"),
        });
      } finally {
        state.isTableLoading = false;
      }
    }

    /** 備份搜尋條件(供匯出使用) */
    function copySearchCriteria() {
      state.exportCriteria.lastPeriodCnt = state.searchCriteria.lastPeriodCnt;
      state.exportCriteria.manufMethodIds = state.searchCriteria.manufMethodIds;
    }

    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      userInfoStore,
      t,

      /** UI Events */
      onCheckAllChanged,
      onManufMethodCheckChanged,
      onBtnClearSearchClicked,
      onBtnSearchTableListClicked,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onColDisplayCtrlPanelHidded,
      onExportReportClicked,
    };
  },
});
</script>
<style></style>
