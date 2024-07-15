<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Search Form -->
    <el-form @submit.prevent ref="searchKaizenForm" :model="searchKaizenFormData" :rules="searchKaizenFormRules">
      <el-row class="d-flex justify-content-between mb-3">
        <!-- 工藝類別 -->
        <el-form-item tem :label="t('form.label.WFS')">
          <el-select v-model="searchKaizenFormData.manufactureMethodIds" multiple :placeholder="t('placeholder.select')">
            <el-option v-for="item in allManufMethodList" :key="item.id" :label="item.methodName" :value="item.id" />
          </el-select>
        </el-form-item>
        <!-- /工藝類別 -->

        <!-- 搜尋btn -->
        <el-button class="ms-auto" type="primary" @click="onSearchKaizenClicked()">{{ t("search") }}</el-button>
        <!-- /搜尋btn -->
      </el-row>
      <!-- /Search Form  -->

      <!-- table 改善事項 -->
      <el-row>
        <el-col :span="24">
          <el-table highlight-current-row :data="displayKaizenList" style="width: 100%" height="300" @current-change="onCopyDialogSelectRow">
            <el-table-column :label="t('PJCode')" prop="codePJ" />
            <el-table-column :label="t('Kaizen_PN')" prop="kaizenPN" />
            <el-table-column :label="t('Kaizen')" prop="kaizen" />
            <el-table-column :label="t('Job_Number')" prop="jobNumber" />
            <el-table-column :label="t('Kaizen_Plan')" prop="kaizenPlan" />
            <el-table-column :label="t('Kaizen_Content')" prop="kaizenContent" />
            <el-table-column :label="t('Origin_PPH')" prop="originPPH" />
            <el-table-column :label="t('Kaizen_Before_Work_Hours')" prop="kaizenBeforeWorkHours" />
            <el-table-column :label="t('Kaizen_After_Work_Hours')" prop="KaizenAfterWorkHours" />
          </el-table>
        </el-col>
      </el-row>
      <!-- /table -->

      <!-- pagination -->
      <el-row justify="end" class="mt-3">
        <el-col>
          <div class="demo-pagination-block mt-3 d-flex justify-content-end">
            <el-pagination
              v-model:currentPage="searchKaizenFormData.currentPage"
              v-model:page-size="searchKaizenFormData.pageSize"
              :pager-count="paginatorSetup.pagerCount"
              :layout="paginatorSetup.layout"
              :small="paginatorSetup.small"
              :page-sizes="[20, 50, 100]"
              :total="searchKaizenFormData.filterdListLength"
              @size-change="onPagesizeChanged"
              @current-change="onCurrentPageChanged"
              justify="end"
            />
          </div>
        </el-col>
      </el-row>
      <!-- /pagination -->
    </el-form>
  </div>
</template>
<script lang="ts">
import { defineComponent, reactive, toRefs, ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import _ from "lodash";
import { ElMessage } from "element-plus";
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IResKaizenDTO from "@/models/dto/IResKaizenItemDTO";
import IReqKaizenDTO from "@/models/dto/IReqKaizenItemListDTO";
import { usePaginatorSetup } from "@/stores/PaginatorStore";

export default defineComponent({
  emits: ["onCopyDialogSelectRow"],
  setup(prpos, context) {
    const { t } = useI18n();
    const paginatorSetup = usePaginatorSetup();

    //預設後端分頁參數
    const currentPage = 1;
    const pageSize = 50;
    const filterdListLength = 0;

    const state = reactive({
      // 工藝類別選項
      allManufMethodList: [] as IResManufMethodDTO[],
      // 改善事項清單
      displayKaizenList: [] as IResKaizenDTO[],
      //搜尋改善事項參數
      searchKaizenFormData: {
        //搜尋參數
        codePJ: "",
        team: "",
        kaizenPN: "",
        startDate: "",
        endDate: "",
        manufactureMethodIds: [],
        applicant: "",

        //後端分頁參數
        currentPage: currentPage,
        pageSize: pageSize,
        filterdListLength: filterdListLength,
      } as IReqKaizenDTO,
    });

    // select table row
    const currentRow = ref();

    //#region 建立表單ref與Validator

    /* 自訂檢查: 起始時間不能大於結束時間 */
    const checkStartDateSmallerThanEndDate = (rules: any, value: any, callback: any) => {
      // 調整起始時間或結束時間 有符合 起始時間不能大於結束時間, 如果有警告標示要清除
      if (state.searchKaizenFormData.endDate >= state.searchKaizenFormData.startDate) {
        searchKaizenForm.value.clearValidate("startDate");
        searchKaizenForm.value.clearValidate("endDate");
        callback();
      }

      // 起始時間不能大於結束時間
      if (state.searchKaizenFormData.startDate > state.searchKaizenFormData.endDate) {
        callback(new Error(t("form.validation_msg.startDate_cant_be_bigger_than_endDate")));
      }

      callback();
    };

    /* 搜尋欄位Form 的欄位檢查 */
    const searchKaizenForm = ref();
    const searchKaizenFormRules = ref({
      startDate: [
        {
          validator: checkStartDateSmallerThanEndDate,
          trigger: "blur",
        },
      ],
      endDate: [
        {
          validator: checkStartDateSmallerThanEndDate,
          trigger: "blur",
        },
      ],
    });

    /* 清除檢查 */
    const clearValidator = (Form: any) => {
      Form.value?.clearValidate();
    };

    //#endregion

    //#region Hook functions
    onMounted(async () => {
      //工藝類別選項
      await getAllManufMethods();
      //取得改善事項清單
      await searchKaizenItem();
    });
    //#endregion

    //#region UI Events
    /** 按下清除按鈕 */
    const onClearSearchKaizenClicked = () => {
      /* 重置檢查 */
      clearValidator(searchKaizenForm);
      state.searchKaizenFormData.codePJ = "";
      state.searchKaizenFormData.team = "";
      state.searchKaizenFormData.kaizenPN = "";
      state.searchKaizenFormData.startDate = "";
      state.searchKaizenFormData.endDate = "";
      state.searchKaizenFormData.manufactureMethodIds = [];
      state.searchKaizenFormData.applicant = "";
    };

    /** 按下搜尋按鈕 */
    const onSearchKaizenClicked = async () => {
      try {
        /* 檢查欄位 */
        await searchKaizenForm.value.validate();
        /* 取得改善事項清單 */
        await searchKaizenItem();
      } catch (error) {
        console.error(error);
      }
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
    //#endregion

    // #region select table row
    const onCopyDialogSelectRow = (val: IResKaizenDTO | undefined) => {
      currentRow.value = val;
      console.log("select row", currentRow.value);
      console.log(context);
      context.emit("onCopyDialogSelectRow", val); //第二個參數val(選擇要傳出的值) 將物件傳出去
    };
    //#endregion

    //#region Private Functions
    /** 呼叫API, 取得所有工藝類別 */
    async function getAllManufMethods() {
      try {
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
      }
    }

    /** 呼叫API, 取得所有改善事項清單 */
    async function searchKaizenItem() {
      try {
        const getResult = await API.searchKaizenItem(state.searchKaizenFormData);
        if (getResult.data.result !== APIResultCode.success) {
          throw new Error(getResult.data.msg);
        }

        //取得改善事項清單
        state.displayKaizenList = getResult.data.content;
        //更新分頁資料總數
        state.searchKaizenFormData.filterdListLength = getResult.data.filterdListLength;
      } catch (error: any) {
        console.error(error);
        ElMessage({
          type: "error",
          message: t("errorMessage"),
        });
      }
    }

    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      t,
      searchKaizenForm,
      searchKaizenFormRules,

      // function
      onClearSearchKaizenClicked,
      onSearchKaizenClicked,
      onPagesizeChanged,
      onCurrentPageChanged,
      onCopyDialogSelectRow,
    };
  },
});
</script>
<style scoped>
.el-pagination {
  flex-wrap: wrap;
}
.el-table :deep(.cell) {
  white-space: nowrap;
  text-overflow: unset;
}
</style>
