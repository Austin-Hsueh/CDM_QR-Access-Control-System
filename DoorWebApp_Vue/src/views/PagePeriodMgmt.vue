<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("project_code") }}</span>
    </div>

    <!-- Search Form -->
    <el-form @submit.prevent class="mt-2"  ref="searchCriteriaFormRef" :model="searchCriteria" :rules="searchCriteriaFormRules" :label-position="'right'" :label-width="'auto'">
      <div class="row row-cols-1 row-cols-sm-2 row-cols-md-2 row-cols-lg-4 row-cols-xl-4">
        <!-- 期數 -->
        <div class="col">
          <el-form-item class="w-100" prop="periodNum" :label="t('period') + ':'">
            <el-input
              v-model="searchCriteria.periodNum"
              @keyup.enter="onInputFieldEnterKeyUp"
              clearable
              :placeholder="t('placeholder.input')"
            />
          </el-form-item>
        </div>

        <!-- 建立者 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('creator') + ':'">
            <el-input v-model="searchCriteria.creatorKeyword" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
          </el-form-item>
        </div>

        <!-- 起始時間 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('startDate') + ':'">
            <el-date-picker
              v-model="searchCriteria.startDate"
              value-format="YYYY-MM-DD"
              type="month"
              :placeholder="t('placeholder.date')"
              style="width: 100%"
              clearable
            />
          </el-form-item>
        </div>

        <!-- 結束時間 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('endDate') + ':'">
            <el-date-picker
              v-model="searchCriteria.endDate"
              value-format="YYYY-MM-DD"
              type="month"
              :placeholder="t('placeholder.date')"
              style="width: 100%"
              clearable
            />
          </el-form-item>
        </div>
      </div>
    </el-form>

    <!-- Btn: 清除 搜尋 -->
    <div class="d-flex flex-row justify-content-end">
      <el-button type="" @click="onClearSearchCriteriaClicked()">{{ t("clear") }}</el-button>
      <el-button type="primary" @click="onSearchperiodClicked()">{{ t("search") }}</el-button>
    </div>

    <hr />

    <!-- 新增btn -->
    <el-row justify="end" class="mb-3" v-if="userInfoStore.permissions.some((x) => x === 420)">
      <el-col :span="2" :xs="3" :sm="3" :md="2" :lg="2" :xl="2" class="d-flex justify-content-end">
        <el-button type="primary" @click="onCreatePeriodClicked()" :disabled="!userInfoStore.permissions.some((x) => x === 220)">{{
          t("create")
        }}</el-button>
      </el-col>
    </el-row>
    <!-- /新增btn -->

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
            <el-checkbox v-model="colDisplayCtrl[0]">{{ t("period") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[1]">{{ t("creator") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[2]">{{ t("createDate") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[3]">{{ t("startMonth") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[4]">{{ t("endMonth") }}</el-checkbox>
          </div>
        </el-popover>
      </div>
      <el-table ref="periodTableRef" class="w-100" v-loading.fullscreen.lock="isTableLoading" :stripe="true" :data="displayPeriods" @sort-change="onSortChanged">
        <el-table-column min-width="80" v-if="colDisplayCtrl[0]" sortable="custom" :label="t('period')" prop="periodNum" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[1]" sortable="custom" :label="t('creator')" prop="creatorDisplayName" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[2]" sortable="custom" :label="t('createDate')" prop="createTime" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[3]" sortable="custom" :label="t('startMonth')" prop="startMonth" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[4]" sortable="custom" :label="t('endMonth')" prop="endMonth" />
        <el-table-column width="80" :label="t('operation')" prop="operate" class="operateBtnGroup d-flex" fixed="right">
          <template #default="scope">
            <el-button
              link
              type="success"
              @click="onEditPeriodClicked(scope.row)"
              v-if="userInfoStore.permissions.some((x) => x === 420)"
            >
              <el-icon :size="16"><EditPen /></el-icon>
            </el-button>
            <el-button v-else link type="primary" @click="onViewperiodClicked(scope.row)" >
              <el-icon><ZoomIn /></el-icon>
            </el-button>

            <el-button
              link
              type="danger"
              @click="onDeletePeriodClicked(scope.row)"
              v-if="userInfoStore.permissions.some((x) => x === 420)"
            >
              <el-icon :size="16"><Delete /></el-icon>
            </el-button>
            <el-button v-else link type="info" @click="onDeletePeriodClicked(scope.row)" :disabled="true">
              <el-icon :size="16"><Delete /></el-icon>
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>
    <!-- /table -->

    <!-- pagination -->
    <el-row justify="end" class="mt-3">
      <el-col>
        <div class="demo-pagination-block mt-3 d-flex justify-content-end">
          <el-pagination
            v-model:currentPage="currentPage"
            v-model:page-size="pageSize"
            :pager-count="paginatorSetup.pagerCount"
            :layout="paginatorSetup.layout"
            :small="paginatorSetup.small"
            :page-sizes="[20, 50, 100]"
            :total="filteredPeriods.length"
            @size-change="onPagesizeChanged"
            @current-change="onCurrentPageChanged"
            justify="end"
          />
        </div>
      </el-col>
    </el-row>
    <!-- /pagination -->
  </div>

  <!-- 新增彈窗 -->
  <el-dialog class="dialog" v-model="isShowAddPeriodDialog" :title="t('create')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isAddDialogLoading" ref="addPeriodFormRef" :model="newPeriodData" :rules="addPeriodFormRules">
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-lg-8 col-12" :label-width="formLabelWidth">
        <label>{{ newPeriodData.creator?.displayName }} ({{ newPeriodData.creator?.username }})</label>
      </el-form-item>
      <el-form-item :label="t('form.label.period')" prop="periodNum" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-input v-model.trim="newPeriodData.periodNum" :placeholder="t('placeholder.input')" />
      </el-form-item>
      <el-form-item :label="t('form.label.month_range')" prop="duration" :label-width="formLabelWidth">
        <el-date-picker
          class="w-100"
          v-model="newPeriodData.duration"
          unlink-panels
          value-format="YYYY-MM-DD"
          type="monthrange"
          :placeholder="t('placeholder.date')"
        />
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" prop="remark" class="text-nowrap" :label-width="formLabelWidth">
        <el-input v-model="newPeriodData.remark" :rows="4" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddPeriodDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSubmitNewPeriodClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 編輯彈窗 -->
  <el-dialog class="dialog" v-model="isShowModifyPeriodDialog" :title="t('modify')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isModifyDialogLoading" ref="modifyPeriodFormRef" :model="selectedPeriodData" :rules="modifyPeriodFormRules">
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-lg-8 col-12" :label-width="formLabelWidth">
        <label>{{ selectedPeriodData.creator?.displayName }} ({{ selectedPeriodData.creator?.username }})</label>
      </el-form-item>
      <el-form-item :label="t('form.label.period')" prop="periodNum" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-input v-model.trim="selectedPeriodData.periodNum" :placeholder="t('placeholder.input')" />
      </el-form-item>
      <el-form-item :label="t('form.label.month_range')" prop="duration" :label-width="formLabelWidth">
        <el-date-picker
          class="w-100"
          v-model="selectedPeriodData.duration"
          value-format="YYYY-MM-DD"
          type="monthrange"
          unlink-panels
          :placeholder="t('placeholder.date')"
        />
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" prop="remark" class="text-nowrap" :label-width="formLabelWidth">
        <el-input v-model="selectedPeriodData.remark" :rows="4" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifyPeriodDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSaveModifyResultClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->

  <!-- 瀏覽彈窗 -->
  <el-dialog class="dialog" v-model="isShowViewPeriodDialog" :title="t('detail')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isModifyDialogLoading" ref="modifyPeriodFormRef" :model="selectedPeriodData" :rules="modifyPeriodFormRules">
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-lg-8 col-12" :label-width="formLabelWidth">
        <label>{{ selectedPeriodData.creator?.displayName }} ({{ selectedPeriodData.creator?.username }})</label>
      </el-form-item>
      <el-form-item :label="t('form.label.period')" prop="periodNum" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedPeriodData.periodNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.month_range')" prop="duration" :label-width="formLabelWidth">
        <label>{{ moment(selectedPeriodData.duration[0]).format("YYYY-MM") }} - {{ moment(selectedPeriodData.duration[1]).format("YYYY-MM") }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" prop="remark" class="text-nowrap" :label-width="formLabelWidth">
        <p class="remark-text">{{ selectedPeriodData.remark }}</p>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button type="primary" @click="isShowViewPeriodDialog = false">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->
</template>
<script lang="ts">
import { defineComponent, ref, reactive, toRefs, nextTick, onActivated } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import { ElMessage, ElMessageBox, ElNotification } from "element-plus";
import { Search, EditPen, Delete, Calendar, Document, ZoomIn } from "@element-plus/icons-vue";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import PeriodSearchCriteria from "@/models/PeriodSearchCriteria";
import IReqPeriodSearchCriteriaDTO from "@/models/dto/IReqPeriodSearchCriteriaDTO";
import IResPeriodInfoDTO from "@/models/dto/IResPeriodInfoDTO";
import IReqPeriodInfoDTO from "@/models/dto/IReqPeriodInfoDTO";
import PeriodInfo from "@/models/PeriodInfo";
import ElTable from "element-plus/lib/components/table";
import IResSiteInfoDTO from "@/models/dto/IResSiteInfoDTO";
import { compare, dateFormat, delay } from "@/plugins/utility";
import { DataTableType } from "@/models/enums/DataTableType";
import numeral from "numeral";
import moment from "moment";

export default defineComponent({
  setup() {
    // #region 外部匯入 初始 預設
    const { t } = useI18n();
    const userInfoStore = useUserInfoStore();
    const paginatorSetup = usePaginatorSetup();
    const tableType = DataTableType.periodTable;
    // #endregion

    // #region state
    const state = reactive({
      isTableLoading: false,
      searchCriteria: new PeriodSearchCriteria(),
      filteredPeriods: [] as IResPeriodInfoDTO[],
      displayPeriods: [] as IResPeriodInfoDTO[],
      isSiteLoading: false,

      // pagination
      currentPage: 1,
      pageSize: 20,

      // 新增彈窗
      isShowAddPeriodDialog: false,
      isAddDialogLoading: false,
      formLabelWidth: "110px",
      newPeriodData: new PeriodInfo(),

      // 編輯彈窗
      selectedPeriodData: new PeriodInfo(),
      isShowModifyPeriodDialog: false,
      isModifyDialogLoading: false,

      // 瀏覽彈窗
      isShowViewPeriodDialog: false,

      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });
    // #endregion

    // #region 建立表單ref與Validator
    const periodNumValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "periodNum";
      if (!value) {
        return callback(new Error(t("form.validation_msg.period_is_required")));
      }

      let InputNum = Number(value);
      if (Number.isNaN(InputNum)) {
        callback(new Error(t("validation_msg.invalid_uint_value")));
        return;
      }
      
      if(InputNum <= 0) {
        callback(new Error(t("form.validation_msg.please_input_a_number_greater_then_zero")));
        return;
      }

      InputNum = Math.round(InputNum);
      state.newPeriodData[filed] = InputNum;

      var result = await API.getPeriodInfoByNum(InputNum);
      if(result.data.result === APIResultCode.success) {
        return callback(new Error(t("form.validation_msg.duplicate_period_num")))
      }

      callback();
    };

    const durationValidator = async (rule: any, value: Date[], callback: any) => {
      if (!value || value.length != 2) {
        return callback(new Error(t("form.validation_msg.duration_is_required")));
      }

      var result = await API.validatePeriodDuration(value[0], value[1], undefined);
      if(result.data.result !== APIResultCode.success) {
        return callback(new Error(t("form.validation_msg.period_overlapping_detected") + `#${result.data.content}`));
        //return callback(new Error(result.data.msg))
      }

      callback();
    };

    const addPeriodFormRules = ref({
      periodNum: [{ required: true, validator:periodNumValidator , trigger: "blur" }],
      duration: [{ required: true, validator:durationValidator, trigger: "blur" }],
    });
    const addPeriodFormRef = ref();


    const modifyPeriodNumValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "periodNum";
      if (!value) {
        return callback(new Error(t("form.validation_msg.period_is_required")));
      }

      let InputNum = Number(value);
      if (Number.isNaN(InputNum)) {
        callback(new Error(t("validation_msg.invalid_uint_value")));
        return;
      }
      
      if(InputNum <= 0) {
        callback(new Error(t("form.validation_msg.please_input_a_number_greater_then_zero")));
        return;
      }

      InputNum = Math.round(InputNum);
      state.selectedPeriodData[filed] = InputNum;

      var result = await API.getPeriodInfoByNum(InputNum);

      if(result.data.result === APIResultCode.success && result.data.content.id !== state.selectedPeriodData.id) {
        return callback(new Error(t("form.validation_msg.duplicate_period_num")))
      }

      callback();
    };

    const modifyDurationValidator = async (rule: any, value: Date[], callback: any) => {
      if (!value || value.length != 2) {
        return callback(new Error(t("form.validation_msg.duration_is_required")));
      }

      var result = await API.validatePeriodDuration(value[0], value[1], state.selectedPeriodData.id);
      if(result.data.result !== APIResultCode.success) {
        return callback(new Error(t("form.validation_msg.period_overlapping_detected") + `#${result.data.content}`));
        //return callback(new Error(result.data.msg))
      }

      callback();
    };

    const modifyPeriodFormRules = ref({
      periodNum: [{ required: true, validator:modifyPeriodNumValidator , trigger: "blur" }],
      duration: [{ required: true, validator:modifyDurationValidator, trigger: "blur" }],
    });
    const modifyPeriodFormRef = ref();

    const periodNumSearchValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "periodNum";
      console.log(`validate(uint) : ${source.field}`);
      if(!value) {
        state.searchCriteria[filed] = undefined;
        return;
      } 

      var inputNum = Number(value);
      if(Number.isNaN(inputNum)) {
        callback(new Error(t('validation_msg.invalid_uint_value')));
        return;
      }

      if(inputNum <= 0) {
        state.searchCriteria[filed] = 1;
        return;
      }

      state.searchCriteria[filed] = Math.round(inputNum);
      callback();
    }
    const searchCriteriaFormRef = ref();
    const searchCriteriaFormRules = ref({
      periodNum: [{ validator:periodNumSearchValidator , trigger: "blur" }],
    })
    const periodTableRef = ref<InstanceType<typeof ElTable>>();
    // #endregion

    // #region Hook functions
    onActivated(async () => {
      try {
        state.isTableLoading = true;
        await updateUserPreference();
        await filterperiodListAsync();
      } catch (error) {
        console.log(error);
        ElMessage({
          type: "error",
          message: "處理失敗，請查看詳細訊息",
        });
      } finally {
        state.isTableLoading = false;
      }
    });
    // #endregion

    //#region UI Events
    /** 按下搜尋 */
    const onSearchperiodClicked = async () => {
      try {
        await searchCriteriaFormRef.value?.validate();
      } catch {
        return;
      }
      
      try {
        state.isTableLoading = true;

        //清除table排序
        periodTableRef.value?.clearSort();

        await filterperiodListAsync();
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

    /** 按下並放開Enter鍵 */
    const onInputFieldEnterKeyUp = async () => {
      await onSearchperiodClicked();
    };

    /** search 清除 */
    const onClearSearchCriteriaClicked = () => {
      searchCriteriaFormRef.value?.clearValidate();
      state.searchCriteria.clear();
    };

    /** 新增彈窗 */
    const onCreatePeriodClicked = async () => {

      //初始化欄位內容
      state.newPeriodData.creator = {
        id: userInfoStore.userId,
        username: userInfoStore.username,
        displayName: userInfoStore.displayName,
      };
      state.newPeriodData.duration = [];
      state.newPeriodData.remark = undefined;
      state.isShowAddPeriodDialog = true;

      await nextTick();
      addPeriodFormRef.value?.clearValidate();
    };

    /** 新增 送出 */
    const onSubmitNewPeriodClicked = async () => {
      addPeriodFormRef.value?.validate(async (valid: boolean) => {
        if (!valid) return;

        try {
          await ElMessageBox.confirm(t("Create_Confirm_Msg"), t("Warning"), {
            confirmButtonText: t("Confirm"),
            cancelButtonText: t("Cancel"),
            type: "warning",
          });
        } catch (error) {
          return;
        }

        try {
          state.isAddDialogLoading = true;
          const createperiodResult = await API.createPeriod(state.newPeriodData.ToIReqPeriodInfoDTO());

          if (
            createperiodResult.data.result === APIResultCode.duplicate_period_name ||
            createperiodResult.data.result === APIResultCode.period_duratiuon_conflict
          ) {
            //錯誤提示: 期數衝突提示
            ElMessage({
              type: "error",
              message: createperiodResult.data.msg,
            });
            throw new Error(createperiodResult.data.msg);
          }

          if (createperiodResult.data.result !== APIResultCode.success) {
            throw new Error(createperiodResult.data.msg);
          }

          ElMessage({
            showClose: true,
            message: "新增成功",
            type: "success",
          });

          // 關閉彈窗
          state.isShowAddPeriodDialog = false;

          // // 表單regex重置
          // addPeriodFormRef.value.resetFields();

          // 刷新
          state.isTableLoading = true;
          await filterperiodListAsync();
        } catch (error) {
          console.log(error);
          ElMessage({
            type: "error",
            message: "處理失敗，請查看詳細訊息",
          });
        } finally {
          state.isAddDialogLoading = false;
          state.isTableLoading = false;
        }
      });
    };

    /** 編輯彈窗 */
    const onEditPeriodClicked = async (target: IResPeriodInfoDTO) => {
      try {
        state.isTableLoading = true;

        await getSelectedPeriodDetail(target.id);

        state.isShowModifyPeriodDialog = true;
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

    /** 編輯 送出 */
    const onSaveModifyResultClicked = async () => {
      modifyPeriodFormRef.value?.validate(async (valid: boolean) => {
        if (!valid) return;

        try {
          await ElMessageBox.confirm(t("update_confirm_msg"), t("Warning"), {
            confirmButtonText: t("Confirm"),
            cancelButtonText: t("Cancel"),
            type: "warning",
          });
        } catch (error) {
          return;
        }

        try {
          state.isModifyDialogLoading = true;

          const updateperiodResult = await API.updatePeriod(state.selectedPeriodData.id, state.selectedPeriodData.ToIReqPeriodInfoDTO());

          if (updateperiodResult.data.result === 603) {
            //錯誤提示: 專案衝突提示
            ElMessage({
              type: "error",
              message: updateperiodResult.data.msg,
            });
          }
          if (updateperiodResult.data.result !== APIResultCode.success) {
            throw new Error(updateperiodResult.data.msg);
          }

          ElMessage({
            showClose: true,
            message: "新增成功",
            type: "success",
          });

          // 關閉彈窗
          state.isShowModifyPeriodDialog = false;

          // 刷新
          state.isTableLoading = true;
          await filterperiodListAsync();
        } catch (error) {
          console.log(error);
          ElMessage({
            type: "error",
            message: "處理失敗，請查看詳細訊息",
          });
        } finally {
          state.isModifyDialogLoading = false;
          state.isTableLoading = false;
        }
      });
    };

    /** 瀏覽彈窗 */
    const onViewperiodClicked = async (target: IResPeriodInfoDTO) => {
      try {
        state.isTableLoading = true;

        await getSelectedPeriodDetail(target.id);

        state.isShowViewPeriodDialog = true;
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

    /** 刪除彈窗 */
    const onDeletePeriodClicked = async (target: IResPeriodInfoDTO) => {
      try {
        await ElMessageBox.confirm(t("Delete_Confirm_Msg"), t("Warning"), {
          confirmButtonText: t("Confirm"),
          cancelButtonText: t("Cancel"),
          type: "warning",
        });
      } catch (error) {
        console.log(error);
        return;
      }

      try {
        // 刪除
        state.isTableLoading = true;

        const deleteResponse = await API.deletePeriod(target.id);
        if (deleteResponse.data.result == APIResultCode.item_is_being_used) {
          const projectCodeList = deleteResponse.data.content;
          ElNotification({
            title: t("error"),
            message: t("delete_fail")+": "+ t("period_is_being_used"),
            type: "error",
          });
          return;
        }

        if (deleteResponse.data.result !== APIResultCode.success) {
          throw new Error(deleteResponse.data.msg);
        }

        ElMessage({
          showClose: true,
          message: t("delete_success"),
          type: "success",
        });

        await filterperiodListAsync();
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

    /** 改變排序 */
    const onSortChanged = (sort: any) => {
      //console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      try {
        state.isTableLoading = true;
        state.filteredPeriods.sort(compare(sort.prop, sort.order));
        updateDisplayList();
      } catch (error) {
        console.log(error);
      } finally {
        state.isTableLoading = false;
      }
    };

    /** 分頁輸入 */
    const onPagesizeChanged = () => {
      updateDisplayList();
    };

    const onCurrentPageChanged = () => {
      updateDisplayList();
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

    /** 更新搜尋結果 */
    const filterperiodListAsync = async () => {
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      const searchCriteriaDTO = state.searchCriteria.ToReqDTO();
      await getfilteredPeriods(searchCriteriaDTO);

      updateDisplayList();
    };

    /** 呼叫API, 取得搜尋結果 */
    const getfilteredPeriods = async (searchCriteria: IReqPeriodSearchCriteriaDTO) => {
      /* 若 開發時 等待 1 秒 */
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      const periodSearchResult = await API.searchPeriod(searchCriteria);
      if (periodSearchResult.data.result !== APIResultCode.success) {
        throw new Error(periodSearchResult.data.msg);
      }
      state.filteredPeriods = periodSearchResult.data.content;
    };

    /** 呼叫API, 取得一特定傳案的細項 */
    const getSelectedPeriodDetail = async (periodId: number) => {
      /* 若 開發時 等待 1 秒 */
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      const getPeriodDetailResult = await API.getPeriodDetailById(periodId);
      if (getPeriodDetailResult.data.result !== APIResultCode.success) {
        throw new Error(getPeriodDetailResult.data.msg);
      }

      const periodDtl = getPeriodDetailResult.data.content;

      state.selectedPeriodData.id = periodDtl.id;
      state.selectedPeriodData.creator = periodDtl.creator;
      state.selectedPeriodData.periodNum = periodDtl.periodNum;
      state.selectedPeriodData.remark = periodDtl.remark;

      state.selectedPeriodData.duration = [periodDtl.startMonth, periodDtl.endMonth];
    };

    /** 更新顯示清單 */
    const updateDisplayList = () => {
      state.displayPeriods = state.filteredPeriods.slice((state.currentPage - 1) * state.pageSize, state.currentPage * state.pageSize);
    };

    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      t,
      userInfoStore,
      addPeriodFormRef,
      addPeriodFormRules,
      modifyPeriodFormRef,
      modifyPeriodFormRules,
      searchCriteriaFormRef,
      searchCriteriaFormRules,
      periodTableRef,

      // icons
      Search,
      EditPen,
      Delete,
      Calendar,
      Document,
      ZoomIn,

      // function
      getfilteredPeriods,
      updateDisplayList,

      onCreatePeriodClicked,
      onSubmitNewPeriodClicked,
      onSearchperiodClicked,
      onSaveModifyResultClicked,
      onEditPeriodClicked,
      onDeletePeriodClicked,
      onClearSearchCriteriaClicked,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onInputFieldEnterKeyUp,
      onColDisplayCtrlPanelHidded,
      onViewperiodClicked,

      // date
      dateFormat,

      numeral,
      moment,
    };
  },
});
</script>
<style scoped>
.remark-text {
  text-align: left;
  white-space: pre;
}
/* /deep/ .el-input-number .el-input__inner{ text-align: left;} */
</style>
