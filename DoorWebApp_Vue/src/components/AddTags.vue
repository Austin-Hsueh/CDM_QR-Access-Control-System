<template>
  <el-tag
    v-for="tag in tags"
    :key="tag"
    class="mx-1"
    closable
    :disable-transitions="false"
    @close="onBtnCloseClicked(tag)"
  >
    {{ tag }}
  </el-tag>
  <el-input
    ref="InputRef"
    v-if="inputVisible"
    v-model="inputValue"
    size="small"
    style="width: 100px"
    @keyup.enter="onInputAddTagConfirmed"
    @blur="onInputAddTagConfirmed"
  />
  <el-button
    v-else
    class="button-new-tag ml-1"
    size="small"
    style="width: 100px"
    @click="onBtnAddTagClicked"
  >
    + {{ t("tag.New") }}
  </el-button>
</template>

<script lang="ts">
import { nextTick, ref, reactive, toRef, toRefs, defineComponent, computed ,PropType } from "vue";
import { useI18n } from "vue-i18n";
import { ElInput } from "element-plus";
import { taggedTemplateExpression } from "@babel/types";

export default defineComponent({
  name: "addTag",
  emit:['update:modelValue'],
  props: {
    //modelValue: Array
    modelValue: {
      required: true,
      type: Array as PropType<Array<string>>,
    }
  },
  setup(props, context) {
    const { t } = useI18n();
    const state = reactive({
      inputValue: "",
      inputVisible: false,
    });

    const tags = computed({
      get: () => props.modelValue,
      set: val => context.emit("update:modelValue", val)
    })

    //#region Ref
    const InputRef = ref<InstanceType<typeof ElInput>>();
    //#endregion

    //#region UI Events
    /** 按下 刪除 tag 按鈕 */
    const onBtnCloseClicked = (tag: string) => {
      // var currentTags = [...tags.value];
      // currentTags.splice(currentTags.indexOf(tag), 1);
      // tags.value = currentTags
      tags.value.splice(tags.value.indexOf(tag), 1);
    };

    /** 按下 新增 tag 按鈕 */
    const onBtnAddTagClicked = async () => {
      state.inputVisible = true;
      await nextTick();
      InputRef.value!.input!.focus();
    };

    /** 確認 新增 tag 輸入格 */
    const onIptAddTagConfirmed = () => {
      console.log(`onIptAddTagConfirmed : ${state.inputValue}`);
      
      try {
        if(!state.inputValue) return;

        const duplicateTag = props.modelValue.some(x=> x === state.inputValue);
        if(duplicateTag) {
          return;
        }

        tags.value.push(state.inputValue);

      } catch(err) {
        console.log(err);
      } finally {
        state.inputVisible = false;
        state.inputValue = "";
      }
    };
    //#endregion

    return {
      ...toRefs(state),
      t,
      tags,

      /* Ref */
      InputRef,

      /* UI Events */
      onBtnCloseClicked,
      onBtnAddTagClicked,
      onInputAddTagConfirmed: onIptAddTagConfirmed,
    };
  },
});
</script>
