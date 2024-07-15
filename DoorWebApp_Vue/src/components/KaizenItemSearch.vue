<template>
  <!-- Search Form -->
  <el-form
    @submit.prevent
    class="mt-2"
    ref="searchCriteriaFormRef"
    :model="searchCriteria"
    :rules="searchCriteriaFormRules"
    :label-position="'right'"
    :label-width="'auto'"
  >
    <div class="row row-cols-1 row-cols-sm-2 row-cols-lg-2 row-cols-xl-4">
      <!-- Input: 專案代碼 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('form.label.project_code')">
          <el-input v-model="searchCriteria.pjcode" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>
      <!-- Input: 工藝類別 -->
      <div class="col">
        <el-form-item class="w-100" tem :label="t('form.label.WFS')">
          <el-select
            v-model="searchCriteria.manufMethodIds"
            class="w-100"
            multiple
            :loading="isManufMethodListLoading"
            :placeholder="t('placeholder.select')"
            @visible-change="onManufMethodListShown"
          >
            <el-option v-for="item in allManufMethodList" :key="item.id" :label="item.methodName" :value="item.id" />
          </el-select>
        </el-form-item>
      </div>

      <!-- Input: 小組 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('form.label.team')" prop="teamNum">
          <el-input v-model="searchCriteria.teamNum" clearable @keyup.enter="onInputFieldEnterKeyUp" :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>

      <!-- Input: 申請者 -->
      <div class="col">
        <el-form-item class="w-100" tem :label="t('form.label.applicant')">
          <el-input v-model="searchCriteria.creator" clearable @keyup.enter="onInputFieldEnterKeyUp" :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>

      <!-- Input: 改善料號 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('form.label.Kaizen_PN')">
          <el-input v-model="searchCriteria.pn" clearable @keyup.enter="onInputFieldEnterKeyUp" :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>

      <!-- Input: 起始時間 -->
      <div class="col">
        <el-form-item class="w-100" :label="t('form.label.startDate')" prop="startDate">
          <el-date-picker
            style="width: 100%"
            :placeholder="t('placeholder.date')"
            value-format="YYYY-MM-DD"
            type="month"
            v-model="searchCriteria.startDate"
            clearable
            name="startDate"
          />
        </el-form-item>
      </div>

      <!-- Input: 結束時間 -->
      <div class="col">
        <el-form-item class="w-100" tem :label="t('form.label.endDate')" prop="endDate">
          <el-date-picker
            style="width: 100%"
            :placeholder="t('placeholder.date')"
            value-format="YYYY-MM-DD"
            type="month"
            v-model="searchCriteria.endDate"
            clearable
            name="endDate"
          />
        </el-form-item>
      </div>
    </div>
  </el-form>

  <!-- Btn: 清除 搜尋 -->
  <div class="d-flex flex-row justify-content-end">
    <el-button @click="onClearSearchKaizenClicked()">{{ t("clear") }}</el-button>
    <el-button type="primary" @click="onSearchKaizenClicked()">{{ t("search") }}</el-button>
  </div>

  <hr />

  <!-- table -->
  <div class="d-flex flex-column">
    <div class="align-self-end">
      <el-popover placement="bottom" @hide="onColDisplayCtrlPanelHidded" trigger="click" :Title="t('column')">
        <template #reference>
          <el-button link>
            <el-icon :size="20"><Operation /></el-icon>
          </el-button>
        </template>
        <div>
          <el-checkbox v-model="colDisplayCtrl[0]">{{ t("PJCode") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[1]">{{ t("kaizenCode") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[2]">{{ t("Kaizen_PN") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[3]">{{ t("team") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[4]">{{ t("Job_Number") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[5]">{{ t("work_remark") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[6]">{{ t("WFS") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[7]">{{ t("Kaizen_Plan") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[8]">{{ t("Kaizen") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[9]">{{ t("Kaizen_Content") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[10]">{{ t("implement_day") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[11]">{{ t("hyperlink") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[12]">{{ t("Kaizen_Before_Work_Hours") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[13]">{{ t("Kaizen_After_Work_Hours") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[14]">{{ t("creator") }}</el-checkbox>
          <el-checkbox v-model="colDisplayCtrl[15]">{{ t("createDate") }}</el-checkbox>
        </div>
      </el-popover>
    </div>

    <el-table
      ref="kaizenItemTableRef"
      class="w-100"
      type="selection"
      v-loading.fullscreen.lock="isTableLoading"
      :max-height="450"
      :data="displayKaizenList"
      :stripe="true"
      highlight-current-row
      @current-change="onKaizenItemSelectChanged"
      @sort-change="onSortChanged"
    >
      <el-table-column width="70" prop="radio" fixed="left" align="center" :label="t('select')">
        <template #default="scope">
          <el-radio v-model="tableRadio" :label="scope.row.id"><i></i></el-radio>
        </template>
      </el-table-column>
      <el-table-column min-width="140" v-if="colDisplayCtrl[0]" sortable="custom" prop="projectCode" :label="t('PJCode')" />
      <el-table-column min-width="160" v-if="colDisplayCtrl[1]" sortable="custom" prop="kaizenCode" :label="t('kaizenCode')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[2]" sortable="custom" prop="pn" :label="t('Kaizen_PN')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[3]" sortable="custom" prop="teamNum" :label="t('team')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[4]" sortable="custom" prop="workNum" :label="t('Job_Number')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[5]" sortable="custom" prop="workRemark" :label="t('work_remark')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[6]" sortable="custom" prop="manufMethodName" :label="t('WFS')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[7]" sortable="custom" prop="impPlan" :label="t('Kaizen_Plan')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[8]" sortable="custom" prop="impMethod" :label="t('Kaizen')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[9]" sortable="custom" prop="impMethodDetail" :label="t('Kaizen_Content')" />
      <el-table-column min-width="140" v-if="colDisplayCtrl[10]" sortable="custom" prop="implementDay" :label="t('implement_day')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[11]" sortable="custom" prop="hyperlink" :label="t('hyperlink')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[12]" sortable="custom" prop="procTimeBeforeImp" :label="t('Kaizen_Before_Work_Hours')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[13]" sortable="custom" prop="procTimeAfterImp" :label="t('Kaizen_After_Work_Hours')" />
      <el-table-column min-width="120" v-if="colDisplayCtrl[14]" sortable="custom" prop="creatorDisplayName" :label="t('creator')" />
      <el-table-column min-width="220" v-if="colDisplayCtrl[15]" sortable="custom" prop="createTime" :label="t('createDate')" />
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
          :page-sizes="[10, 50, 100]"
          :total="filteredTotal"
          @size-change="onPagesizeChanged"
          @current-change="onCurrentPageChanged"
          justify="end"
        />
      </div>
    </el-col>
  </el-row>
</template>
<script lang="ts">
import { defineComponent, reactive, toRefs, ref, onMounted, onActivated } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import _ from "lodash";
import { ElMessage } from "element-plus";
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IResKaizenItemDTO from "@/models/dto/IResKaizenItemDTO";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import ElTable from "element-plus/lib/components/table";
/** Utils */
import { delay } from "@/plugins/utility";
import IReqKaizenItemSearchCriteriaDTO from "@/models/dto/IReqKaizenItemSearchCriteriaDTO";
import { DataTableType } from "@/models/enums/DataTableType";

export default defineComponent({
  emits: ["KaizenItemSelect"],
  setup(props, context) {
    const { t } = useI18n();
    const paginatorSetup = usePaginatorSetup();
    const tableType = DataTableType.kaizenItemSearchTable;

    const state = reactive({
      isTableLoading: false,
      tableRadio: undefined as number | undefined,

      // 工藝類別選項
      allManufMethodList: [] as IResManufMethodDTO[],
      isManufMethodListLoading: false,

      // 改善事項清單
      displayKaizenList: [] as IResKaizenItemDTO[],

      //搜尋改善事項參數
      searchCriteria: {
        page: 1,
        pageSize: 10,
        manufMethodIds: [],
      } as IReqKaizenItemSearchCriteriaDTO,

      filteredTotal: 0,
      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });

    //#region 建立表單ref與Validator
    const kaizenItemTableRef = ref<InstanceType<typeof ElTable>>();

    const teamNumSearchValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "teamNum";
      //console.log(`validate(uint) : ${source.field}`);
      if (!value) {
        state.searchCriteria[filed] = undefined;
        return;
      }

      var inputNum = Number(value);
      if (Number.isNaN(inputNum)) {
        callback(new Error(t("validation_msg.invalid_uint_value")));
        return;
      }

      if (inputNum <= 0) {
        callback(new Error(t("validation_msg.invalid_uint_value")));
        return;
      }

      state.searchCriteria[filed] = Math.round(inputNum);
      callback();
    };

    const searchCriteriaFormRef = ref();
    const searchCriteriaFormRules = ref({
      teamNum: [{ validator: teamNumSearchValidator, trigger: "blur" }],
    });
    //#endregion

    //#region Hook functions
    onMounted(async () => {
      //工藝類別選項
      await getAllManufMethods();
      //取得改善事項清單
      await searchKaizenItem();
    });
    onActivated(async () => {
      //工藝類別選項
      await getAllManufMethods();
      //取得改善事項清單
      await searchKaizenItem();
    });
    //#endregion

    //#region UI Events
    /** 按下清除按鈕 */
    const onClearSearchKaizenClicked = () => {
      state.searchCriteria.pjcode = undefined;
      state.searchCriteria.creator = undefined;
      state.searchCriteria.teamNum = undefined;
      state.searchCriteria.pn = undefined;
      state.searchCriteria.startDate = undefined;
      state.searchCriteria.endDate = undefined;
      state.searchCriteria.manufMethodIds.length = 0;

      //清除table排序
      kaizenItemTableRef.value?.clearSort();
    };

    /** 顯示工藝類別下拉選單時 */
    const onManufMethodListShown = async () => {
      await getAllManufMethods();
    };

    /** 按下搜尋按鈕 */
    const onSearchKaizenClicked = async () => {
      try {
      

        //清除table排序
        kaizenItemTableRef.value?.clearSort();

        /* 取得改善事項清單 */
        await searchKaizenItem();
      } catch (error) {
        console.error(error);
      }
    };

    /** 按下並放開Enter鍵 */
    const onInputFieldEnterKeyUp = async () => {
      console.log("onInputFieldEnterKeyUp");

      /* 取得改善事項清單 */
      await searchKaizenItem();
    };

    /** 當改善事項被選擇時 */
    const onKaizenItemSelectChanged = async (kaizenItem: IResKaizenItemDTO) => {
      if (!kaizenItem) return;
      console.log(`onKaizenItemSelectChanged.  ${kaizenItem.id}, ${kaizenItem.kaizenCode}`);
      state.tableRadio = kaizenItem.id;
      context.emit("KaizenItemSelect", kaizenItem);
    };

    /** 改變排序 */
    const onSortChanged = (sort: any) => {
      console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      state.searchCriteria.sortColName = sort.prop;
      state.searchCriteria.sortOrder = sort.order;
      searchKaizenItem();
    };

    /** 分頁輸入 */
    const onPagesizeChanged = () => {
      /* 取得改善事項清單 */
      searchKaizenItem();
    };

    const onCurrentPageChanged = () => {
      /* 取得改善事項清單 */
      searchKaizenItem();
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
    };
    //#endregion

    //#region Private Functions
    /** 供外部呼叫 : 當顯示dialog的時候會呼叫此方法，用以重置搜尋結果 */
    const resetSearch = async () => {
      try {
        state.isTableLoading = true;
        await updateUserPreference();
        await getAllManufMethods();
        await searchKaizenItem();
        state.tableRadio = undefined;
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isTableLoading = false;
      }
    };

    /** 更新使用者設置偏好 */
    const updateUserPreference = async () => {
      const getPrefResponse = await API.getTableDisplayPreference(tableType);
      if (getPrefResponse.data.result !== 1) {
        console.log(`updateUserPreference : ${getPrefResponse.data.msg}`);
      }
      state.colDisplayCtrl = getPrefResponse.data.content;
    };

    /** 呼叫API, 取得所有工藝類別 */
    async function getAllManufMethods() {
      try {
        /* Table Loading Flag 開啟 */
        state.isManufMethodListLoading = true;

        /* 若 開發時 等待 1 秒 */
        if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);
        const getResult = await API.getManufMethods();

        if (getResult.data.result !== APIResultCode.success) {
          throw new Error(getResult.data.msg);
        }

        state.allManufMethodList = getResult.data.content;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: t("errorMessage"),
        });
      } finally {
        /* Table Loading Flag 關閉 */
        state.isManufMethodListLoading = false;
      }
    }

    /** 呼叫API, 取得所有改善事項清單 */
    async function searchKaizenItem() {
      try {
        /* 檢查欄位 */
        await searchCriteriaFormRef.value.validate();
      } catch (error) {
        return;
      }

      try {
        /* Table Loading Flag 開啟 */
        state.isTableLoading = true;

        const getResult = await API.searchKaizenItem(state.searchCriteria);
        if (getResult.data.result !== APIResultCode.success) {
          throw new Error(getResult.data.msg);
        }

        //取得改善事項清單
        state.displayKaizenList = getResult.data.content.pageItems;

        //更新分頁資料總數
        state.filteredTotal = getResult.data.content.totalItems;
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
    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      t,
      searchCriteriaFormRef,
      searchCriteriaFormRules,

      // function
      onClearSearchKaizenClicked,
      onSearchKaizenClicked,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onManufMethodListShown,
      onInputFieldEnterKeyUp,
      onKaizenItemSelectChanged,
      onColDisplayCtrlPanelHidded,
      resetSearch,
    };
  },
});
</script>
<style scoped></style>
