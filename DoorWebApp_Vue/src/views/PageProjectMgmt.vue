<template>
  <div class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("TPS_team_code") }}</span>
    </div>

    <!-- Search Form -->
    <el-form @submit.prevent class="mt-2" :label-position="'right'" :label-width="'auto'">
      <div class="row row-cols-1 row-cols-sm-2 row-cols-md-2 row-cols-lg-4 row-cols-xl-4">
        <!-- 專案代碼 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('PJCode') + ':'">
            <el-input v-model.trim="searchCriteria.pjCode" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
          </el-form-item>
        </div>

        <!-- 建立者 -->
        <div class="col">
          <el-form-item class="w-100" :label="t('creator') + ':'">
            <el-input v-model="searchCriteria.creatorKeyword" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
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
            <el-select :placeholder="t('placeholder.select')" v-model="searchCriteria.factory" value-key="id" class="w-100" clearable>
              <el-option v-for="factory in searchCriteria.site?.factories" :key="factory.id" :label="factory.name" :value="factory" />
            </el-select>
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
      <el-button type="primary" @click="onSearchProjectClicked()">{{ t("search") }}</el-button>
    </div>

    <hr />

    <!-- 新增btn -->
    <el-row justify="end" class="mb-3" v-if="userInfoStore.permissions.some((x) => x === 420)">
      <el-col :span="2" :xs="3" :sm="3" :md="2" :lg="2" :xl="2" class="d-flex justify-content-end">
        <el-button type="primary" @click="onCreateProjectClicked()" :disabled="!userInfoStore.permissions.some((x) => x === 220)">{{
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
            <el-checkbox v-model="colDisplayCtrl[0]">{{ t("PJCode") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[1]">{{ t("period") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[2]">{{ t("startMonth") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[3]">{{ t("endMonth") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[4]">{{ t("factories") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[5]">{{ t("factorys") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[6]">{{ t("team") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[7]">{{ t("payRate") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[8]">{{ t("creator") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[9]">{{ t("createDate") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[10]">{{ t("improvement_project") }}</el-checkbox>
          </div>
        </el-popover>
      </div>
      <el-table
        ref="projectTableRef"
        class="w-100"
        v-loading.fullscreen.lock="isTableLoading"
        :stripe="true"
        :data="displayProjects"
        :cell-style="cellStyle"
        @sort-change="onSortChanged"
      >
        <el-table-column min-width="140" v-if="colDisplayCtrl[0]" sortable="custom" :label="t('PJCode')" prop="pjCode" />
        <el-table-column min-width="80" v-if="colDisplayCtrl[1]" sortable="custom" :label="t('period')" prop="periodNum" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[2]" sortable="custom" :label="t('startMonth')" prop="startMonth" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[3]" sortable="custom" :label="t('endMonth')" prop="endMonth" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[4]" sortable="custom" :label="t('factories')" prop="siteName" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[5]" sortable="custom" :label="t('factorys')" prop="factoryName" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[6]" sortable="custom" :label="t('team')" prop="teamNum" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[7]" sortable="custom" :label="t('payRate')" prop="workRate" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[8]" sortable="custom" :label="t('creator')" prop="creatorDisplayName" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[9]" sortable="custom" :label="t('createDate')" prop="createTime" />
        <el-table-column min-width="120" v-if="colDisplayCtrl[10]" align="center" :label="t('improvement_project')" prop="improvementProject">
          <template #default="scope">
            <el-button link type="primary" @click="onKaizenItemLinkClicked(scope.row)">
              <el-icon :size="16"><Document /></el-icon>
            </el-button>
          </template>
        </el-table-column>
        <el-table-column width="80" :label="t('operation')" prop="operate" class="operateBtnGroup d-flex" fixed="right">
          <template #default="scope">
            <el-button
              link
              type="success"
              @click="onEditProjectClicked(scope.row)"
              v-if="userInfoStore.permissions.some((x) => x === 420)"
            >
              <el-icon :size="16"><EditPen /></el-icon>
            </el-button>
            <el-button link type="primary" @click="onViewProjectClicked(scope.row)" v-else>
              <el-icon><ZoomIn /></el-icon>
            </el-button>

            <el-button
              link
              type="danger"
              @click="onDeleteProjectClicked(scope.row)"
              v-if="userInfoStore.permissions.some((x) => x === 420)"
            >
              <el-icon :size="16"><Delete /></el-icon>
            </el-button>
            <el-button v-else link type="info" @click="onDeleteProjectClicked(scope.row)" :disabled="true">
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
            :total="filteredProjects.length"
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
  <el-dialog class="dialog" v-model="isShowAddDialog" :title="t('create')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isAddDialogLoading" ref="createProjectFormRef" :model="newProjectData" :rules="projectFormRules">
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-lg-8 col-12" :label-width="formLabelWidth">
        <label>{{ newProjectData.creator?.displayName }} ({{ newProjectData.creator?.username }})</label>
      </el-form-item>
      <el-form-item :label="t('form.label.period')" prop="period" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-select v-model="newProjectData.period" :placeholder="t('placeholder.input')" value-key="id">
          <el-option v-for="period in periods" :key="period.id" :label="period.periodNum" :value="period" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('form.label.month_range')" prop="duration" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ newProjectData.period?.startMonth }} ~ {{ newProjectData.period?.endMonth }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.site')" prop="site" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-select
          :placeholder="t('placeholder.select')"
          :loading="isSiteLoading"
          :loading-text="t('loading')"
          @visible-change="onSiteListShown"
          v-model="newProjectData.site"
          value-key="id"
          class="w-100"
          @change="newProjectData.factory = undefined"
        >
          <el-option v-for="site in sites" :key="site.id" :label="site.name" :value="site" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('form.label.factory')" prop="factory" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-select :placeholder="t('placeholder.select_hint')" v-model="newProjectData.factory" value-key="id" class="w-100">
          <el-option v-for="factory in newProjectData.site?.factories" :key="factory.id" :label="factory.name" :value="factory" />
        </el-select>
      </el-form-item>

      <el-form-item :label="t('form.label.team_name')" prop="teamNum" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-input v-model.trim="newProjectData.teamNum" :placeholder="t('placeholder.input')" />
      </el-form-item>
      <el-form-item :label="t('form.label.work_rate')" prop="workRate" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-input-number v-model="newProjectData.workRate" :min="0"  :precision="2" :controls="false" />
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" prop="remark" class="text-nowrap" :label-width="formLabelWidth">
        <el-input v-model="newProjectData.remark" :rows="4" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowAddDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSubmitNewProjectClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /新增彈窗 -->

  <!-- 編輯彈窗 -->
  <el-dialog class="dialog" v-model="isShowModifyDialog" :title="t('modify')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isModifyDialogLoading" ref="editProjectFormRef" :model="selectedProjectData" :rules="projectFormRules">
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-lg-8 col-12" :label-width="formLabelWidth">
        <label>{{ selectedProjectData.creator?.displayName }} ({{ selectedProjectData.creator?.username }})</label>
      </el-form-item>
      <el-form-item :label="t('form.label.period')" prop="period" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.period?.periodNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.month_range')" prop="duration" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.period?.startMonth }} ~ {{ selectedProjectData.period?.endMonth }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.site')" prop="site" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.site?.name }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.factory')" prop="factory" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.factory?.name }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.team_name')" prop="teamNum" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.teamNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.work_rate')" prop="workRate" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <el-input-number v-model="selectedProjectData.workRate" :min="0" :precision="2" :controls="false" />
      </el-form-item>

      <el-form-item :label="t('form.label.remark')" prop="remark" class="text-nowrap" :label-width="formLabelWidth">
        <el-input v-model="selectedProjectData.remark" :rows="4" type="textarea" :placeholder="t('placeholder.input')" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifyDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSaveModifyResultClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->

  <!-- 瀏覽彈窗 -->
  <el-dialog class="dialog" v-model="isShowViewDialog" :title="t('detail')">
    <el-form @submit.prevent v-loading.fullscreen.lock="isModifyDialogLoading" ref="editProjectFormRef" :model="selectedProjectData" :rules="projectFormRules">
      <el-form-item :label="t('form.label.creator')" prop="creator" class="text-nowrap col-lg-8 col-12" :label-width="formLabelWidth">
        <label>{{ selectedProjectData.creator?.displayName }} ({{ selectedProjectData.creator?.username }})</label>
      </el-form-item>
      <el-form-item :label="t('form.label.period')" prop="duration" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.period?.periodNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.month_range')" prop="duration" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.period?.startMonth }} ~ {{ selectedProjectData.period?.endMonth }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.site')" prop="site" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.site?.name }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.factory')" prop="factory" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.factory?.name }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.team_name')" prop="teamNum" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ selectedProjectData.teamNum }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.work_rate')" prop="workRate" :label-width="formLabelWidth" class="col-lg-8 col-12">
        <label>{{ numeral(selectedProjectData.workRate).format("0.0") }}</label>
      </el-form-item>
      <el-form-item :label="t('form.label.remark')" prop="remark" class="text-nowrap" :label-width="formLabelWidth">
        <p class="remark-text">{{ selectedProjectData.remark }}</p>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button type="primary" @click="isShowViewDialog = false">{{ t("Confirm") }}</el-button>
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
import IReqProjectSearchCriteriaDTO from "@/models/dto/IReqProjectSearchCriteriaDTO";
import IResProjectInfoDTO from "@/models/dto/IResProjectInfoDTO";
import ProjectInfo from "@/models/ProjectInfo";
import ProjectSearchCriteria from "@/models/ProjectSearchCriteria";
import ElTable from "element-plus/lib/components/table";
import IResSiteInfoDTO from "@/models/dto/IResSiteInfoDTO";
import { compare, dateFormat, delay } from "@/plugins/utility";
import { DataTableType } from "@/models/enums/DataTableType";
import numeral from "numeral";
import moment from "moment";
import IResPeriodInfoDTO from "@/models/dto/IResPeriodInfoDTO";

export default defineComponent({
  setup() {
    // #region 外部匯入 初始 預設
    const { t } = useI18n();
    const userInfoStore = useUserInfoStore();
    const router = useRouter();
    const paginatorSetup = usePaginatorSetup();
    const tableType = DataTableType.projectTable;
    // #endregion

    // #region state
    const state = reactive({
      isTableLoading: false,
      searchCriteria: new ProjectSearchCriteria(),
      filteredProjects: [] as IResProjectInfoDTO[],
      displayProjects: [] as IResProjectInfoDTO[],
      isSiteLoading: false,

      // pagination
      currentPage: 1,
      pageSize: 20,

      // 新增彈窗
      isShowAddDialog: false,
      isAddDialogLoading: false,
      formLabelWidth: "110px",
      newProjectData: new ProjectInfo(),

      // 編輯彈窗
      selectedProjectData: new ProjectInfo(),
      isShowModifyDialog: false,
      isModifyDialogLoading: false,

      // 瀏覽彈窗
      isShowViewDialog: false,

      // 廠區select
      sites: [] as IResSiteInfoDTO[],

      // 期數select
      periods: [] as IResPeriodInfoDTO[],

      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });
    // #endregion

    // #region 建立表單ref與Validator
    const workRateValidator = (rule: any, value: any, callback: any) => {
      if (!value || value <= 0) {
        return callback(new Error(t("validation_msg.work_rate_is_required")));
      }

      if(value <= 0 ) {
        return callback(new Error(t("validation_msg.invalid_work_rate")));
      }

      // if (!(0 <= value && value <= 100.0)) {
      //   return callback(new Error(t("validation_msg.invalid_work_rate")));
      // }

      callback();
    };

    const teamNumValidator = (rule: any, value: any, callback: any) => {
      if (!value) {
        return callback(new Error(t("form.validation_msg.team_name_is_required")));
      }

      const teamNumInt = parseInt(value, 10);
      if (Number.isNaN(teamNumInt)) {
        console.log("value is not a number");
        return callback(new Error(t("form.validation_msg.please_input_a_number")));
      }

      if (!(1 <= teamNumInt && teamNumInt <= 99)) {
        return callback(new Error(t("form.validation_msg.please_input_a_number_between_1_and_99")));
      }

      state.newProjectData.teamNum = teamNumInt;
      callback();
    };
    const projectFormRules = ref({
      site: [{ required: true, message: () => t("form.validation_msg.site_is_required"), trigger: "blur" }],
      factory: [{ required: true, message: () => t("form.validation_msg.factory_is_required"), trigger: "blur" }],
      teamNum: [{ required: true, validator: teamNumValidator, trigger: "blur" }],
      workRate: [{ required: true, validator: workRateValidator, trigger: "blur" }],
      period: [{ required: true, message: () => t("form.validation_msg.period_is_required"), trigger: "blur" }],
    });

    const createProjectFormRef = ref();
    const editProjectFormRef = ref();
    const projectTableRef = ref<InstanceType<typeof ElTable>>();
    // #endregion

    // #region Hook functions
    onActivated(async () => {
      try {
        state.isTableLoading = true;
        await updateUserPreference();
        await updateSiteList();

        await filterProjectListAsync();
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
    const onSearchProjectClicked = async () => {
      try {
        state.isTableLoading = true;

        //清除table排序
        projectTableRef.value?.clearSort();

        await filterProjectListAsync();
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
      await onSearchProjectClicked();
    };

    /** search 清除 */
    const onClearSearchCriteriaClicked = () => {
      state.searchCriteria.clear();
    };

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

    /** 下拉選單連動 */
    const onChangeSelect = (factorys: string[]) => {
      //state.factorys = factorys;
    };

    /** 按下改善事項超連結, 跳轉頁面至改善事項，同時帶入當前的傳案代碼 */
    const onKaizenItemLinkClicked = (target: IResProjectInfoDTO) => {
      //console.log("onKaizenItemLinkClicked : " + target.pjCode);
      router.push({
        name: "kaizen_list2",
        params: {
          pjcode: target.pjCode,
        },
      });
    };

    /** 新增彈窗 */
    const onCreateProjectClicked = async () => {
      await updateSiteList();
      await updatePeriodList();

      //初始化欄位內容
      state.newProjectData.creator = {
        id: userInfoStore.userId,
        username: userInfoStore.username,
        displayName: userInfoStore.displayName,
      };
      state.newProjectData.period = state.periods[0];
      state.newProjectData.site = undefined;
      state.newProjectData.factory = undefined;
      state.newProjectData.teamNum = undefined;
      state.newProjectData.workRate = undefined;
      state.newProjectData.remark = undefined;
      state.isShowAddDialog = true;

      await nextTick();
      createProjectFormRef.value?.clearValidate();
    };

    /** 新增 送出 */
    const onSubmitNewProjectClicked = async () => {
      createProjectFormRef.value?.validate(async (valid: boolean) => {
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
          const createProjectResult = await API.createProject(state.newProjectData.ToIReqProjectInfoDTO());

          if (createProjectResult.data.result === 613) {
            //錯誤提示: 專案衝突提示
            ElMessage({
              type: "error",
              message: createProjectResult.data.msg,
            });
            throw new Error(createProjectResult.data.msg);
          }

          if (createProjectResult.data.result !== APIResultCode.success) {
            throw new Error(createProjectResult.data.msg);
          }

          ElMessage({
            showClose: true,
            message: "新增成功",
            type: "success",
          });

          // 關閉彈窗
          state.isShowAddDialog = false;

          // // 表單regex重置
          // createProjectFormRef.value.resetFields();

          // 刷新
          state.isTableLoading = true;
          await filterProjectListAsync();
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
    const onEditProjectClicked = async (target: IResProjectInfoDTO) => {
      try {
        state.isTableLoading = true;

        // await updateSiteList();
        // await updatePeriodList();
        await getSelectedProjectDetail(target.id);

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

    /** 編輯 送出 */
    const onSaveModifyResultClicked = async () => {
      editProjectFormRef.value?.validate(async (valid: boolean) => {
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

          const updateProjectResult = await API.updateProject(state.selectedProjectData.id, state.selectedProjectData.ToIReqProjectInfoDTO());

          if (updateProjectResult.data.result === 603) {
            //錯誤提示: 專案衝突提示
            ElMessage({
              type: "error",
              message: updateProjectResult.data.msg,
            });
          }
          if (updateProjectResult.data.result !== APIResultCode.success) {
            throw new Error(updateProjectResult.data.msg);
          }

          ElMessage({
            showClose: true,
            message: "新增成功",
            type: "success",
          });

          // 關閉彈窗
          state.isShowModifyDialog = false;

          // // 表單regex重置
          // createProjectFormRef.value.resetFields();

          // 刷新
          state.isTableLoading = true;
          await filterProjectListAsync();
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
    const onViewProjectClicked = async (target: IResProjectInfoDTO) => {
      try {
        state.isTableLoading = true;

        //await updateSiteList();
        await getSelectedProjectDetail(target.id);

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

    /** 刪除彈窗 */
    const onDeleteProjectClicked = async (target: IResProjectInfoDTO) => {
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

        const deleteResponse = await API.deleteProject(target.id);
        if (deleteResponse.data.result == APIResultCode.item_is_being_used) {
          const kaizenItemCodeList = deleteResponse.data.content;
          ElNotification({
            title: t("error"),
            message:  t("delete_fail")+": "+ t("project_is_being_used"),
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

        await filterProjectListAsync();
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
        state.filteredProjects.sort(compare(sort.prop, sort.order));
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

    /** 呼叫API, 更新期數清單 */
    const updatePeriodList = async () => {
      try {
        const getPeriodsResult = await API.getAvailablePeriodItems();

        if (getPeriodsResult.data.result !== APIResultCode.success) {
          throw new Error(getPeriodsResult.data.msg);
        }

        state.periods = getPeriodsResult.data.content;
      } catch (error) {
        console.log(error);
      }
    };

    /** 更新搜尋結果 */
    const filterProjectListAsync = async () => {
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      const searchCriteriaDTO = state.searchCriteria.ToReqDTO();
      await getFilteredProjects(searchCriteriaDTO);

      updateDisplayList();
    };

    /** cellstyle */
    const cellStyle = (column: any) => {
      if (column.label === "改善項目") {
        return {
          color: "#2F80ED",
        };
      }
    };

    /** 呼叫API, 取得搜尋結果 */
    const getFilteredProjects = async (searchCriteria: IReqProjectSearchCriteriaDTO) => {
      /* 若 開發時 等待 1 秒 */
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      const projectSearchResult = await API.searchProject(searchCriteria);
      if (projectSearchResult.data.result !== APIResultCode.success) {
        throw new Error(projectSearchResult.data.msg);
      }
      state.filteredProjects = projectSearchResult.data.content;
    };

    /** 呼叫API, 取得一特定傳案的細項 */
    const getSelectedProjectDetail = async (projectId: number) => {
      /* 若 開發時 等待 1 秒 */
      if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

      const getProjectDetailResult = await API.getProjectById(projectId);
      if (getProjectDetailResult.data.result !== APIResultCode.success) {
        throw new Error(getProjectDetailResult.data.msg);
      }

      const pjDtl = getProjectDetailResult.data.content;
      state.selectedProjectData.id = pjDtl.id;
      state.selectedProjectData.creator = pjDtl.creator;
      state.selectedProjectData.pjcode = pjDtl.pjCode;
      //state.selectedProjectData.period = pjDtl.periodNum;
      state.selectedProjectData.period = pjDtl.period;
      state.selectedProjectData.site = pjDtl.site;
      state.selectedProjectData.factory = pjDtl.factory;
      state.selectedProjectData.teamNum = pjDtl.teamNum;
      state.selectedProjectData.workRate = pjDtl.workRate;
      state.selectedProjectData.remark = pjDtl.remark;
    };

    /** 更新顯示清單 */
    const updateDisplayList = () => {
      state.displayProjects = state.filteredProjects.slice((state.currentPage - 1) * state.pageSize, state.currentPage * state.pageSize);
    };

    //#endregion

    return {
      ...toRefs(state),
      paginatorSetup,
      t,
      userInfoStore,
      createProjectFormRef,
      editProjectFormRef,
      projectFormRules,
      projectTableRef,

      // icons
      Search,
      EditPen,
      Delete,
      Calendar,
      Document,
      ZoomIn,

      // function
      getFilteredProjects,
      updateDisplayList,

      onCreateProjectClicked,
      onSubmitNewProjectClicked,
      onSearchProjectClicked,
      onSaveModifyResultClicked,
      onEditProjectClicked,
      onDeleteProjectClicked,
      onClearSearchCriteriaClicked,
      onChangeSelect,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onSiteListShown,
      onKaizenItemLinkClicked,
      onInputFieldEnterKeyUp,
      onColDisplayCtrlPanelHidded,
      onViewProjectClicked,

      // date
      dateFormat,

      // cell-style
      cellStyle,
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
</style>
