<template>
  <div class="loginBG">
    <el-card class="box-card" shadow="hover" :body-style="singinInputCardStyle">
      <div class="d-flex flex-column justify-content-start align-items-center mb-3">
        <img class="box-logo mt-3 mb-3" src="../assets/logo.png" alt="Logo" />
        <el-form @submit.prevent :model="loginData" :rules="loginFormRules" ref="loginForm">
          <el-form-item prop="username">
            <el-input
              class="w-100"
              v-model="loginData.username"
              :placeholder="t('username')"
              :prefix-icon="User"
              @keydown="onKeydown()"
              @keyup.enter="onSigninClicked()"
              tabindex="1"
            ></el-input>
          </el-form-item>
          <el-form-item prop="password">
            <el-input
              class="w-100"
              v-model="loginData.password"
              :placeholder="t('password')"
              show-password
              :prefix-icon="Lock"
              @keydown="onKeydown()"
              @keyup.enter="onSigninClicked()"
              tabindex="2"
            ></el-input>
          </el-form-item>
          <el-form-item>
            <el-select class="w-100" v-model="loginData.locale" @change="onLangSelectChanged" :placeholder="t('select_language')">
              <el-option value="en_us" label="English" />
              <el-option value="zh_tw" label="繁體中文" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-checkbox v-model="loginData.isKeepLogin">{{ t("Remember_me") }}</el-checkbox>
          </el-form-item>

          <el-button class="w-100" type="primary" plain @click="onSigninClicked()" :loading="isSigninBtnLoading">
            {{ isSigninBtnLoading ? t("processing") : t("login") }}
          </el-button>
          <el-button class="w-100" type="primary" text="忘記密碼" plain @click="onPasswordClicked()" :loading="isSigninBtnLoading" style="margin-left: 0; margin-top: 5px;">
            {{ t('PasswordForget') }}
          </el-button>
        </el-form>

        <div class="error-msg" v-if="isShowErrorMsg">{{ t('wrong_password') }}</div>
      </div>
    </el-card>
  </div>
  <!-- 忘記密碼彈窗 -->
  <el-dialog class="dialog" top="3vh" v-model="isShowPasswordDialog" :title="t('PasswordForget')">
    <el-form label-width="100px"  ref="passwordForm" :rules="passwordRules" :model="passwordFormData">
      <el-form-item :label="t('username')" prop="username"  >
        <el-input style="width:90%" v-model="passwordFormData.username"/>
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="isShowPasswordDialog = false">{{ t("Cancel") }}</el-button>
        <el-button type="primary"  @click="submitNewPasswordForm()">{{ t("Confirm") }}</el-button>
      </span>
    </template>
  </el-dialog>
  <!-- /編輯彈窗 -->
</template>

<script lang="ts">
import { defineComponent, onMounted, reactive, Ref, ref, toRefs } from "vue";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";
import IReqLoginDTO from "@/models/dto/IReqLoginDTO";
import { User, Lock } from "@element-plus/icons-vue";
import { delay } from "@/plugins/utility";
import i18n from "@/locale";
import {LocaleType} from "@/models/enums/LocaleType";
import { FormInstance, FormRules, ElNotification, NotificationParams  } from 'element-plus';

export default defineComponent({
  name: "login-view",
  setup() {
    const t = i18n.global.t;
    const router = useRouter();
    const userInfoStore = useUserInfoStore();

    const state = reactive({
      isSigninBtnLoading: false,
      isShowErrorMsg: false,
      isShowPasswordDialog: false,
      errorMsg: "",
      singinInputCardStyle: {
        padding: "10px 30px",
        width: "350px",
        display: "flex",
        "flex-direction": "column",
        background: "#FFFFFF",
      },
      loginData: {} as IReqLoginDTO,
      //selectedLang: localStorage.getItem("locale") as "en_us" | "zh_tw" | "zh_cn",
      //isKeepSignin: false,
      passwordFormData:{username:''}
    });

    //#region 建立表單ref與Validator
    const loginForm = ref();
    const loginFormRules = ref({
      username: [
        { required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" },
        { pattern: /^[a-zA-Z0-9]+$/, message: () => t("validation_msg.only_letters_and_numbers") },
      ],
      password: [{ required: true, message: () => t("validation_msg.password_is_required"), trigger: "blur" }],
    });

    const passwordForm = ref();
    const passwordRules = ref({
      username: [
        { required: true, message: () => t("validation_msg.username_is_required"), trigger: "blur" },
        { pattern: /^[a-zA-Z0-9]+$/, message: () => t("validation_msg.only_letters_and_numbers") },
      ],
    });
    //#endregion

    //#region Hook functions
    onMounted(() => {
      state.loginData.locale = userInfoStore.getLocale();
    });
    //#endregion

    //#region UI Events
    /** 按下登入按鈕 */
    const onSigninClicked = async () => {
      console.log(`NODE_ENV:${process.env.NODE_ENV}, locale:${state.loginData.locale}`);

      //記錄使用者語系偏好
      userInfoStore.setLocale(state.loginData.locale);

      loginForm.value.validate(async (valid: boolean) => {
        if (!valid) return;

        state.isSigninBtnLoading = true;

        try {
          const signinResponse = await API.login(state.loginData);
          if (process.env.VUE_APP_RUN_ENV === "DEV") await delay(1000);

          if (signinResponse.data.result !== APIResultCode.success) {
            console.log("login fail!");
            state.isShowErrorMsg = true;
            //state.errorMsg = signinResponse.data.msg;
            return;
          }

          //signin success!
          const signinResult = signinResponse.data.content;
          console.log(signinResponse.data);
          console.log(`token:${signinResponse.data.content}`);

          userInfoStore.setToken(signinResult.token, state.loginData.isKeepLogin);
          userInfoStore.userId = signinResult.userId;
          userInfoStore.username = signinResult.username;
          userInfoStore.displayName = signinResult.displayName;

          userInfoStore.setQRcode(signinResult.qrcode);

          //轉至主畫面
          router.replace({ path: "/" });
        } catch (error) {
          console.log(error);
        } finally {
          state.isSigninBtnLoading = false;
        }
      });
    };

    /** 選擇語言 */
    const onLangSelectChanged = async () => {
      userInfoStore.setLocale(state.loginData.locale);
    };

    /** 輸入帳號及密碼 */
    const onKeydown = () => {
      state.isShowErrorMsg = false;
      state.errorMsg = "";
    };

    /** 按下忘記密碼按鈕 */
    const onPasswordClicked = () => {
      state.isShowPasswordDialog = true;
    };
    
    /** 提交使用者帳號 */
    const submitNewPasswordForm = async() => {
      console.log(state.passwordFormData.username)
      let notifyParam: NotificationParams = {};
      const updateResponse = await API.getNewPassword(state.passwordFormData);
      if (updateResponse.data.result != 1) {
        notifyParam = {
          title: "失敗",
          type: "error",
          message: `帳號：${state.passwordFormData.username} 取得密碼失敗`,
          duration: 2000,
        };
      } else {
        notifyParam = {
          title: "成功",
          type: "success",
          message: `帳號：${state.passwordFormData.username} 密碼已更新，請至信箱收信。`,
          duration: 2000,
        };
      }

      ElNotification(notifyParam);
      state.isShowPasswordDialog = false;
    }
    //#endregion

    //#region Private Functions
    //#endregion

    return {
      ...toRefs(state),
      t,
      loginForm,
      loginFormRules,
      passwordForm,
      passwordRules,
      User,
      Lock,
      LocaleType,
      onSigninClicked,
      onLangSelectChanged,
      onKeydown,
      onPasswordClicked,
      submitNewPasswordForm
    };
  },
});
</script>

<style scoped>
.mf-box-logo {
  width: 30%;
  margin: auto;
}

.box-logo {
  /* height: 80px;
  margin: 20px 20px 0px 20px; */
  width: 85% !important;
}
.box-title {
  font-size: 1.5rem;
  font-weight: bold;
  color: #606266;
}

.box-card {
  border-width: 0px;
}

.loginBG {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;

  background-image: url("~@/assets/BG.jpg");
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center;
  /* background-size: 100px auto; */
}

.error-msg {
  color: red;
  padding: 0px;
  margin: 10px;
  font-size: 0.8rem;
}
.submit-form-item {
  margin: 10px;
}
</style>
