<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("modify_kaizen_strategy") }}</span>
    </div>

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

    <!-- Btn: 匯出 -->
    <div class="d-flex mb-2 pl-2 justify-content-end">
      <el-button @click="onExportReportClicked" :disabled="displayKaizenList.length == 0">{{ t("export") }}</el-button>
    </div>

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
        v-loading.fullscreen.lock="isTableLoading"
        :data="displayKaizenList"
        :stripe="true"
        @sort-change="onSortChanged"
      >
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
        <el-table-column min-width="120" v-if="colDisplayCtrl[11]" sortable="custom" align="center" prop="hyperlink" :label="t('hyperlink')">
          <template #default="scope">
            <el-button v-if="validHyperlink(scope.row.hyperlink)" link type="primary" @click="onHyperlinkClicked(scope.row.hyperlink)">
              <el-icon :size="16"><Document /></el-icon>
            </el-button>
            <label v-else>{{ scope.row.hyperlink }}</label>
          </template>
        </el-table-column>
        <el-table-column
          min-width="120"
          v-if="colDisplayCtrl[12]"
          sortable="custom"
          prop="procTimeBeforeImp"
          :label="t('Kaizen_Before_Work_Hours')"
        />
        <el-table-column min-width="120" v-if="colDisplayCtrl[13]" sortable="custom" prop="procTimeAfterImp" :label="t('Kaizen_After_Work_Hours')" />
        <el-table-column
          width="120"
          v-if="colDisplayCtrl[14]"
          sortable="custom"
          prop="creatorDisplayName"
          :show-overflow-tooltip="true"
          :label="t('creator')"
        />
        <el-table-column min-width="220" v-if="colDisplayCtrl[15]" sortable="custom" prop="createTime" :label="t('createDate')" />
        <el-table-column max-width="80" :label="t('operation')" align="center" prop="operate" class="operateBtnGroup d-flex" fixed="right">
          <template #default="scope">
            <el-button v-if="userInfoStore.permissions.some((x) => x === 320)" link type="success" @click="onEditKaizenItemClicked(scope.row)" >
              <el-icon :size="16"><EditPen /></el-icon>
            </el-button>
            <el-button v-else link type="primary" @click="onViewKaizenItemClicked(scope.row)">
              <el-icon><ZoomIn /></el-icon>
            </el-button>
          </template>
        </el-table-column>
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

  <!-- 編輯彈窗 -->
  <el-dialog class="kaizen-list-dialog" top="1vh" v-model="isShowModifyDialog" @before-close="onModifyDialogClosing" :title="t('modify')">
    <el-form
      @submit.prevent
      v-loading.fullscreen.lock="isModifyDialogLoading"
      ref="modifyKaizenItemFormRef"
      :model="selectedKaizenItem"
      :rules="modifyKaizenItemFormRules"
      :label-position="formLabelPosition"
      :label-width="formLabelWidth"
    >
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.creatorDisplayName }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.kaizen_code')" prop="kaizenCode" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.kaizenCode }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.project_code')" prop="site" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.projectCode }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.Kaizen_PN')" prop="pn" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.pn }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.team')" prop="teamNum" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.teamNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.work_number')" prop="workNum" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.workNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.work_remark')" prop="workRemark" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.workRemark }}</label>
      </el-form-item>
      <el-form-item
        :label="t('form.label.manufacture_method')"
        prop="manufMethodName"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <label>{{ selectedKaizenItem.manufMethodName }}</label>
      </el-form-item>
      <el-form-item
        :label="t('form.label.hoursAdjustmentDate')"
        prop="implementDay"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <el-date-picker
          class="mx-3 m-sm-0"
          style="width: 150px"
          v-model="selectedKaizenItem.implementDay"
          type="date"
          prop="implementDay"
          value-format="YYYY-MM-DD"
          :clearable="false"
          :placeholder="t('placeholder.date')"
        />
      </el-form-item>
      <el-form-item :label="t('form.label.improvement_type')" prop="impPlan" class="text-nowrap col-12" :label-width="formLabelWidth">
        <el-input class="mx-3 m-sm-0" v-model="selectedKaizenItem.impPlan" clearable :placeholder="t('placeholder.input')" />
      </el-form-item>
      <el-form-item
        :label="t('form.label.improvement_suggestions')"
        prop="impMethod"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <el-input class="mx-3 m-sm-0" v-model="selectedKaizenItem.impMethod" clearable :placeholder="t('placeholder.input')" />
      </el-form-item>
      <el-form-item
        :label="t('form.label.implement_detail')"
        prop="impMethodDetail"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <el-input
          class="mx-3 m-sm-0"
          v-model="selectedKaizenItem.impMethodDetail"
          :autosize="{ minRows: 5 }"
          type="textarea"
          :placeholder="t('placeholder.input')"
        />
      </el-form-item>
      <el-form-item :label="t('form.label.hyperLink')" prop="hyperlink" class="text-nowrap col-12" :label-width="formLabelWidth">
        <el-input class="mx-3 m-sm-0" v-model="selectedKaizenItem.hyperlink" clearable :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifyDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSaveModifyResultClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>

  <!-- 瀏覽彈窗 -->
  <el-dialog class="kaizen-list-dialog" top="1vh" v-model="isShowViewDialog" :title="t('detail')">
    <el-form
      @submit.prevent
      ref="modifyKaizenItemFormRef"
      :model="selectedKaizenItem"
      :label-position="formLabelPosition"
      :label-width="formLabelWidth"
    >
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.creatorDisplayName }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.kaizen_code')" prop="kaizenCode" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.kaizenCode }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.project_code')" prop="site" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.projectCode }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.Kaizen_PN')" prop="pn" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.pn }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.team')" prop="teamNum" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.teamNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.work_number')" prop="workNum" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.workNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.work_remark')" prop="workRemark" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.workRemark }}</label>
      </el-form-item>
      <el-form-item
        :label="t('form.label.manufacture_method')"
        prop="manufMethodName"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <label>{{ selectedKaizenItem.manufMethodName }}</label>
      </el-form-item>
      <el-form-item
        :label="t('form.label.hoursAdjustmentDate')"
        prop="implementDay"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <label>{{ selectedKaizenItem.implementDay }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.improvement_type')" prop="impPlan" class="text-nowrap col-12" :label-width="formLabelWidth">
        <label>{{ selectedKaizenItem.impPlan }}</label>
      </el-form-item>
      <el-form-item
        :label="t('form.label.improvement_suggestions')"
        prop="impMethod"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <label>{{ selectedKaizenItem.impMethod }}</label>
      </el-form-item>
      <el-form-item
        :label="t('form.label.implement_detail')"
        prop="impMethodDetail"
        class="text-nowrap col-12"
        :label-width="formLabelWidth"
      >
        <p class="richtextlabel">{{ selectedKaizenItem.impMethodDetail }}</p>
      </el-form-item>
      <el-form-item :label="t('form.label.hyperLink')" prop="hyperlink" class="text-nowrap col-12" :label-width="formLabelWidth">
        <el-button v-if="validHyperlink(selectedKaizenItem.hyperlink)" link type="primary" @click="onHyperlinkClicked(selectedKaizenItem.hyperlink)">
          <el-icon :size="16"><Document /></el-icon>
        </el-button>
        <label v-else>{{ selectedKaizenItem.hyperlink }}</label>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button type="primary" @click="isShowViewDialog = false">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
</template>
<script lang="ts">
import { defineComponent, reactive, toRefs, ref, onMounted, onActivated } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import _ from "lodash";
import { ElMessage, ElMessageBox, ElNotification } from "element-plus";
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IResKaizenItemDTO from "@/models/dto/IResKaizenItemDTO";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import ElTable from "element-plus/lib/components/table";
/** Utils */
import { delay } from "@/plugins/utility";
import IReqKaizenItemSearchCriteriaDTO from "@/models/dto/IReqKaizenItemSearchCriteriaDTO";
import { DataTableType } from "@/models/enums/DataTableType";
import KaizenItemViewModel from "@/models/KaizenItemViewModel";
import IReqKaizenItemDTO from "@/models/dto/IReqKaizenItemDTO";

export default defineComponent({
  props: {
    pjcode: String,
  },
  setup(props) {
    const { t } = useI18n();
    const userInfoStore = useUserInfoStore();
    const paginatorSetup = usePaginatorSetup();
    const tableType = DataTableType.kaizenItemTable;

    const state = reactive({
      isTableLoading: false,
      dialogTopVh: "1vh",
      formLabelPosition: "right" as "top" | "left" | "right",
      formItemSize: "default" as "default" | "small",
      formLabelWidth: "180px" as string,

      // 工藝類別選項
      allManufMethodList: [] as IResManufMethodDTO[],
      isManufMethodListLoading: false,

      // 改善事項清單
      displayKaizenList: [] as IResKaizenItemDTO[],

      //搜尋條件
      searchCriteria: {
        page: 1,
        pageSize: 20,
        manufMethodIds: [],
      } as IReqKaizenItemSearchCriteriaDTO,

      //匯出用的搜尋條件
      exportCriteria: {} as IReqKaizenItemSearchCriteriaDTO,

      //編輯彈窗
      isShowModifyDialog: false,
      isModifyDialogLoading: false,
      selectedKaizenItem: {} as IResKaizenItemDTO,

      //瀏覽彈窗
      isShowViewDialog: false,

      filteredTotal: 0,
      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });

    //#region 建立表單ref與Validator
    const kaizenItemTableRef = ref<InstanceType<typeof ElTable>>();

    const modifyKaizenItemFormRef = ref();
    const modifyKaizenItemFormRules = ref({
      impPlan: [{ required: true, message: () => t("form.validation_msg.improvement_plan_is_required"), trigger: "blur" }],
      impMethod: [{ required: true, message: () => t("form.validation_msg.improvement_method_is_required"), trigger: "blur" }],
      impMethodDetail: [{ required: true, message: () => t("form.validation_msg.implement_detail_is_required"), trigger: "blur" }],
      implementDay: [{ required: true, message: () => t("form.validation_msg.implement_day_is_required"), trigger: "blur" }],
    });

    const teamNumSearchValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "teamNum";
      // console.log(`validate(uint) : ${source.field}`);
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
      //console.log(`KaizenList v2 onMounted :  props.pjcode=${props.pjcode}`);
      state.searchCriteria.pjcode = props.pjcode;

      //更新使用者設置偏好
      await updateUserPreference();

      //工藝類別選項
      await getAllManufMethods();

      //取得改善事項清單
      await searchKaizenItem();
    });
    onActivated(async () => {
      //console.log(`KaizenList v2 onActivated : props.pjcode=${props.pjcode}`);
      state.searchCriteria.pjcode = props.pjcode;

      //更新使用者設置偏好
      await updateUserPreference();

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
      //console.log("onInputFieldEnterKeyUp");

      /* 取得改善事項清單 */
      await searchKaizenItem();
    };

    /** 改變排序 */
    const onSortChanged = (sort: any) => {
      console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      state.searchCriteria.sortColName = sort.prop;
      state.searchCriteria.sortOrder = sort.order;
      searchKaizenItem();
      console.log("onCurrentPageChanged > scroll to 0,0");
      window.scrollTo(0, 0);
    };

    /** 分頁輸入 */
    const onPagesizeChanged = async () => {
      /* 取得改善事項清單 */
      await searchKaizenItem();
      window.scrollTo(0, 0);
    };

    const onCurrentPageChanged = async () => {
      /* 取得改善事項清單 */
      await searchKaizenItem();
      window.scrollTo(0, 0);
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
    };

    /** 按下編輯按鈕 */
    const onEditKaizenItemClicked = async (target: IResKaizenItemDTO) => {
      try {
        state.isTableLoading = true;

        await GetSelectedItemDetailAsync(target.id);

        setFormStyle();

        state.isShowModifyDialog = true;
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

    /** 按下瀏覽按鈕 */
    const onViewKaizenItemClicked = async (target: IResKaizenItemDTO) => {
      try {
        state.isTableLoading = true;

        await GetSelectedItemDetailAsync(target.id);

        setFormStyle();
        
        state.isShowViewDialog = true;
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
    const onModifyDialogClosing = () => {
      modifyKaizenItemFormRef.value?.clearValidate();
    };

    /** 按下修改存檔按鈕 */
    const onSaveModifyResultClicked = async () => {
      try {
        await modifyKaizenItemFormRef?.value.validate();

        await ElMessageBox.confirm(t("Modify_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });
      } catch (error) {
        console.log(error);
        return;
      }

      try {
        state.isModifyDialogLoading = true;
        const patchedKaizenItem: IReqKaizenItemDTO = {
          impPlan: state.selectedKaizenItem.impPlan,
          impMethod: state.selectedKaizenItem.impMethod,
          impMethodDetail: state.selectedKaizenItem.impMethodDetail,
          implementDay: state.selectedKaizenItem.implementDay,
          hyperlink: state.selectedKaizenItem.hyperlink,

          //下方的項目暫且不允許被修改(保留往後會有修改需求)
          projectCode: state.selectedKaizenItem.projectCode,
          pn: state.selectedKaizenItem.pn,
          workNum: Number(state.selectedKaizenItem.workNum),
          manufMethodId: state.selectedKaizenItem.manufMethodId,
          procTimeAfterImp: Number(state.selectedKaizenItem.procTimeAfterImp),
          customerName: "",
        };
        const modifySiteResponse = await API.patchKaizenItem(state.selectedKaizenItem.id, patchedKaizenItem);
        state.isModifyDialogLoading = false;

        if (modifySiteResponse.data.result !== APIResultCode.success) {
          throw new Error(modifySiteResponse.data.msg);
        }

        state.isShowModifyDialog = false;
        ElMessage({
          showClose: true,
          message: t("modify_success"),
          type: "success",
        });

        /* 刷新資料 */
        state.isTableLoading = true;
        await searchKaizenItem();
        state.isTableLoading = false;
      } catch (error) {
        console.error(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isModifyDialogLoading = false;
        state.isTableLoading = false;
      }
    };

    /** 按下超連結 */
    const onHyperlinkClicked = async (url: string) => {
      window.open(url, "_blank");
    };

    /** 按下匯出按鈕 */
    const onExportReportClicked = async () => {
      try {
        state.isTableLoading = true;
        const exportResponse = await API.exportKaizenItemsBySearchCriteria(state.exportCriteria);
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
        state.isTableLoading = false;
      }
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

    /** 更新所選項目的詳細資料 */
    const GetSelectedItemDetailAsync = async (kaizenId: number) => {
      try {
        const getKaizenItemResponse = await API.getKaizenItemById(kaizenId);
        if (getKaizenItemResponse.data.result !== APIResultCode.success) {
          throw new Error(getKaizenItemResponse.data.msg);
        }

        state.selectedKaizenItem = getKaizenItemResponse.data.content;
      } catch (error) {
        console.log(error);
      }
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

        copySearchCriteria();

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

    /** 備份搜尋條件(供匯出使用) */
    function copySearchCriteria() {
      state.exportCriteria.creator = state.searchCriteria.creator;
      state.exportCriteria.pjcode = state.searchCriteria.pjcode;
      state.exportCriteria.manufMethodIds = state.searchCriteria.manufMethodIds;
      state.exportCriteria.pn = state.searchCriteria.pn;
      state.exportCriteria.teamNum = state.searchCriteria.teamNum;
      state.exportCriteria.startDate = state.searchCriteria.startDate;
      state.exportCriteria.endDate = state.searchCriteria.endDate;
    }

    /** 檢驗輸入的字串是否為合法的URL */
    function validHyperlink(input: string): boolean {
      console.log(`valid hyperlink : ${input}`);
      try {
        new URL(input);
        return true;
      } catch (error) {
        return false;
      }
    }

    /** RWD設置 */
    function setFormStyle() {
      // small
      if (window.innerWidth <= 576) {
        state.formLabelPosition = "right";
        state.formItemSize = "default";
        state.formLabelWidth = "auto";
        state.dialogTopVh = "1vh";
        return;
      }

      // medium
      if (window.innerWidth <= 768) {
        state.formLabelPosition = "right";
        state.formItemSize = "default";
        state.formLabelWidth = "180px";
        state.dialogTopVh = "3vh";
        return;
      }

      state.formLabelPosition = "right";
      state.formItemSize = "default";
      state.formLabelWidth = "180px";
      state.dialogTopVh = "8vh";
    }
    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      userInfoStore,
      t,
      modifyKaizenItemFormRef,
      modifyKaizenItemFormRules,
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
      onColDisplayCtrlPanelHidded,
      onEditKaizenItemClicked,
      onViewKaizenItemClicked,
      onSaveModifyResultClicked,
      onModifyDialogClosing,
      onHyperlinkClicked,
      onExportReportClicked,
      validHyperlink,
    };
  },
});
</script>
<style scoped>
.richtextlabel {
  text-align: left;
  white-space: pre;
}
</style>
