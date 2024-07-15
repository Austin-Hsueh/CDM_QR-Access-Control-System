<template>
  <div v-loading.fullscreen.lock="isPageLoading" class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start">
      <span class="fs-4 fw-bold content-title">{{ t("TPS_Performance_Tracking") }}</span>
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
      </div>

      <div class="row row-cols-1 row-cols-md-2 row-cols-lg-4">
        <!-- 專案代碼 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('PJCode') + ':'">
            <el-input
              v-model.trim="searchCriteria.projectCodeKeyword"
              @keyup.enter="onInputFieldEnterKeyUp"
              clearable
              :placeholder="t('placeholder.input')"
            />
          </el-form-item>
        </div>

        <!-- 改善料號 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('top_pn') + ':'">
            <el-input v-model.trim="searchCriteria.pnKeyword" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
          </el-form-item>
        </div>

        <!-- 廠區 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('site') + ':'">
            <el-select
              :placeholder="t('placeholder.select')"
              :loading="isSiteLoading"
              :loading-text="t('loading')"
              @visible-change="onSiteListShown"
              v-model="searchCriteria.site"
              value-key="id"
              class="w-100"
              @change="searchCriteria.factory = undefined"
              clearable
            >
              <el-option v-for="site in sites" :key="site.id" :label="site.name" :value="site" />
            </el-select>
          </el-form-item>
        </div>

        <!-- 工廠 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('factory') + ':'">
            <el-select v-model="searchCriteria.factory" value-key="id" class="w-100" clearable :placeholder="t('placeholder.select')">
              <el-option v-for="factory in searchCriteria.site?.factories" :key="factory.id" :label="factory.name" :value="factory" />
            </el-select>
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
    <div class="d-flex mb-2 pl-2 justify-content-end" v-if="userInfoStore.permissions.some(x=> [120].includes(x))">
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
            <el-checkbox v-model="colDisplayCtrl[0]">{{ t("implement_day") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[1]">{{ t("site") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[2]">{{ t("factory") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[3]">{{ t("endCustomer") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[4]">{{ t("period") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[5]">{{ t("startMonth") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[6]">{{ t("endMonth") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[7]">{{ t("PJCode") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[8]">{{ t("top_pn") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[9]">{{ t("payRate") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[10]">{{ t("area_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[11]">{{ t("area_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[12]">{{ t("processing_leadtime_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[13]">{{ t("processing_leadtime_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[14]">{{ t("vender_leadtime_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[15]">{{ t("vender_leadtime_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[16]">{{ t("pph_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[17]">{{ t("pph_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[18]">{{ t("org_std_process_time") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[19]">{{ t("std_process_time") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[20]">{{ t("std_process_time_diff") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[21]">{{ t("std_process_time_diff_ratio") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[22]">{{ t("material_costs") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[23]">{{ t("stock_in_num") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[24]">{{ t("total_workhour") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[25]">{{ t("current_pph") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[26]">{{ t("process_LT") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[27]">{{ t("workhour_performance") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[28]">{{ t("pph_performance") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[29]">{{ t("lt_performance") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[30]">{{ t("vender_lt_performance") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[31]">{{ t("co2_emission") }}</el-checkbox>
          </div>
        </el-popover>
      </div>

      <el-table class="w-100" :data="displayList" :stripe="true" @sort-change="onSortChanged">
        <el-table-column min-width="120" v-if="colDisplayCtrl[0]" prop="implementDay" :label="t('implement_day')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[1]" prop="siteName" :label="t('site')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[2]" prop="factoryName" :label="t('factory')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[3]" prop="customerName" :label="t('endCustomer')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[4]" prop="periodNum" :label="t('period')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[5]" prop="startMonth" :label="t('startMonth')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[6]" prop="endMonth" :label="t('endMonth')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[7]" prop="projectCode" :label="t('PJCode')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[8]" prop="topPN" :label="t('top_pn')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[9]" prop="workRate" :label="t('payRate')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[10]" prop="areaBeforeImp" :label="t('area_before_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[11]" prop="procLTBeforeImp" :label="t('area_after_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[12]" prop="venderLTBeforeImp" :label="t('processing_leadtime_before_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[13]" prop="pphBeforeImp" :label="t('processing_leadtime_after_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[14]" prop="areaAfterImp" :label="t('vender_leadtime_before_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[15]" prop="venderLTAfterImp" :label="t('vender_leadtime_after_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[16]" prop="procLTAfterImp" :label="t('pph_before_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[17]" prop="pphAfterImp" :label="t('pph_after_improvement')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[18]" prop="stdProcTimeOrg" :label="t('org_std_process_time')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[19]" prop="stdProcTime" :label="t('std_process_time')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[20]" prop="stdProcTimeDiff" :label="t('std_process_time_diff')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[21]" prop="stdProcTimeDiffRatio" :label="t('std_process_time_diff_ratio')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[22]" prop="materialCosts" :label="t('material_costs')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[23]" prop="stockInNum" :label="t('stock_in_num')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[24]" prop="totalWorkHour" :label="t('total_workhour')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[25]" prop="currentPPH" :label="t('current_pph')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[26]" prop="procLT" :label="t('process_LT')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[27]" prop工資率="工資率workHourPerf" :label="t('workhour_performance')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[28]" prop="pphPerf" :label="t('pph_performance')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[29]" prop="ltPerf" :label="t('lt_performance')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[30]" prop="venderLTPref" :label="t('vender_lt_performance')" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[31]" prop="co2Emission" :label="t('co2_emission')" />
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
            :page-sizes="pageSize"
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
import { ElMessage, ElNotification } from "element-plus";
import _ from "lodash";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import { DataTableType } from "@/models/enums/DataTableType";
import IResTPSPerformanceDataViewDTO from "@/models/dto/IResTPSPerformanceDataViewDTO";
import IResSiteInfoDTO from "@/models/dto/IResSiteInfoDTO";
import TPSPerformanceSearchCriteria from "@/models/TPSPerformanceSearchCriteria";
import { useUserInfoStore } from "@/stores/UserInfoStore";

export default defineComponent({
  setup() {
    const { t } = useI18n();
    const paginatorSetup = usePaginatorSetup();
    const userInfoStore = useUserInfoStore();
    const tableType = DataTableType.tpsPerformance;
    const PageSizeList = [20, 50, 100];

    const state = reactive({
      isPageLoading: false,
      isSiteLoading: false,

      // pagination
      filteredTotal: 0,
      pageSize: PageSizeList,

      /* Component Data */
      displayList: [] as IResTPSPerformanceDataViewDTO[],
      filterdListLength: 0,

      /* 搜尋條件 Search Condition */
      searchCriteria: new TPSPerformanceSearchCriteria(1, PageSizeList[0]),

      // 匯出用的搜尋條件
      exportCriteria: new TPSPerformanceSearchCriteria(1, PageSizeList[0]),

      //廠區清單
      sites: [] as IResSiteInfoDTO[],

      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });

    //#region Hook functions
    onMounted(async () => {
      PageInit();
    });

    onActivated(async () => {
      PageInit();
    });

    const PageInit = _.throttle(async function () {
      try {
        state.isPageLoading = true;
        await updateUserPreference();
        state.searchCriteria.lastPeriodCnt = 1;
        await updateSearchResultListAsync();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isPageLoading = false;
      }
    }, 100);
    //#endregion

    //#region UI Events

    /** 顯示廠區下拉選單時 */
    const onSiteListShown = async () => {
      try {
        state.isSiteLoading = true;
        await updateSiteList();
      } catch (error) {
        console.log(error);
      } finally {
        state.isSiteLoading = false;
      }
    };

    /** 按下並放開Enter鍵 */
    const onInputFieldEnterKeyUp = async () => {
      await updateSearchResultListAsync();
    };

    /** 按下 清理 搜尋條件 按鈕 */
    const onBtnClearSearchClicked = () => {
      state.searchCriteria.clear();
    };

    /** 按下 搜尋 按鈕 */
    const onBtnSearchTableListClicked = async () => {
      try {
        /* 刷新資料 */
        await updateSearchResultListAsync();
      } catch (error) {
        console.error(error);
      }
    };

    /** 按下匯出按鈕 */
    const onExportReportClicked = async () => {
      try {
        state.isPageLoading = true;
        console.log(state.exportCriteria);
        const exportResponse = await API.exportTPSPerformanceReport(state.exportCriteria.ToReqDTO());
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

    /** 改變排序 */
    const onSortChanged = async (sort: any) => {
      //console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      try {
        state.searchCriteria.sortColName = sort.prop;
        state.searchCriteria.sortOrder = sort.order;
        await updateSearchResultListAsync();
        window.scrollTo(0, 0);
      } catch (error) {
        console.log(error);
      } finally {
        state.isPageLoading = false;
      }
    };

    /** 分頁輸入 */
    const onPagesizeChanged = () => {
      updateSearchResultListAsync();
    };

    const onCurrentPageChanged = () => {
      updateSearchResultListAsync();
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
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

    /** 呼叫API, 更新廠區清單 */
    const updateSiteList = async () => {
      try {
        const getSiteResult = await API.getAllSites();

        if (getSiteResult.data.result !== APIResultCode.success) {
          throw new Error(getSiteResult.data.msg);
        }

        state.sites = getSiteResult.data.content;
      } catch (error) {
        console.log(error);
      }
    };

    /** 更新顯示清單 */
    const updateSearchResultListAsync = async () => {
      try {
        /* Table Loading Flag 開啟 */
        state.isPageLoading = true;

        copySearchCriteria();

        const searchResult = await API.searchTPSPerformanceReport(state.searchCriteria.ToReqDTO());
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
        state.isPageLoading = false;
      }
    };

    /** 備份搜尋條件(供匯出使用) */
    function copySearchCriteria() {
      state.exportCriteria = new TPSPerformanceSearchCriteria(state.searchCriteria.page, state.searchCriteria.pageSize);
      state.exportCriteria.lastPeriodCnt = state.searchCriteria.lastPeriodCnt;
      state.exportCriteria.site = state.searchCriteria.site;
      state.exportCriteria.factory = state.searchCriteria.factory;
      state.exportCriteria.projectCodeKeyword = state.searchCriteria.projectCodeKeyword;
      state.exportCriteria.pnKeyword = state.searchCriteria.pnKeyword;

    }

    //#endregion
    return {
      ...toRefs(state),
      paginatorSetup,
      userInfoStore,
      t,

      onInputFieldEnterKeyUp,
      onExportReportClicked,
      onBtnClearSearchClicked,
      onBtnSearchTableListClicked,
      onSiteListShown,
      onColDisplayCtrlPanelHidded,
      onSortChanged,
      onPagesizeChanged,
      onCurrentPageChanged,
    };
  },
});
</script>
