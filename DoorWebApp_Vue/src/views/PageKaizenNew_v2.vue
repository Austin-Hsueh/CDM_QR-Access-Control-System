<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3" style="max-width: 1000px">
    <!-- Title -->
    <div class="text-start">
      <span class="fs-4 fw-bold content-title">{{ t("Kaizen_New") }}</span>
    </div>

    <!-- 複製、清除btn -->
    <div class="d-flex align-items-center justify-content-end ms-auto col-12 mt-xl-4 mt-lg-0 mt-md-0 mt-sm-2 mt-xs-2 mt-2">
      <el-button type="success" plain @click="onCopyBtnClicked()">{{ t("copy") }}</el-button>
      <el-button type="" @click="onClearFormClicked()">{{ t("clear") }}</el-button>
    </div>

    <el-divider />

    <!-- form -->
    <el-form
      @submit.prevent
      ref="newKaizenFormRef"
      v-loading.fullscreen.lock="isFormLoading"
      :model="addForm"
      :rules="newKaizenFormRules"
      :label-position="formLabelPosition"
    >
      <!-- 基本資訊 -->
      <div class="row row-cols-sm-2 row-cols-1 mb-5">
        <el-form-item class="col mb-0 mb-sm-auto" :size="formItemSize" prop="creator" :label="t('form.label.creator')">
          <label>{{ userInfoStore.displayName }}</label>
        </el-form-item>
        <el-form-item class="col mb-0 mb-sm-auto" :size="formItemSize" prop="createDate" :label="t('form.label.createDate')">
          <label>{{ today }}</label>
        </el-form-item>
      </div>

      <!-- 所屬專案  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("project") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 專案代碼 -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="projectCode" :label="t('form.label.project_code')">
          <el-input
            style="width: 250px"
            v-model="addForm.projectCode"
            :formatter="(value: any) => { return value.pjCode}"
            clearable
            readonly
            @click="onPJCodeInputFieldClicked"
            :placeholder="t('placeholder.input')"
          >
            <template #append>
              <el-button :icon="Search" @click="onSearchProjectBtnClicked" />
            </template>
          </el-input>
        </el-form-item>

        <!-- 廠區 -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="site" :label="t('form.label.site')">
          <label>{{ addForm.project?.siteName }}</label>
        </el-form-item>

        <!-- 工廠 -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="factory" :label="t('form.label.factory')">
          <label>{{ addForm.project?.factoryName }}</label>
        </el-form-item>

        <!-- 小組 -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="team" :label="t('form.label.team')">
          <label>{{ addForm.project?.teamNum }}</label>
        </el-form-item>

        <!-- 開始月份 -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="startMonth" :label="t('form.label.start_month')">
          <label>{{ addForm.project?.startMonth }}</label>
        </el-form-item>

        <!-- 結束月份 -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="endMonth" :label="t('form.label.end_month')">
          <label>{{ addForm.project?.endMonth }}</label>
        </el-form-item>
      </div>

      <!-- 作業料號  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("process_part_no") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 改善料號 -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="apsPNInfo" :label="t('form.label.Kaizen_PN')">
          <el-select
            style="width: 250px"
            v-model="addForm.apsPNInfo"
            filterable
            remote
            reserve-keyword
            default-first-option
            :placeholder="t('placeholder.input')"
            :remote-method="onPNSuggestionListShown"
            :loading="isPNListLoading"
            value-key="pn"
            @change="onPNSelectChanged"
          >
            <el-option v-for="item in candidatePN" :key="item.pn" :label="item.pn" :value="item" />
          </el-select>
        </el-form-item>

        <!-- 作業編號 -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="workNum" :label="t('form.label.jobNumber')">
          <el-select
            style="width: 250px"
            v-model="addForm.apsRoutingInfo"
            value-key="workNum"
            @change="onWorkNumSelectChanged"
            :placeholder="t('placeholder.select')"
          >
            <el-option
              v-for="item in addForm.apsPNInfo?.routings"
              :key="item.workNum"
              :label="`${item.workNum} (${item.workRemark})`"
              :value="item"
            />
          </el-select>
        </el-form-item>

        <!-- BU -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="bu" :label="t('form.label.BU')">
          <label>{{ addForm.apsPNInfo?.bu }}</label>
        </el-form-item>

        <!-- 終端客戶 -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="endCustomer" :label="t('form.label.endCustomer')">
          <label>{{ addForm.apsPNInfo?.custName }}</label>
        </el-form-item>

        <!-- 改善後工時 -->
        <el-form-item
          class="my-1"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="kaizenBeforeWorkHours"
          :label="t('form.label.proc_sec_after_improvement')"
        >
          <label>{{ addForm.apsRoutingInfo?.stdTime }}</label>
        </el-form-item>

        <!-- 作業說明 -->
        <el-form-item class="my-1" :label-width="formLabelWidth" :size="formItemSize" prop="jobDescription" :label="t('form.label.jobDescription')">
          <label>{{ addForm.apsRoutingInfo?.workRemark }}</label>
        </el-form-item>
      </div>

      <!-- 改善內容  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("Kaizen_detail") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 工藝類別 -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="manufMethod" :label="t('form.label.manufacture_method')">
          <el-select
            style="width: 150px"
            v-model="addForm.manufMethod"
            :loading="isManufMethodListLoading"
            value-key="id"
            @visible-change="onManufMethodListShown"
            @change="onManufMethodSelectChanged"
            :placeholder="t('placeholder.select')"
            :loading-text="t('loading')"
          >
            <el-option v-for="manufMethod in allManufMethodList" :key="manufMethod.id" :label="manufMethod.methodName" :value="manufMethod" />
          </el-select>
        </el-form-item>

        <!-- 工時調整日期 -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="implementDay"
          :label="t('form.label.hoursAdjustmentDate')"
        >
          <el-date-picker
            style="width: 150px"
            v-model="addForm.implementDay"
            type="date"
            prop="implementDay"
            value-format="YYYY-MM-DD"
            :placeholder="t('placeholder.date')"
          />
        </el-form-item>

        <!-- 改善前工時 -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="procTimeBeforeImp"
          :label="t('form.label.proc_sec_before_improvement')"
        >
          <el-input-number
            style="width: 150px"
            v-model="addForm.procTimeBeforeImp"
            :precision="1"
            :step="0.1"
            :min="0.0"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("second") }})</span>
        </el-form-item>

        <!-- 原PPH -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="originalPPH" :label="t('form.label.originalPPH')">
          <el-input-number
            style="width: 150px"
            v-model="addForm.originalPPH"
            :precision="1"
            :step="0.1"
            :min="0.0"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">(個)</span>
        </el-form-item>

        <!-- 改善方案 -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="impPlan" :label="t('form.label.improvement_type')">
          <el-input style="width: 350px" v-model="addForm.impPlan" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>

        <!-- 改善對策 -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="impMethod"
          :label="t('form.label.improvement_suggestions')"
        >
          <el-input style="width: 350px" v-model="addForm.impMethod" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>

        <!-- 對策內容 -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="impMethodDetail"
          :label="t('form.label.implement_detail')"
        >
          <el-input
            style="width: 350px"
            v-model="addForm.impMethodDetail"
            :autosize="{ minRows: 5 }"
            type="textarea"
            :placeholder="t('placeholder.input')"
          />
        </el-form-item>
      </div>

      <!-- 面積  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("area") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 改善前面積 -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="areaBeforeImp"
          :label="t('form.label.areaBeforeImprovement')"
        >
          <el-input-number
            style="width: 200px"
            :precision="1"
            :step="0.1"
            :min="0.0"
            v-model="addForm.areaBeforeImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">(m<sup>2</sup>)</span>
        </el-form-item>

        <!-- 改善後面積 -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="areaAfterImp"
          :label="t('form.label.afterTheAreaIsImproved')"
        >
          <el-input-number
            style="width: 200px"
            :precision="1"
            :step="0.1"
            :min="0.0"
            v-model="addForm.areaAfterImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">(m<sup>2</sup>)</span>
        </el-form-item>
      </div>

      <!-- 交期 -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("lead_time") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 原製程L/T -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="procLTBeforeImp" :label="t('form.label.originalProcess')">
          <el-input-number
            style="width: 200px"
            :precision="0"
            :step="1"
            :min="0"
            v-model="addForm.procLTBeforeImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("day") }})</span>
        </el-form-item>

        <!-- 改善後製程L/T -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="procLTAfterImp"
          :label="t('form.label.processing_LT_after_improvement')"
        >
          <el-input-number
            style="width: 200px"
            :precision="0"
            :step="1"
            :min="0"
            v-model="addForm.procLTAfterImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("day") }})</span>
        </el-form-item>

        <!-- 改善後供應商L/T -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="venderLTAfterImp"
          :label="t('form.label.supplierAfterImprovement')"
        >
          <el-input-number
            style="width: 200px"
            :precision="0"
            :step="1"
            :min="0"
            v-model="addForm.venderLTAfterImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("day") }})</span>
        </el-form-item>

        <!-- 改善前供應商L/T -->
        <el-form-item
          class="mb-3"
          :label-width="formLabelWidth"
          :size="formItemSize"
          prop="venderLTBeforeImp"
          :label="t('form.label.improveFormerSuppliers')"
        >
          <el-input-number
            style="width: 200px"
            :precision="0"
            :step="1"
            :min="0"
            v-model="addForm.venderLTBeforeImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("day") }})</span>
        </el-form-item>
      </div>

      <!-- 其他 -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("other") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 超連結 -->
        <el-form-item class="mb-3" :label-width="formLabelWidth" :size="formItemSize" prop="hyperLink" :label="t('form.label.hyperLink')">
          <el-input style="width: 350px" v-model="addForm.hyperLink" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>

      <!-- 送出btn -->
      <div class="d-flex align-items-center justify-content-end ms-auto col-12 mt-xl-4 mt-lg-0 mt-md-0 mt-sm-2 mt-xs-2 mt-2">
        <el-button type="primary" @click="onSubmitFormClicked()">{{ t("submit") }}</el-button>
      </div>
    </el-form>
  </div>

  <!-- 複製彈窗 -->
  <!-- <el-dialog class="copyDialog" v-model="isShowKaizenItemCopyDialog" :title="t('copy')">
    <KaizenCopyDialog v-model="copyDialogSelectRow" @onCopyDialogSelectRow="onCopyDialogSelectRow"></KaizenCopyDialog>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowKaizenItemCopyDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSendCopyRow()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog> -->
  <!-- /複製彈窗 -->

  <!-- 查詢彈窗 -->
  <el-dialog class="project-list-dialog" v-model="isShowProjectSearchDialog" :title="t('inquire')">
    <ProjectSearch ref="projectSearchRef" @project-select="onProjectSelected" />
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowProjectSearchDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSelectProjectConfirmClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /查詢彈窗 -->
</template>

<script lang="ts">
import { defineComponent, ref, reactive, toRefs, onMounted, onActivated, nextTick } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";

// element-plus
import { Search, EditPen, Delete, Calendar } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
// DTO
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IReqSelectRowKaizenDTO from "@/models/dto/IReqSelectRowKaizenDTO";
import IReqFactoryDTO from "@/models/dto/IReqFactoryDTO";
import IReqProjectCodeMgmtDTO from "@/models/dto/IReqProjectCodeMgmtDTO";
// components
import ProjectSearch from "@/components/ProjectSearch.vue";
import KaizenCopyDialog from "@/components/KaizenCopyDialog.vue";
import KaizenSearchDialog from "@/components/KaizenSearchDialog.vue";
import { dateFormat } from "@/plugins/utility";
import IResProjectInfoDTO from "@/models/dto/IResProjectInfoDTO";
import IReqPNInfoSearchCriteriaDTO from "@/models/dto/IReqPNInfoSearchCriteriaDTO";
import IResPNInfo2DTO from "@/models/dto/IResPNInfo2DTO";
import KaizenItemViewModel from "@/models/KaizenItemViewModel";

export default defineComponent({
  components: {
    //KaizenCopyDialog,
    ProjectSearch,
  },
  setup() {
    const { t } = useI18n();
    const router = useRouter();
    const userInfoStore = useUserInfoStore();

    const state = reactive({
      formLabelPosition: "right" as "top" | "left" | "right",
      formItemSize: "default" as "default" | "small",
      formLabelWidth: 200,
      today: "",

      // 新增
      addForm: {} as KaizenItemViewModel,
      isFormLoading: false,

      //料號查詢
      candidatePN: [] as IResPNInfo2DTO[],
      isPNListLoading: false,

      // 工藝類別選項
      allManufMethodList: [] as IResManufMethodDTO[],
      isManufMethodListLoading: false,

      // 複製彈窗
      isShowKaizenItemCopyDialog: false as boolean,
      copyDialogSelectRow: {} as IReqSelectRowKaizenDTO,

      // 查詢彈窗
      targetProject: {} as IResProjectInfoDTO,
      isShowProjectSearchDialog: false as boolean,
      searchDialogSelectRow: {} as IReqProjectCodeMgmtDTO,
    });

    //#region 建立表單ref與Validator
    const targetPNValidator = async (rule: any, value: string, callback: any) => {
      if (!state.addForm.apsPNInfo) {
        return callback(new Error(t("validation_msg.part_no_is_required")));
      }
      return callback();
    };

    const workNumValidator = async (rule: any, value: any, callback: any) => {
      if (!state.addForm.apsRoutingInfo) {
        return callback(new Error(t("validation_msg.work_num_is_required")));
      }
      return callback();
    };

    const projectValidator = async (rule: any, value: any, callback: any) => {
      if (!state.addForm.project) {
        return callback(new Error(t("validation_msg.project_code_is_required")));
      }
      return callback();
    };
    const manufMethodValidator = async (rule: any, value: any, callback: any) => {
      if (!state.addForm.manufMethod) {
        return callback(new Error(t("validation_msg.manuf_method_is_required")));
      }
      return callback();
    };

    const procTimeBeforeImpValidator = async (rule: any, value: any, callback: any) => {
      if (!value) return callback(new Error(t("validation_msg.processing_time_is_required")));
    };

    const originalPPHValidator = async (rule: any, value: any, callback: any) => {
      if (!value) return callback(new Error(t("validation_msg.original_pph_is_required")));
    };

    const areaValueValidator = async (rule: any, value: any, callback: any) => {
      console.log("areaValueValidator");
      console.log(value);

      if (!value || Number.isNaN(value)) {
        console.log("not a number");
        callback(new Error(t("validation_msg.invalid_ufloat_value")));
      }
      callback();
    };
    const leadTimeValueValidator = async (rule: any, value: any, callback: any) => {
      console.log("leadTimeValueValidator");
      console.log(value);
      if (!value || Number.isNaN(value)) {
        console.log("not a number");
        callback(new Error(t("validation_msg.invalid_uint_value")));
      }
      callback();
    };

    const newKaizenFormRef = ref();
    const newKaizenFormRules = ref({
      projectCode: [{ required: true, validator: projectValidator, trigger: "blur" }],
      apsPNInfo: [{ required: true, validator: targetPNValidator, trigger: "blur" }],
      workNum: [{ required: true, validator: workNumValidator, trigger: "blur" }],
      manufMethod: [{ required: true, validator: manufMethodValidator, trigger: "blur" }],
      procTimeBeforeImp: [{ required: true, validator: procTimeBeforeImpValidator, trigger: "blur" }],
      impPlan: [{ required: true, message: () => t("form.validation_msg.improvement_plan_is_required"), trigger: "blur" }],
      impMethod: [{ required: true, message: () => t("form.validation_msg.improvement_method_is_required"), trigger: "blur" }],
      impMethodDetail: [{ required: true, message: () => t("form.validation_msg.implement_detail_is_required"), trigger: "blur" }],
      originalPPH: [{ required: true, validator: originalPPHValidator, trigger: "blur" }],
      implementDay: [{ required: true, message: () => t("form.validation_msg.implement_day_is_required"), trigger: "blur" }],
      areaBeforeImp: [{ required: false, validator: areaValueValidator, trigger: "blur" }],
      areaAfterImp: [{ required: false, validator: areaValueValidator, trigger: "blur" }],
      procLTBeforeImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
      procLTAfterImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
      venderLTAfterImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
      venderLTBeforeImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
    });
    const projectSearchRef = ref(ProjectSearch);
    //#endregion

    //#region Hook functions
    onMounted(() => {
      console.log(`onMounted : displayName=${userInfoStore.displayName}`);

      setBasicInfo();
    }),
      onActivated(() => {
        console.log(`onActivated : displayName=${userInfoStore.displayName}`);
        setBasicInfo();
      });
    //#endregion

    //#region UI Events
    /** 點擊專案代碼輸入欄位 */
    const onPJCodeInputFieldClicked = async () => {
      if (state.addForm.project) return;
      await showProjectSearchDialog();
    };

    /** 按下查詢專案按鈕 */
    const onSearchProjectBtnClicked = async () => {
      await showProjectSearchDialog();
    };

    /** 於Dialog內選擇專案 */
    const onProjectSelected = (project: IResProjectInfoDTO) => {
      state.targetProject = project;
    };

    /** 完成專案選擇 */
    const onSelectProjectConfirmClicked = () => {
      if (!state.targetProject) return;
      console.log(`onProjectSelectChanged.  ${state.targetProject.id}, ${state.targetProject.pjCode}`);

      state.addForm.project = state.targetProject;
      state.addForm.projectCode = state.targetProject.pjCode;

      //重新執行projectCode區域的validator
      newKaizenFormRef.value?.validateField("projectCode");

      state.isShowProjectSearchDialog = false;
    };

    /** 開啟複製彈窗 */
    const onCopyBtnClicked = () => {
      state.isShowKaizenItemCopyDialog = true;
    };

    /** 按下清除按鈕 */
    const onClearFormClicked = async () => {
      try {
        await ElMessageBox.confirm(t("clear_confirm_msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });

        await resetFormDataAsync();
      } catch (error) {
        console.log(error);
      }
    };

    /** 顯示工藝類別下拉選單時 */
    const onManufMethodListShown = async () => {
      try {
        state.isManufMethodListLoading = true;
        await updateManufMethodList();
      } catch (error) {
        console.log(error);
      } finally {
        state.isManufMethodListLoading = false;
      }
    };

    /** 依使用者輸入的字串提供料號選擇建議 */
    const onPNSuggestionListShown = async (queryString: string) => {
      console.log(`onPNSuggestionListShown : ${queryString}`);
      state.isPNListLoading = true;
      try {
        state.candidatePN.length = 0;
        const searchCriteria: IReqPNInfoSearchCriteriaDTO = {
          pnKeyword: queryString,
          takeNum: 10,
        };
        const searchPNResponse = await API.searchPartInfoV2(searchCriteria);
        if (searchPNResponse.data.result !== APIResultCode.success) {
          throw new Error(searchPNResponse.data.msg);
        }
        state.candidatePN = searchPNResponse.data.content;
      } catch (error) {
        console.log(error);
      } finally {
        state.isPNListLoading = false;
      }
    };

    /** 選擇改善料號 */
    const onPNSelectChanged = () => {
      //重新執行projectCode區域的validator (此時應該有值, 告警訊息應該消除)
      newKaizenFormRef.value?.validateField("apsPNInfo");
      state.addForm.apsRoutingInfo = undefined;
    };

    /** 選擇作業編號 */
    const onWorkNumSelectChanged = () => {
      //重新執行jobNumber區域的validator (此時應該有值, 告警訊息應該消除)
      newKaizenFormRef.value?.validateField("jobNumber");
    };

    const onManufMethodSelectChanged = () => {
      //重新執行manufMethod區域的validator (此時應該有值, 告警訊息應該消除)
      newKaizenFormRef.value?.validateField("manufMethod");
    };

    /** 按下送出 */
    const onSubmitFormClicked = async () => {
      console.log("onSubmitFormClicked");
      try {
        await newKaizenFormRef.value?.validate();

        await ElMessageBox.confirm(t("Create_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });
      } catch (error) {
        console.log(error);
        return;
      }

      try {
        state.isFormLoading = true;

        const requestDTO = state.addForm.ToIReqKaizenItemDTO();
        //const createKaizenItemResponse = await API.
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isFormLoading = false;
        await resetFormDataAsync();
      }
    };
    //#endregion

    //#region Private Functions
    /** 重置所有表單資料 done */
    async function resetFormDataAsync() {
      state.addForm = new KaizenItemViewModel();
      state.candidatePN.length = 0;

      await nextTick();

      newKaizenFormRef.value.clearValidate();

      setBasicInfo();
    }

    /** 設置基本建立資訊(建立者、建立日期) */
    function setBasicInfo() {
      //state.addForm.creator = userInfoStore.displayName; //建立者資訊直接從userInfoStore提取
      state.today = dateFormat(new Date());
      state.addForm.implementDay = new Date();
    }

    /** 顯示專案查詢dialge(重置查詢結果) */
    async function showProjectSearchDialog() {
      console.log("ShowProjectSearchDialog");
      state.isShowProjectSearchDialog = true;
      await nextTick();
      await projectSearchRef.value?.resetSearch();
    }

    /** 呼叫API, 取得所有工藝類別 */
    async function updateManufMethodList() {
      const getAllManufMethodResponse = await API.getManufMethods();

      if (getAllManufMethodResponse.data.result !== APIResultCode.success) {
        throw new Error(getAllManufMethodResponse.data.msg);
      }

      state.allManufMethodList = getAllManufMethodResponse.data.content;
    }
    //#endregion

    return {
      t,
      ...toRefs(state),
      newKaizenFormRef,
      newKaizenFormRules,
      projectSearchRef,

      // icons
      Search,

      // function
      onPJCodeInputFieldClicked,
      onSearchProjectBtnClicked,
      onClearFormClicked,
      onManufMethodListShown,
      onCopyBtnClicked,
      onProjectSelected,
      onPNSuggestionListShown,
      onPNSelectChanged,
      onWorkNumSelectChanged,
      onManufMethodSelectChanged,
      onSelectProjectConfirmClicked,
      onSubmitFormClicked,

      userInfoStore,
    };
  },
});
</script>

<style scoped></style>
