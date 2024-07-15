<template>
  <eiv>Template</eiv>
</template>

<script lang="ts">
import { defineComponent, reactive, toRefs } from "vue";

export default defineComponent({
  name: "view-template",
  setup() {
    const state = reactive({
      
    });

    //#region 建立表單ref與Validator
    //#endregion


    //#region Hook functions
    //#endregion


    //#region UI Events
    //#endregion


    //#region Private Functions
    //#endregion

    return {
      ...toRefs(state),
    };
  },
});
</script>

<style scoped>
</style>
