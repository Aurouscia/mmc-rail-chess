<script setup lang="ts">

</script>

<template>
<div class="topbar">
  <div class="left">
    <div class="topbar-item">
      <div>
        <RouterLink to="/">主页</RouterLink>
      </div>
    </div>
    <div class="topbar-item">
      <div>
        <RouterLink to="/games">对局</RouterLink>
      </div>
    </div>
    <div class="topbar-item">
      <div>
        <RouterLink to="/maps">棋盘</RouterLink>
      </div>
    </div>
    <div class="topbar-item">
      <div>
        <RouterLink to="/ranking">战绩</RouterLink>
      </div>
    </div>
    <div class="topbar-item">
      <div>
        <RouterLink to="/guide">规则</RouterLink>
      </div>
    </div>
  </div>
  <div class="right">
    <div class="topbar-item">
      <div>
        <RouterLink to="/Login">登录/个人</RouterLink>
      </div>
    </div>
  </div>
</div>
</template>

<style scoped>
.topbar{
  position: fixed;
  top:0px;
  left:0px;
  height: var(--top-bar-height);
  width: calc(100vw - 20px);
  padding-right: 20px;
  overflow: visible;
  display: flex;
  justify-content: space-between;
  margin-right: auto;
  white-space: nowrap;
  background-color: white;
  box-shadow: 0px 2px 5px #aaa;
  z-index: 1000;
}
.left{
  display: flex;
  font-size: 20px;
}
.right{
  display: flex;
  font-size:16px
}
.topbar-item{
  padding: 0px 10px 0px 10px;
  position: relative;
}
.topbar-item>div{
  display: flex;
  height: 50px;
  justify-content: center;
  align-items: center;
}

a{
  color:unset;
  text-decoration: unset;
}
a:hover{
  text-decoration: underline;
}
</style>