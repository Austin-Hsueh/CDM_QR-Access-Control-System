import IPaginatorSetup from "@/models/IPaginatorSetup";
import { defineStore } from "pinia";

export const usePaginatorSetup = defineStore("paginator", {
  state: (): IPaginatorSetup => {
    return {
      pagerCount: 7,
      layout: "total, sizes, prev, pager, next, jumper",
      small: false,
    };
  },
  actions: {
    update(width: number) {
      //X-Small
      if (width < 576) {
        console.log("paginator : xs");
        this.pagerCount = 4;
        this.layout = "total, sizes, jumper";
        this.small = true;
        return;
      }

      //Small
      if (576 <= width && width < 768) {
        console.log("paginator : sm");
        this.pagerCount = 4;
        this.layout = "total, sizes, jumper";
        this.small = true;
        return;
      }

      //Medium
      if (768 <= width && width < 992) {
        console.log("paginator : md");
        this.pagerCount = 4;
        this.layout = "total, sizes,  pager , jumper";
        this.small = true;
        return;
      }

      //Large
      if (992 <= width && width < 1200) {
        console.log("paginator : lg");
        this.pagerCount = 7;
        this.layout = "total, sizes, prev, pager, next, jumper";
        this.small = false;
        return;
      }

      //Extra Large
      if (width >= 1200) {
        console.log("paginator : xl");
        this.pagerCount = 7;
        this.layout = "total, sizes, prev, pager, next, jumper";
        this.small = false;
        return;
      }
    },
  },
});
