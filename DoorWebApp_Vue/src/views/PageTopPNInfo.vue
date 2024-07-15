<template>
  <div v-loading.fullscreen.lock="isTableLoading" class="content-body mt-3 d-flex flex-column justify-content-start align-content-start px-3">
    <!-- Title -->
    <div class="text-start mb-3">
      <span class="fs-4 fw-bold content-title">{{ t("create_new_part_no") }}</span>
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
      <div class="row row-cols-1 row-cols-sm-2 row-cols-md-2 row-cols-lg-2 row-cols-xl-4">
        <!-- Input: 成品料號 -->
        <div class="col">
          <el-form-item class="w-100" tem :label="t('form.label.top_pn')">
            <el-input v-model="searchCriteria.topPNKeyword" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
          </el-form-item>
        </div>

        <!-- 期數 -->
        <div class="col">
          <el-form-item class="w-100" prop="periodNum" :label="t('period') + ':'">
            <el-input v-model="searchCriteria.periodNum" @keyup.enter="onInputFieldEnterKeyUp" clearable :placeholder="t('placeholder.input')" />
          </el-form-item>
        </div>
      </div>
    </el-form>

    <!-- Btn: 清除 搜尋 -->
    <div class="d-flex flex-row justify-content-end">
      <el-button @click="onClearSearchTopPNInfoClicked()">{{ t("clear") }}</el-button>
      <el-button type="primary" @click="onSearchTopPNInfoClicked()">{{ t("search") }}</el-button>
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
            <el-checkbox v-model="colDisplayCtrl[0]">{{ t("top_pn") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[1]">{{ t("period") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[2]">{{ t("area_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[3]">{{ t("area_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[4]">{{ t("processing_leadtime_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[5]">{{ t("processing_leadtime_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[6]">{{ t("vender_leadtime_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[7]">{{ t("vender_leadtime_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[8]">{{ t("pph_before_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[9]">{{ t("pph_after_improvement") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[10]">{{ t("createDate") }}</el-checkbox>
            <el-checkbox v-model="colDisplayCtrl[11]">{{ t("modifiedDate") }}</el-checkbox>
          </div>
        </el-popover>
      </div>

      <el-table ref="TopPNInfoTableRef" class="w-100" :data="displayTopPNInfoList" :stripe="true" @sort-change="onSortChanged">
        <el-table-column min-width="140" v-if="colDisplayCtrl[0]" sortable="custom" prop="topPN" :label="t('top_pn')" />
        <el-table-column min-width="100" v-if="colDisplayCtrl[1]" sortable="custom" prop="periodNum" align="center" :label="t('period')" />
        <el-table-column
          min-width="140"
          v-if="colDisplayCtrl[2]"
          align="center"
          sortable="custom"
          prop="areaBeforeImp"
          :label="t('area_before_improvement')"
        />
        <el-table-column
          min-width="140"
          v-if="colDisplayCtrl[3]"
          align="center"
          sortable="custom"
          prop="areaAfterImp"
          :label="t('area_after_improvement')"
        />
        <el-table-column
          min-width="140"
          v-if="colDisplayCtrl[4]"
          align="center"
          sortable="custom"
          prop="procLTBeforeImp"
          :label="t('processing_leadtime_before_improvement')"
        />
        <el-table-column
          min-width="140"
          v-if="colDisplayCtrl[5]"
          align="center"
          sortable="custom"
          prop="procLTAfterImp"
          :label="t('processing_leadtime_after_improvement')"
        />
        <el-table-column
          min-width="160"
          v-if="colDisplayCtrl[6]"
          align="center"
          sortable="custom"
          prop="venderLTBeforeImp"
          :label="t('vender_leadtime_before_improvement')"
        />
        <el-table-column
          min-width="160"
          v-if="colDisplayCtrl[7]"
          align="center"
          sortable="custom"
          prop="venderLTAfterImp"
          :label="t('vender_leadtime_after_improvement')"
        />
        <el-table-column
          min-width="140"
          v-if="colDisplayCtrl[8]"
          align="center"
          sortable="custom"
          prop="pphBeforeImp"
          :label="t('pph_before_improvement')"
        />
        <el-table-column
          min-width="140"
          v-if="colDisplayCtrl[9]"
          align="center"
          sortable="custom"
          prop="pphAfterImp"
          :label="t('pph_after_improvement')"
        />
        <el-table-column min-width="220" v-if="colDisplayCtrl[10]" align="center" sortable="custom" prop="createTime" :label="t('createDate')" />
        <el-table-column min-width="220" v-if="colDisplayCtrl[11]" align="center" sortable="custom" prop="modifiedTime" :label="t('modifiedDate')" />
        <el-table-column width="60" align="center" :label="t('operation')" prop="operate" class="operateBtnGroup d-flex" fixed="right">
          <template #default="scope">
            <el-button link type="success" @click="onEditTopPNInfoClicked(scope.row)" v-if="userInfoStore.permissions.some((x) => x === 320)">
              <el-icon :size="16"><EditPen /></el-icon>
            </el-button>
            <el-button v-else link type="primary" @click="onViewTopPNInfoClicked(scope.row)">
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
  <el-dialog class="top-pn-modify-dialog" :top="dialogTopVh" v-model="isShowModifyDialog" :title="t('modify')">
    <el-form
      @submit.prevent
      v-loading.fullscreen.lock="isModifyDialogLoading"
      ref="modifyTopPNInfoFormRef"
      :model="selectedTopPNInfo"
      :rules="modifyTopPNInfoFormRules"
      :label-position="formLabelPosition"
      :label-width="formLabelWidth"
    >
      <div class="mb-3 row row-cols-2 row-cols-sm-3">
        <div class="col text-start">
          <label>{{ t("form.label.period") }}</label>
          <label class="ms-1">{{ selectedTopPNInfo.periodNum }}</label>
        </div>
        <div class="col text-start">
          <label>{{ t("form.label.top_pn") }}</label>
          <label class="ms-1">{{ selectedTopPNInfo.topPN }}</label>
        </div>
      </div>

      <div class="row row-cols-1 row-cols-sm-2">
        <!-- 段落: 改善前  -->
        <div class="col">
          <div class="d-flex flex-column justify-content-start align-items-start mb-3">
            <span>{{ t("before_improvement") }}</span>
            <hr class="w-100 my-0" />
          </div>

          <!-- 改善前面積 -->
          <el-form-item class="mb-4" :size="formItemSize" prop="areaBeforeImp" :label="t('form.label.area_before_improvement')">
            <div class="w-100 d-flex flex-row">
              <el-input
                class="ms-3 ms-md-0 number_input_box"
                :precision="1"
                :step="0.1"
                :min="0.0"
                v-model="selectedTopPNInfo.areaBeforeImp"
                clearable
                :placeholder="t('placeholder.input')"
              />
              <span class="ms-1">(m<sup>2</sup>)</span>
            </div>
          </el-form-item>

          <!-- 原製程L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="procLTBeforeImp" :label="t('form.label.processing_leadtime_before_improvement')">
            <el-input
              class="ms-3 ms-md-0 number_input_box"
              :precision="0"
              :step="1"
              :min="0"
              v-model="selectedTopPNInfo.procLTBeforeImp"
              clearable
              :placeholder="t('placeholder.input')"
            />
            <span class="ms-1">({{ t("day") }})</span>
          </el-form-item>

          <!-- 改善前供應商L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="venderLTBeforeImp" :label="t('form.label.vender_leadtime_before_improvement')">
            <el-input
              class="ms-3 ms-md-0 number_input_box"
              :precision="0"
              :step="1"
              :min="0"
              v-model="selectedTopPNInfo.venderLTBeforeImp"
              clearable
              :placeholder="t('placeholder.input')"
            />
            <span class="ms-1">({{ t("day") }})</span>
          </el-form-item>

          <!-- 改善前PPH -->
          <el-form-item class="mb-4" :size="formItemSize" prop="pphBeforeImp" :label="t('form.label.pph_before_improvement')">
            <el-input
              class="ms-3 ms-md-0 number_input_box"
              v-model="selectedTopPNInfo.pphBeforeImp"
              :precision="1"
              :step="0.1"
              :min="0.0"
              clearable
              :placeholder="t('placeholder.input')"
            />
            <span class="ms-1">({{ t("pcs") }})</span>
          </el-form-item>
        </div>

        <!-- 段落: 改善後  -->
        <div class="col">
          <div class="d-flex flex-column justify-content-start align-items-start mb-3">
            <span>{{ t("after_improvement") }}</span>
            <hr class="w-100 my-0" />
          </div>

          <!-- 改善後面積 -->
          <el-form-item class="mb-4" :size="formItemSize" prop="areaAfterImp" :label="t('form.label.area_after_improvement')">
            <el-input
              class="ms-3 ms-md-0 number_input_box"
              :precision="1"
              :step="0.1"
              :min="0.0"
              v-model="selectedTopPNInfo.areaAfterImp"
              clearable
              :placeholder="t('placeholder.input')"
            />
            <span class="ms-1">(m<sup>2</sup>)</span>
          </el-form-item>

          <!-- 改善後製程L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="procLTAfterImp" :label="t('form.label.processing_leadtime_after_improvement')">
            <el-input
              class="ms-3 ms-md-0 number_input_box"
              :precision="0"
              :step="1"
              :min="0"
              v-model="selectedTopPNInfo.procLTAfterImp"
              clearable
              :placeholder="t('placeholder.input')"
            />
            <span class="ms-1">({{ t("day") }})</span>
          </el-form-item>

          <!-- 改善後供應商L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="venderLTAfterImp" :label="t('form.label.vender_leadtime_after_improvement')">
            <el-input
              class="ms-3 ms-md-0 number_input_box"
              :precision="0"
              :step="1"
              :min="0"
              v-model="selectedTopPNInfo.venderLTAfterImp"
              clearable
              :placeholder="t('placeholder.input')"
            />
            <span class="ms-1">({{ t("day") }})</span>
          </el-form-item>

          <!-- 改善後PPH -->
          <el-form-item class="mb-4" :size="formItemSize" prop="pphAfterImp" :label="t('form.label.pph_after_improvement')">
            <el-input
              class="ms-3 ms-md-0 number_input_box"
              v-model="selectedTopPNInfo.pphAfterImp"
              :precision="1"
              :step="0.1"
              :min="0.0"
              clearable
              :placeholder="t('placeholder.input')"
            />
            <span class="ms-1">({{ t("pcs") }})</span>
          </el-form-item>
        </div>
      </div>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowModifyDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary" @click="onSaveModifyResultClicked()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>

  <!-- 瀏覽彈窗 -->
  <el-dialog class="top-pn-modify-dialog" :top="dialogTopVh" v-model="isShowViewDialog" :title="t('detail')">
    <el-form :model="selectedTopPNInfo" :label-position="formLabelPosition" :label-width="formLabelWidth">
      <div class="mb-3 row row-cols-2 row-cols-sm-3">
        <div class="col text-start">
          <label>{{ t("form.label.period") }}</label>
          <label>{{ selectedTopPNInfo.periodNum }}</label>
        </div>
        <div class="col text-start">
          <label>{{ t("form.label.top_pn") }}</label>
          <label>{{ selectedTopPNInfo.topPN }}</label>
        </div>
      </div>

      <div class="row row-cols-1 row-cols-sm-2">
        <!-- 段落: 改善前  -->
        <div class="col">
          <div class="d-flex flex-column justify-content-start align-items-start mb-3">
            <span>{{ t("before_improvement") }}</span>
            <hr class="w-100 my-0" />
          </div>

          <!-- 改善前面積 -->
          <el-form-item class="mb-4" :size="formItemSize" prop="areaBeforeImp" :label="t('form.label.area_before_improvement')">
            <div v-if="selectedTopPNInfo.areaBeforeImp">
              <label>{{ numeral(selectedTopPNInfo.areaBeforeImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">(m<sup>2</sup>)</span>
            </div>
          </el-form-item>

          <!-- 原製程L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="procLTBeforeImp" :label="t('form.label.processing_leadtime_before_improvement')">
            <div v-if="selectedTopPNInfo.procLTBeforeImp">
              <label>{{ numeral(selectedTopPNInfo.procLTBeforeImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">({{ t("day") }})</span>
            </div>
          </el-form-item>

          <!-- 改善前供應商L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="venderLTBeforeImp" :label="t('form.label.vender_leadtime_before_improvement')">
            <div v-if="selectedTopPNInfo.venderLTBeforeImp">
              <label>{{ numeral(selectedTopPNInfo.venderLTBeforeImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">({{ t("day") }})</span>
            </div>
          </el-form-item>

          <!-- 改善前PPH -->
          <el-form-item class="mb-4" :size="formItemSize" prop="pphBeforeImp" :label="t('form.label.pph_before_improvement')">
            <div v-if="selectedTopPNInfo.pphBeforeImp">
              <label>{{ numeral(selectedTopPNInfo.pphBeforeImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">({{ t("pcs") }})</span>
            </div>
          </el-form-item>
        </div>

        <!-- 段落: 改善後  -->
        <div class="col">
          <div class="d-flex flex-column justify-content-start align-items-start mb-3">
            <span>{{ t("after_improvement") }}</span>
            <hr class="w-100 my-0" />
          </div>

          <!-- 改善後面積 -->
          <el-form-item class="mb-4" :size="formItemSize" prop="areaAfterImp" :label="t('form.label.area_after_improvement')">
            <div v-if="selectedTopPNInfo.areaAfterImp">
              <label>{{ numeral(selectedTopPNInfo.areaAfterImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">(m<sup>2</sup>)</span>
            </div>
          </el-form-item>

          <!-- 改善後製程L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="procLTAfterImp" :label="t('form.label.processing_leadtime_after_improvement')">
            <div v-if="selectedTopPNInfo.procLTAfterImp">
              <label>{{ numeral(selectedTopPNInfo.procLTAfterImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">({{ t("day") }})</span>
            </div>
          </el-form-item>

          <!-- 改善後供應商L/T -->
          <el-form-item class="mb-4" :size="formItemSize" prop="venderLTAfterImp" :label="t('form.label.vender_leadtime_after_improvement')">
            <div v-if="selectedTopPNInfo.venderLTAfterImp">
              <label>{{ numeral(selectedTopPNInfo.venderLTAfterImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">({{ t("day") }})</span>
            </div>
          </el-form-item>

          <!-- 改善後PPH -->
          <el-form-item class="mb-4" :size="formItemSize" prop="pphAfterImp" :label="t('form.label.pph_after_improvement')">
            <div v-if="selectedTopPNInfo.pphAfterImp">
              <label>{{ numeral(selectedTopPNInfo.pphAfterImp).format("0.0") }}</label>
              <span class="ms-1 ms-sm-2">({{ t("pcs") }})</span>
            </div>
          </el-form-item>
        </div>
      </div>
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
import { useI18n } from "vue-i18n";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import _ from "lodash";
import { usePaginatorSetup } from "@/stores/PaginatorStore";
import { ElMessage, ElMessageBox } from "element-plus";
import IReqTopPNInfoDTO from "@/models/dto/IReqTopPNInfoDTO";
import IReqTopPNInfoSearchDTO from "@/models/dto/IReqTopPNInfoSearchDTO";
import IResTopPNInfoDetailDTO from "@/models/dto/IResTopPNInfoDetailDTO";
import IResTopPNInfoTableViewModelDTO from "@/models/dto/IResTopPNInfoTableViewModelDTO";
import ElTable from "element-plus/lib/components/table";
import { delay } from "@/plugins/utility";
import { DataTableType } from "@/models/enums/DataTableType";
import numeral from "numeral";

export default defineComponent({
  props: {
    pjcode: String,
  },
  setup(props) {
    const { t } = useI18n();
    const userInfoStore = useUserInfoStore();
    const paginatorSetup = usePaginatorSetup();
    const tableType = DataTableType.topPNInfoTable;

    const state = reactive({
      isTableLoading: false,
      dialogTopVh: "1vh",
      formLabelPosition: "right" as "top" | "left" | "right",
      formItemSize: "default" as "default" | "small",
      formLabelWidth: "180px" as string,

      // 成品料號改善清單
      displayTopPNInfoList: [] as IResTopPNInfoTableViewModelDTO[],

      //搜尋改善事項參數
      searchCriteria: {
        page: 1,
        pageSize: 20,
      } as IReqTopPNInfoSearchDTO,

      isShowModifyDialog: false,
      isModifyDialogLoading: false,
      isShowViewDialog: false,
      selectedTopPNInfo: {} as IResTopPNInfoDetailDTO,

      filteredTotal: 0,
      colDisplayCtrl: new Array(50).fill(true) as boolean[],
    });

    //#region 建立表單ref與Validator
    const TopPNInfoTableRef = ref<InstanceType<typeof ElTable>>();

    const ufloatValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "areaBeforeImp" | "pphBeforeImp" | "areaAfterImp" | "pphAfterImp";
      console.log(`validate(ufloat) : ${source.field}`);

      if (!value) {
        state.selectedTopPNInfo[filed] = undefined;
        return;
      }

      let InputNum = Number(value);
      if (Number.isNaN(InputNum)) {
        console.log("not a number");
        callback(new Error(t("validation_msg.invalid_ufloat_value")));
        return;
      }

      if (InputNum <= 0) {
        console.log("not a number");
        callback(new Error(t("validation_msg.invalid_ufloat_value")));
        return;
      }

      state.selectedTopPNInfo[filed] = InputNum;
      callback();
    };

    const uintValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "procLTBeforeImp" | "venderLTBeforeImp" | "procLTAfterImp" | "venderLTAfterImp";
      console.log(`validate(uint) : ${source.field}`);

      if (!value) {
        state.selectedTopPNInfo[filed] = undefined;
        return;
      }

      let InputNum = Number(value);
      if (Number.isNaN(InputNum)) {
        callback(new Error(t("validation_msg.invalid_uint_value")));
        return;
      }

      if (InputNum <= 0) {
        console.log("not a number");
        callback(new Error(t("validation_msg.invalid_uint_value")));
        return;
      }

      state.selectedTopPNInfo[filed] = Math.round(InputNum);
      callback();
    };

    const modifyTopPNInfoFormRef = ref();
    const modifyTopPNInfoFormRules = ref({
      areaBeforeImp: [{ required: false, validator: ufloatValidator, trigger: "blur" }],
      procLTBeforeImp: [{ required: false, validator: uintValidator, trigger: "blur" }],
      venderLTBeforeImp: [{ required: false, validator: uintValidator, trigger: "blur" }],
      pphBeforeImp: [{ required: false, validator: ufloatValidator, trigger: "blur" }],

      areaAfterImp: [{ required: false, validator: ufloatValidator, trigger: "blur" }],
      procLTAfterImp: [{ required: false, validator: uintValidator, trigger: "blur" }],
      venderLTAfterImp: [{ required: false, validator: uintValidator, trigger: "blur" }],
      pphAfterImp: [{ required: false, validator: ufloatValidator, trigger: "blur" }],
    });

    const periodNumSearchValidator = async (source: any, value: any, callback: any) => {
      const filed = source.field as "periodNum";
      console.log(`validate(uint) : ${source.field}`);
      console.log(`source : ${source}`);
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
        state.searchCriteria[filed] = 1;
        return;
      }

      state.searchCriteria[filed] = Math.round(inputNum);
      callback();
    };
    const searchCriteriaFormRef = ref();
    const searchCriteriaFormRules = ref({
      periodNum: [{ validator: periodNumSearchValidator, trigger: "blur" }],
    });
    //#endregion

    //#region Hook functions
    onMounted(async () => {
      //更新使用者設置偏好
      await updateUserPreference();

      //取得成品料號改善清單
      await searchTopPNInfo();
    });
    //#endregion

    //#region UI Events
    /** 按下清除按鈕 */
    const onClearSearchTopPNInfoClicked = () => {
      state.searchCriteria.periodNum = undefined;
      state.searchCriteria.topPNKeyword = undefined;

      //清除table排序
      TopPNInfoTableRef.value?.clearSort();
    };

    /** 按下搜尋按鈕 */
    const onSearchTopPNInfoClicked = async () => {
      try {
        /* 檢查欄位 */
        await searchCriteriaFormRef.value.validate();

        //清除table排序
        TopPNInfoTableRef.value?.clearSort();

        /* 取得成品料號改善清單 */
        await searchTopPNInfo();
      } catch (error) {
        console.error(error);
      }
    };

    /** 按下並放開Enter鍵 */
    const onInputFieldEnterKeyUp = async () => {
      console.log("onInputFieldEnterKeyUp");

      /* 取得成品料號改善清單 */
      await searchTopPNInfo();
    };

    /** 改變排序 */
    const onSortChanged = (sort: any) => {
      console.log(`onSortChanged: ${sort.column}, ${sort.prop}, ${sort.order}`);
      state.searchCriteria.sortColName = sort.prop;
      state.searchCriteria.sortOrder = sort.order;
      searchTopPNInfo();
      console.log("onCurrentPageChanged > scroll to 0,0");
      window.scrollTo(0, 0);
    };

    /** 分頁輸入 */
    const onPagesizeChanged = async () => {
      /* 取得成品料號改善清單 */
      await searchTopPNInfo();
      window.scrollTo(0, 0);
    };

    const onCurrentPageChanged = async () => {
      /* 取得成品料號改善清單 */
      await searchTopPNInfo();
      window.scrollTo(0, 0);
    };

    /** 關閉欄位顯示設定Panel時 */
    const onColDisplayCtrlPanelHidded = async () => {
      await API.patchTableDisplayPreference(tableType, { displayMap: state.colDisplayCtrl });
    };

    /** 按下編輯按鈕 */
    const onEditTopPNInfoClicked = async (target: IResTopPNInfoTableViewModelDTO) => {
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

    /**  編輯 送出 */
    const onSaveModifyResultClicked = async () => {
      try {
        await modifyTopPNInfoFormRef.value?.validate();
      } catch (error) {
        console.log(error);
        return;
      }

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

        const newTopPNInfo: IReqTopPNInfoDTO = {
          topPN: state.selectedTopPNInfo.topPN,
          periodNum: state.selectedTopPNInfo.periodNum,
          areaBeforeImp: state.selectedTopPNInfo.areaBeforeImp,
          procLTBeforeImp: state.selectedTopPNInfo.procLTBeforeImp,
          venderLTBeforeImp: state.selectedTopPNInfo.venderLTBeforeImp,
          pphBeforeImp: state.selectedTopPNInfo.pphBeforeImp,
          areaAfterImp: state.selectedTopPNInfo.areaAfterImp,
          venderLTAfterImp: state.selectedTopPNInfo.venderLTAfterImp,
          procLTAfterImp: state.selectedTopPNInfo.procLTAfterImp,
          pphAfterImp: state.selectedTopPNInfo.pphAfterImp,
        };
        const patchTopPNInfoResponse = await API.patchTopPNInfo(state.selectedTopPNInfo.id, newTopPNInfo);

        if (patchTopPNInfoResponse.data.result !== APIResultCode.success) {
          throw new Error(patchTopPNInfoResponse.data.msg);
        }

        ElMessage({
          showClose: true,
          message: "新增成功",
          type: "success",
        });

        state.isShowModifyDialog = false;

        // 刷新
        state.isTableLoading = true;
        await searchTopPNInfo();
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
    };

    /** 按下瀏覽按鈕 */
    const onViewTopPNInfoClicked = async (target: IResTopPNInfoDetailDTO) => {
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
    const GetSelectedItemDetailAsync = async (topPNInfoId: number) => {
      try {
        const TopPNInfoDtlResponse = await API.getTopPNInfoDetailById(topPNInfoId);
        if (TopPNInfoDtlResponse.data.result !== APIResultCode.success) {
          throw new Error(TopPNInfoDtlResponse.data.msg);
        }

        const TopPNInfoDtl = TopPNInfoDtlResponse.data.content;

        state.selectedTopPNInfo.id = TopPNInfoDtl.id;
        state.selectedTopPNInfo.createTime = TopPNInfoDtl.createTime;
        state.selectedTopPNInfo.modifiedTime = TopPNInfoDtl.modifiedTime;
        state.selectedTopPNInfo.topPN = TopPNInfoDtl.topPN;
        state.selectedTopPNInfo.periodId = TopPNInfoDtl.periodId;
        state.selectedTopPNInfo.periodNum = TopPNInfoDtl.periodNum;
        state.selectedTopPNInfo.procLTAfterImp = TopPNInfoDtl.procLTAfterImp;
        state.selectedTopPNInfo.procLTBeforeImp = TopPNInfoDtl.procLTBeforeImp;
        state.selectedTopPNInfo.venderLTAfterImp = TopPNInfoDtl.venderLTAfterImp;
        state.selectedTopPNInfo.venderLTBeforeImp = TopPNInfoDtl.venderLTBeforeImp;
        state.selectedTopPNInfo.pphAfterImp = TopPNInfoDtl.pphAfterImp;
        state.selectedTopPNInfo.pphBeforeImp = TopPNInfoDtl.pphBeforeImp;
        state.selectedTopPNInfo.areaAfterImp = TopPNInfoDtl.areaAfterImp;
        state.selectedTopPNInfo.areaBeforeImp = TopPNInfoDtl.areaBeforeImp;
      } catch (error) {
        console.log(error);
      }
    };

    /** 呼叫API, 取得所有成品料號改善清單 */
    async function searchTopPNInfo() {
      try {
        /* Table Loading Flag 開啟 */
        state.isTableLoading = true;

        const getResult = await API.searchTopPNInfo(state.searchCriteria);
        if (getResult.data.result !== APIResultCode.success) {
          throw new Error(getResult.data.msg);
        }

        //取得成品料號改善清單
        state.displayTopPNInfoList = getResult.data.content.pageItems;

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
      userInfoStore,
      paginatorSetup,
      t,
      modifyTopPNInfoFormRef,
      modifyTopPNInfoFormRules,
      searchCriteriaFormRef,
      searchCriteriaFormRules,
      numeral,

      // function
      onClearSearchTopPNInfoClicked,
      onSearchTopPNInfoClicked,
      onPagesizeChanged,
      onCurrentPageChanged,
      onSortChanged,
      onInputFieldEnterKeyUp,
      onColDisplayCtrlPanelHidded,
      onEditTopPNInfoClicked,
      onSaveModifyResultClicked,
      onViewTopPNInfoClicked,
    };
  },
});
</script>
<style scoped>
.number_input_box {
  min-width: 60px;
  max-width: 120px;
}
</style>
