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

    <!-- <hr class="w-100 my-0" /> -->
    <!-- <el-divider /> -->
    <el-divider />

    <el-form label-position="right" :label-width="formLabelWidth">
      <div class="d-flex flex-row justify-content-between">
        <el-form-item :size="formItemSize" :label="t('form.label.creator')">
          <label>{{ userInfoStore.displayName }}</label>
        </el-form-item>
        <el-form-item :size="formItemSize" :label="t('form.label.createDate')">
          <label>{{ today }}</label>
        </el-form-item>
      </div>
    </el-form>

    <!-- form -->
    <el-form
      @submit.prevent
      ref="newKaizenFormRef"
      v-loading.fullscreen.lock="isFormLoading"
      :model="addForm"
      :rules="newKaizenFormRules"
      :label-position="formLabelPosition"
      :label-width="formLabelWidth"
    >
      <!-- 段落: 基本資訊 -->
      <!-- <div class="row row-cols-sm-2 row-cols-1 mb-5">
        <el-form-item class="col mb-0 mb-sm-auto" :label-position="'right'" :size="formItemSize" prop="creator" :label="t('form.label.creator')">
          <label>{{ userInfoStore.displayName }}</label>
        </el-form-item>
        <el-form-item class="col mb-0 mb-sm-auto" :label-position="'right'" :size="formItemSize" prop="createDate" :label="t('form.label.createDate')">
          <label>{{ today }}</label>
        </el-form-item>
      </div> -->

      <!-- 段落: 所屬專案  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("project") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 專案代碼 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="projectCode" :label="t('form.label.project_code')">
          <el-input
            class="mx-3 m-sm-0"
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
        <el-form-item class="mb-4" :size="formItemSize" prop="site" :label="t('form.label.site')">
          <label class="mx-3 m-sm-0">{{ addForm.project?.siteName }}</label>
        </el-form-item>

        <!-- 工廠 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="factory" :label="t('form.label.factory')">
          <label class="mx-3 m-sm-0">{{ addForm.project?.factoryName }}</label>
        </el-form-item>

        <!-- 小組 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="team" :label="t('form.label.team')">
          <label class="mx-3 m-sm-0">{{ addForm.project?.teamNum }}</label>
        </el-form-item>

        <!-- 開始月份 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="startMonth" :label="t('form.label.start_month')">
          <label class="mx-3 m-sm-0">{{ addForm.project?.startMonth }}</label>
        </el-form-item>

        <!-- 結束月份 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="endMonth" :label="t('form.label.end_month')">
          <label class="mx-3 m-sm-0">{{ addForm.project?.endMonth }}</label>
        </el-form-item>
      </div>

      <!-- 段落: 作業料號  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("process_part_no") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 改善料號 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="apsPNInfo" :label="t('form.label.Kaizen_PN')">
          <el-tooltip :disabled="addForm.project !== undefined" effect="light" placement="bottom" :content="t('please_select_project_first')">
            <el-select
              class="mx-3 m-sm-0"
              style="width: 250px"
              v-model="addForm.apsPNInfo"
              filterable
              remote
              reserve-keyword
              default-first-option
              :disabled="addForm.project === undefined"
              :placeholder="t('placeholder.input')"
              :remote-method="onPNSuggestionListShown"
              :loading="isPNListLoading"
              value-key="pn"
              @change="onPNSelectChanged"
            >
              <el-option v-for="item in candidatePN" :key="item.pn" :label="item.pn" :value="item" />
            </el-select>
          </el-tooltip>
        </el-form-item>

        <!-- 作業編號 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="workNum" :label="t('form.label.work_number')">
          <el-tooltip :disabled="addForm.project !== undefined" effect="light" placement="bottom" :content="t('please_select_project_first')">
            <el-select
              class="mx-3 m-sm-0"
              style="width: 250px"
              v-model="addForm.apsRoutingInfo"
              :disabled="addForm.project === undefined"
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
          </el-tooltip>
        </el-form-item>

        <!-- BU -->
        <el-form-item class="mb-4" :size="formItemSize" prop="bu" :label="t('form.label.BU')">
          <label class="mx-3 m-sm-0">{{ addForm.apsPNInfo?.bu }}</label>
        </el-form-item>

        <!-- 終端客戶 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="endCustomer" :label="t('form.label.endCustomer')">
          <label class="mx-3 m-sm-0">{{ addForm.apsPNInfo?.custName }}</label>
        </el-form-item>

        <!-- 作業說明 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="workRemark" :label="t('form.label.work_remark')">
          <label class="mx-3 m-sm-0">{{ addForm.apsRoutingInfo?.workRemark }}</label>
        </el-form-item>

        <!-- 工藝類別 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="manufMethod" :label="t('form.label.manufacture_method')">
          <el-select
            class="mx-3 m-sm-0"
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
        <el-form-item class="mb-4" :size="formItemSize" prop="implementDay" :label="t('form.label.hoursAdjustmentDate')">
          <el-date-picker
            class="mx-3 m-sm-0"
            style="width: 150px"
            v-model="addForm.implementDay"
            type="date"
            prop="implementDay"
            :placeholder="t('placeholder.date')"
          />
        </el-form-item>

        <!-- 改善方案 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="impPlan" :label="t('form.label.improvement_type')">
          <el-input class="mx-3 m-sm-0" style="width: 350px" v-model="addForm.impPlan" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>

        <!-- 改善對策 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="impMethod" :label="t('form.label.improvement_suggestions')">
          <el-input class="mx-3 m-sm-0" style="width: 350px" v-model="addForm.impMethod" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>

        <!-- 對策內容 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="impMethodDetail" :label="t('form.label.implement_detail')">
          <el-input
            class="mx-3 m-sm-0"
            style="width: 350px"
            v-model="addForm.impMethodDetail"
            :autosize="{ minRows: 5 }"
            type="textarea"
            :placeholder="t('placeholder.input')"
          />
        </el-form-item>

        <!-- 超連結 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="hyperLink" :label="t('form.label.hyperLink')">
          <el-input class="mx-3 m-sm-0" style="width: 350px" v-model="addForm.hyperLink" clearable :placeholder="t('placeholder.input')" />
        </el-form-item>
      </div>

      <!-- 段落: 改善前  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("before_improvement") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 改善前工時 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="procTimeBeforeImp" :label="t('form.label.proc_time_before_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            v-model="addForm.procTimeBeforeImp"
            :precision="1"
            :step="0.1"
            :min="0.0"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("second") }})</span>
        </el-form-item>

        <!-- 改善前面積 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="areaBeforeImp" :label="t('form.label.area_before_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            :precision="1"
            :step="0.1"
            :min="0.0"
            v-model="addForm.areaBeforeImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">(m<sup>2</sup>)</span>
        </el-form-item>

        <!-- 原製程L/T -->
        <el-form-item class="mb-4" :size="formItemSize" prop="procLTBeforeImp" :label="t('form.label.processing_leadtime_before_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            :precision="0"
            :step="1"
            :min="0"
            v-model="addForm.procLTBeforeImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("day") }})</span>
        </el-form-item>

        <!-- 改善前供應商L/T -->
        <el-form-item class="mb-4" :size="formItemSize" prop="venderLTBeforeImp" :label="t('form.label.vender_leadtime_before_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            :precision="0"
            :step="1"
            :min="0"
            v-model="addForm.venderLTBeforeImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("day") }})</span>
        </el-form-item>

        <!-- 改善前PPH -->
        <el-form-item class="mb-4" :size="formItemSize" prop="pphBeforeImp" :label="t('form.label.pph_before_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            v-model="addForm.pphBeforeImp"
            :precision="1"
            :step="0.1"
            :min="0.0"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">(個)</span>
        </el-form-item>
      </div>

      <!-- 段落: 改善後  -->
      <div class="mb-5">
        <div class="d-flex flex-column justify-content-start align-items-start mb-3">
          <span>{{ t("after_improvement") }}</span>
          <hr class="w-100 my-0" />
        </div>

        <!-- 改善後工時 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="procTimeAfterImp" :label="t('form.label.proc_time_after_improvement')">
          <label class="mx-3 m-sm-0">{{ addForm.apsRoutingInfo?.stdTime }}</label>
        </el-form-item>

        <!-- 改善後面積 -->
        <el-form-item class="mb-4" :size="formItemSize" prop="areaAfterImp" :label="t('form.label.area_after_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            :precision="1"
            :step="0.1"
            :min="0.0"
            v-model="addForm.areaAfterImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">(m<sup>2</sup>)</span>
        </el-form-item>

        <!-- 改善後製程L/T -->
        <el-form-item class="mb-4" :size="formItemSize" prop="procLTAfterImp" :label="t('form.label.processing_leadtime_after_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
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
        <el-form-item class="mb-4" :size="formItemSize" prop="venderLTAfterImp" :label="t('form.label.vender_leadtime_after_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            :precision="0"
            :step="1"
            :min="0"
            v-model="addForm.venderLTAfterImp"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">({{ t("day") }})</span>
        </el-form-item>

        <!-- 改善後PPH -->
        <el-form-item class="mb-4" :size="formItemSize" prop="pphAfterImp" :label="t('form.label.pph_after_improvement')">
          <el-input-number
            class="mx-3 m-sm-0 number_input_box"
            v-model="addForm.pphAfterImp"
            :precision="1"
            :step="0.1"
            :min="0.0"
            clearable
            :placeholder="t('placeholder.input')"
          />
          <span class="mx-1">(個)</span>
        </el-form-item>
      </div>

      <!-- 送出btn -->
      <div class="d-flex align-items-center justify-content-end ms-auto col-12 mt-xl-4 mt-lg-0 mt-md-0 mt-sm-2 mt-xs-2 mt-2">
        <el-button type="primary" @click="onSubmitFormClicked()">{{ t("submit") }}</el-button>
      </div>
    </el-form>
  </div>

  <!-- 複製彈窗 -->
  <el-dialog class="kaizen-list-dialog" v-model="isShowKaizenItemSearchDialog" :top="'5vh'" :title="t('copy')">
    <KaizenItemSearch ref="kaizenItemSearchRef" @kaizen-item-select="onKaizenItemSelected"></KaizenItemSearch>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowKaizenItemSearchDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSelectKaizenItemConfirmClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /複製彈窗 -->

  <!-- 查詢彈窗 -->
  <el-dialog class="project-list-dialog" v-model="isShowProjectSearchDialog" :top="'5vh'" :title="t('inquire')">
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
import { Search } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
// DTO
import IResManufMethodDTO from "@/models/dto/IResManufMethodDTO";
import IResKaizenItemDTO from "@/models/dto/IResKaizenItemDTO";
// components
import ProjectSearch from "@/components/ProjectSearch.vue";
import KaizenItemSearch from "@/components/KaizenItemSearch.vue";
import { dateFormat } from "@/plugins/utility";
import IResProjectInfoDTO from "@/models/dto/IResProjectInfoDTO";
import IReqPNInfoSearchCriteriaDTO from "@/models/dto/IReqPNInfoSearchCriteriaDTO";
import IResPNInfo2DTO from "@/models/dto/IResPNInfo2DTO";
import KaizenItemViewModel from "@/models/KaizenItemViewModel";

export default defineComponent({
  components: {
    KaizenItemSearch,
    ProjectSearch,
  },
  setup() {
    const { t } = useI18n();
    const router = useRouter();
    const userInfoStore = useUserInfoStore();

    const state = reactive({
      formLabelPosition: "right" as "top" | "left" | "right",
      formItemSize: "default" as "default" | "small",
      formLabelWidth: "200px" as string,
      today: "",

      // 新增
      addForm: new KaizenItemViewModel(),
      isFormLoading: false,

      //料號查詢
      candidatePN: [] as IResPNInfo2DTO[],
      isPNListLoading: false,

      // 工藝類別選項
      allManufMethodList: [] as IResManufMethodDTO[],
      isManufMethodListLoading: false,

      // 複製彈窗
      isShowKaizenItemSearchDialog: false as boolean,
      targetKaizenItem: {} as IResKaizenItemDTO,

      // 查詢彈窗
      isShowProjectSearchDialog: false as boolean,
      targetProject: {} as IResProjectInfoDTO,
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

    const pphValidator = async (rule: any, value: any, callback: any) => {
      console.log("pphValidator");
      console.log(value);
      if (!value) return callback();
      if (Number.isNaN(value)) {
        console.log("not a number");
        callback(new Error(t("validation_msg.invalid_ufloat_value")));
      }
      callback();
    };

    const areaValueValidator = async (rule: any, value: any, callback: any) => {
      console.log("areaValueValidator");
      console.log(value);
      if (!value) return callback();

      if (Number.isNaN(value)) {
        console.log("not a number");
        callback(new Error(t("validation_msg.invalid_ufloat_value")));
      }
      callback();
    };
    const leadTimeValueValidator = async (rule: any, value: any, callback: any) => {
      console.log("leadTimeValueValidator");
      console.log(value);
      if (!value) return callback();
      if (Number.isNaN(value)) {
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
      impPlan: [{ required: true, message: () => t("form.validation_msg.improvement_plan_is_required"), trigger: "blur" }],
      impMethod: [{ required: true, message: () => t("form.validation_msg.improvement_method_is_required"), trigger: "blur" }],
      impMethodDetail: [{ required: true, message: () => t("form.validation_msg.implement_detail_is_required"), trigger: "blur" }],
      implementDay: [{ required: true, message: () => t("form.validation_msg.implement_day_is_required"), trigger: "blur" }],

      procTimeBeforeImp: [{ required: true, validator: procTimeBeforeImpValidator, trigger: "blur" }],
      areaBeforeImp: [{ required: false, validator: areaValueValidator, trigger: "blur" }],
      procLTBeforeImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
      venderLTBeforeImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
      pphBeforeImp: [{ required: false, validator: pphValidator, trigger: "blur" }],

      areaAfterImp: [{ required: false, validator: areaValueValidator, trigger: "blur" }],
      procLTAfterImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
      venderLTAfterImp: [{ required: false, validator: leadTimeValueValidator, trigger: "blur" }],
      pphAfterImp: [{ required: false, validator: pphValidator, trigger: "blur" }],
    });

    const projectSearchRef = ref(ProjectSearch);
    const kaizenItemSearchRef = ref(KaizenItemSearch);
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
      console.log('onPJCodeInputFieldClicked');
      if (state.addForm.project) return;
      await showProjectSearchDialog();
    };

    /** 按下查詢專案按鈕 */
    const onSearchProjectBtnClicked = async () => {
      console.log('onSearchProjectBtnClicked');
      await showProjectSearchDialog();
    };

    /** 於Dialog內選擇專案 */
    const onProjectSelected = (project: IResProjectInfoDTO) => {
      state.targetProject = project;
    };

    /** 完成專案選擇 */
    const onSelectProjectConfirmClicked = () => {
      if (!state.targetProject) return;

      if (state.addForm.project && state.targetProject.id === state.addForm.project.id) {
        state.isShowProjectSearchDialog = false;
        return;
      }

      console.log(`onProjectSelectChanged.  ${state.targetProject.id}, ${state.targetProject.pjCode}`);

      state.addForm.project = state.targetProject;
      state.addForm.projectCode = state.targetProject.pjCode;

      //重新執行projectCode區域的validator
      newKaizenFormRef.value?.validateField("projectCode");

      //重置料號選擇內容
      state.candidatePN.length = 0;
      state.addForm.apsPNInfo = undefined;
      state.addForm.apsRoutingInfo = undefined;

      state.isShowProjectSearchDialog = false;
    };

    /** 開啟複製彈窗 */
    const onCopyBtnClicked = async () => {
      await showKaizenItemSearchDialog();
    };

    /** 於Dialog內選擇改善事項 */
    const onKaizenItemSelected = (kaizenItem: IResKaizenItemDTO) => {
      console.log('onKaizenItemSelected in v3');
      
      state.targetKaizenItem = kaizenItem;
    };

    /** 完成改善事項複製選擇 */
    const onSelectKaizenItemConfirmClicked = () => {
      if (!state.targetKaizenItem) return;

      //除專案代碼、料號、改善前工時、改善後工時、作業編號其他皆要複製

      state.addForm.manufMethod = state.allManufMethodList.find(x => x.id === state.targetKaizenItem.manufMethodId);
      state.addForm.impPlan = state.targetKaizenItem.impPlan;
      state.addForm.impMethod = state.targetKaizenItem.impMethod;
      state.addForm.impMethodDetail = state.targetKaizenItem.impMethodDetail;
      //state.addForm.implementDay = state.targetKaizenItem.implementDay; 
      state.addForm.hyperLink = state.targetKaizenItem.hyperlink;
      state.addForm.areaBeforeImp = Number(state.targetKaizenItem.areaBeforeImp);
      state.addForm.procLTBeforeImp = Number(state.targetKaizenItem.procLTBeforeImp);
      state.addForm.venderLTBeforeImp = Number(state.targetKaizenItem.venderLTBeforeImp);
      state.addForm.pphBeforeImp = Number(state.targetKaizenItem.pphBeforeImp);
      state.addForm.areaAfterImp = Number(state.targetKaizenItem.areaAfterImp);
      state.addForm.procLTAfterImp = Number(state.targetKaizenItem.procLTAfterImp);
      state.addForm.venderLTAfterImp = Number(state.targetKaizenItem.venderLTAfterImp);
      state.addForm.pphAfterImp = Number(state.targetKaizenItem.pphAfterImp);


      state.isShowKaizenItemSearchDialog = false;
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
      if (!state.addForm.project) {
        console.warn("project is undefined");
        return;
      }

      state.isPNListLoading = true;
      try {
        state.candidatePN.length = 0;
        const searchCriteria: IReqPNInfoSearchCriteriaDTO = {
          pnKeyword: queryString,
          siteName: state.addForm.project.siteName,
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
      newKaizenFormRef.value?.validateField("workNum");
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
      } catch (error) {
        console.log(error);
        await nextTick();
        const isError = document.getElementsByClassName('is-error')[0];
        isError.scrollIntoView({
          block:'center',
          behavior:'smooth'
        })
      }
      
      try {
      

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
        const createKaizenItemResponse = await API.createKaizenItem(requestDTO);
        if (createKaizenItemResponse.data.result !== APIResultCode.success) {
          throw new Error(createKaizenItemResponse.data.msg);
        }

        ElMessage({
          showClose: true,
          message: "新增成功",
          type: "success",
        });

        await resetFormDataAsync();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isFormLoading = false;
      }
    };
    //#endregion

    //#region Private Functions
    /** 重置所有表單資料 */
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
      if (window.innerWidth <= 576) {
        state.formLabelPosition = "top";
        state.formItemSize = "default";
        state.formLabelWidth = "auto";
      }
      state.today = dateFormat(new Date());
      state.addForm.implementDay = new Date();
    }

    /** 顯示專案查詢dialog(重置查詢結果) */
    async function showProjectSearchDialog() {
      state.isShowProjectSearchDialog = true;
      console.log('showProjectSearchDialog');
      
      await nextTick();
      await projectSearchRef.value?.resetSearch();
    }

    /** 顯示改善事項查詢dialog(重置查詢結果) */
    async function showKaizenItemSearchDialog() {
      state.isShowKaizenItemSearchDialog = true;
      await nextTick();
      await kaizenItemSearchRef.value?.resetSearch();
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
      kaizenItemSearchRef,

      // icons
      Search,

      // function
      onPJCodeInputFieldClicked,
      onSearchProjectBtnClicked,
      onClearFormClicked,
      onManufMethodListShown,
      onCopyBtnClicked,
      onProjectSelected,
      onKaizenItemSelected,
      onPNSuggestionListShown,
      onPNSelectChanged,
      onWorkNumSelectChanged,
      onManufMethodSelectChanged,
      onSelectProjectConfirmClicked,
      onSubmitFormClicked,
      onSelectKaizenItemConfirmClicked,

      userInfoStore,
    };
  },
});
</script>

<style scoped>
.number_input_box {
  width: 180px;
}
</style>
