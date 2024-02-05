<script setup lang="ts">
import { inject, onMounted, ref,Ref } from 'vue';
import { HttpClient} from '../utils/httpClient';
import { IdentityInfo,IdentityInfoProvider } from '../utils/userInfo';
import Pop from '../components/Pop.vue';
import { Api } from '../utils/api';
import { injectUserInfo } from '../provides';
import SwitchingTabs from '../components/SwitchingTabs.vue';
import { useRouter } from 'vue-router';
import { User } from '../models/user';
import Loading from '../components/Loading.vue';
import Avatar from '../components/Avatar.vue';

const router = useRouter();
const userName = ref<string>("")
const password = ref<string>("")
var identityInfoProvider:IdentityInfoProvider;
const identityInfo = ref<IdentityInfo|undefined>()
var httpClient:HttpClient;
var api:Api;
var pop:Ref<InstanceType<typeof Pop>>
var tabs = ref<InstanceType<typeof SwitchingTabs>>();
async function Login(){
    const token = await api.identites.authen.login({
        userName:userName.value,
        password:password.value
    })
    if(token){
        httpClient.setToken(token);
        identityInfoProvider.clearCache();
        if (identityInfoProvider) {
            identityInfo.value = await identityInfoProvider.getIdentityInfo();
        }
        router.push("/");
    }
};
async function Logout() {
    httpClient.clearToken();
    identityInfoProvider.clearCache();
    pop.value.show("已经成功退出登录","success");
    if(identityInfoProvider){
        identityInfo.value = await identityInfoProvider.getIdentityInfo();
    }
}

const newUserName = ref<string>("")
const newPassword = ref<string>("")
async function Register() {
    const res = await api.identites.user.add({userName:newUserName.value,password:newPassword.value});
    if(res){
        newUserName.value = "";
        newPassword.value = "";
    }
}

const uInfo = ref<User>();
async function edit() {
    const res = await api.identites.user.edit()
    if(res){
        uInfo.value = res;
    }
}
async function editExe() {
    if(uInfo.value){
        await api.identites.user.editExe(uInfo.value);
    }
}
async function tabSwitched(idx:number){
    if(idx==2){
        await edit();
    }
}

const file = ref<HTMLInputElement>();
async function pick() {
    if(!file.value){
        return false;
    }
    file.value.showPicker();
}
async function pickDone() {
    if(!file.value){
        return false;
    }
    if(file.value.files && file.value.files.length>0){
        const resp = await api.identites.user.setAvatar(file.value.files[0]);
        if(resp){
            await edit();
        }
        file.value.value = "";
    }
}

onMounted(async()=>{
    pop = inject('pop') as Ref<InstanceType<typeof Pop>>
    httpClient = inject('http') as HttpClient;
    api = inject('api') as Api;
    identityInfoProvider = injectUserInfo();
    identityInfo.value = await identityInfoProvider.getIdentityInfo();
})
</script>

<template>
    <SwitchingTabs ref="tabs"  @switch="tabSwitched"
        :texts="identityInfo&&identityInfo.Id>0 ? ['登录','注册','个人中心'] : ['登录','注册']">
        <div>
        <div>
            <table>
                <tr>
                    <td>昵称</td>
                    <td>
                        <input v-model="userName" type="text"/>
                    </td>
                </tr>
                <tr>
                    <td>密码</td>
                    <td>
                        <input v-model="password" type="password"/>
                    </td>
                </tr>
            </table>
            <div class="login">
                <button @click="Login" class="confirm">登&nbsp;录</button>
            </div>
        </div>
        <div class="loginInfo" v-if="identityInfo">
            当前登录：
            [{{ identityInfo?.Id }}]{{ identityInfo?.Name || "游客" }}<br/>
            登录有效期：{{ identityInfo?.LeftHours }}小时<br/>
            <button @click="Logout" class="logout">退出登录</button>
        </div>
        </div>
        <div>
            <table>
                <tr>
                    <td>昵称</td>
                    <td>
                        <input v-model="newUserName" type="text"/>
                    </td>
                </tr>
                <tr>
                    <td>密码</td>
                    <td>
                        <input v-model="newPassword" type="text"/>
                    </td>
                </tr>
            </table>
            <div class="login">
                <button @click="Register" class="confirm">注&nbsp;册</button>
            </div>
        </div>
        <div>
            <div v-if="uInfo">
                <div class="avt">
                    <Avatar :file-name="uInfo.AvatarName" :name="uInfo.Name" @click="pick"></Avatar>
                    <input ref="file" @change="pickDone" type="file">
                </div>
                <div class="avtNotice">点击设置</div>
                <table>
                    <tr>
                        <td>昵称</td>
                        <td>
                            <input v-model="uInfo.Name" type="text"/>
                        </td>
                    </tr>
                    <tr>
                        <td>密码</td>
                        <td>
                            <input v-model="uInfo.Pwd" type="text"/>
                        </td>
                    </tr>
                </table>
                <div class="login">
                    <button @click="editExe" class="confirm">保&nbsp;存</button>
                </div>
                <div class="notice">建议修改用户名或密码后立即登录一次，让浏览器记录新信息</div>
            </div>
            <Loading v-else>
            </Loading>
        </div>
    </SwitchingTabs>
</template>

<style scoped>
.avtNotice{
    text-align: center;
    color:#aaa;
    font-size: 14px;
}
input[type=file]{
    display: none;
}
.avt{
    position: relative;
    height: 68px;
    width: 68px;
    margin: 20px auto 0px auto;
}
.notice{
    text-align: center;
    margin-top: 20px;
    color: #aaa
}
table{
    margin:auto;
    background-color: transparent;
    font-size: large;
    color:gray
}
td{
    background-color: transparent;
}
input{
    background-color: #eee;
}
.login{
    display: flex;
    justify-content: center;
}
button.logout{
    background-color: gray;
    color:white;
    padding: 2px;
}
.loginInfo{
    color:gray;
    font-size:small;
    text-align: center;
    position: fixed;
    margin: 0px;
    left:20px;
    bottom: 20px;
}
.register{
    text-align: center;
    color:gray;
    margin-top: 20px;
}
</style>