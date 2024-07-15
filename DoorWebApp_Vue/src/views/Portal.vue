<template>
  <div>Protal</div>
</template>
<script lang="ts">
import { defineComponent, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useUserInfoStore } from "@/stores/UserInfoStore";
import API from "@/apis/TPSAPI";
import { APIResultCode } from "@/models/enums/APIResultCode";



export default defineComponent({
  name:"protal-view",
  setup() {
    const router = useRouter();
    const userInfoStore = useUserInfoStore();


    async function GetSomeData() {
      console.log("GetSomeData");
      
    }
  },
});
</script>
