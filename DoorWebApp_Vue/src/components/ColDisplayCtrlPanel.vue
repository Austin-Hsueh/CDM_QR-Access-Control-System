<template lang="ts">
  <div>
    
  </div>
</template>
<script  lang="ts">
import { defineComponent, PropType, ref, reactive, toRefs, onMounted, onActivated, nextTick } from "vue";

export default defineComponent({
  props:{
    displayMap : {
      type: Array as PropType<Array<Boolean>>
    }
  },
  emits:[
    "change"
  ],
  setup() {
    
  }
})
</script>
<style scoped>
  
</style>