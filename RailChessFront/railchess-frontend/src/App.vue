<script setup lang="ts">
import { onMounted, useTemplateRef } from 'vue';
import Pop from './components/Pop.vue';
import Topbar from './components/Topbar.vue';
import { useProvidesSetup } from './provides';
import { preventAllGestures } from './utils/preventAllGestures';

const pop = useTemplateRef('pop')
const { displayTopbar } = useProvidesSetup(pop);
onMounted(()=>preventAllGestures())
</script>

<template>
  <Pop ref="pop"></Pop>
  <Topbar v-if="displayTopbar"></Topbar>
  <div class="mainOuter">
    <div class="main">
      <RouterView></RouterView>
    </div>
  </div>
</template>

<style scoped>
.mainOuter{
  width: 100vw;
  position: absolute; /*只能absolute不能fixed*/
  top: var(--top-bar-height);
  height: calc(100vh - var(--top-bar-height));
  transition: 0s;
  overflow: auto;
  box-sizing: border-box;
}

.main{
  width: calc(100vw - 80px);
  margin: auto;
  margin-top: 0px;
  box-sizing: border-box;
  max-width: 1400px;
  transition: 0s;

  padding-top: 20px;
}
@media screen and (max-width: 600px) {
  .main{
    width: calc(100vw - 20px);
  }
}
</style>