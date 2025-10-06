# MmcRailChess

## 概述
本项目是mmc及其同学设计的基于地铁图的棋类游戏的线上版实现，遵循Apache-2.0开源协议。
## 架构
1. 后端基于[asp.netCore](https://dotnet.microsoft.com/zh-cn/apps/aspnet)
2. 数据库使用Sqlite+EntityFramework
3. 图像处理使用[ImageSharp](https://sixlabors.com/products/imagesharp/)
4. 前端使用[VUE框架](https://vuejs.org)+[TS语言](https://typescriptlang.org)+[VITE打包工具](https://vite.dev)
5. 双向即时通讯使用[SignalR](https://dotnet.microsoft.com/zh-cn/apps/aspnet/signalr)
6. 身份验证使用[JWT](https://jwt.io)

## 安装
### 前提条件
1. [Visual Studio](https://visualstudio.microsoft.com/zh-hans/) 尽可能新版+web应用开发负载
2. [node客户端](https://nodejs.org/en) 尽可能新版，并确认命令行中有npm命令可用
3. （可选）[git客户端](https://git-scm.com/downloads) 用来下载代码和提交更新
4. （可选）visual studio code（和第一条是两个东西）用来编辑前端代码
5. 一个域名和一台服务器

### 步骤
1. 下载代码文件  
    - 如果使用git，在命令行中输入`git clone 【本仓库链接】`
2. 进入前端文件夹(/RailChessFront/railchess-frontend)，在命令行中输入`npm ci`和`npm run build`
    - 如果`npm ci`失败，设置国内镜像源
3. 双击项目根目录的sln文件，进入vs，按appsettings.json中的注释修改配置文件（注意masterKey必须新设置）
4. 点击顶部绿色启动按钮启动调试，检查是否正常
5. 调试时，将链接改为`http://localhost:5165/Init/Mi/<masterKey>`
    - 该操作将生成/更新数据库架构
6. 停止调试，点击顶部栏`生成-发布`即可选择位置导出
7. 安装到服务器
    - windows
        - windows服务器上安装`.net9.0 hosting bundle`，在[此处](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)可以找到hosting bundle最新版
        - 把导出的程序移动到服务器上，并给予Users用户组该文件夹的控制权限，用IIS新建网站并指向该文件夹
    - linux(docker)
        - todo，有人需要的话请联系我，我会添加dockerfile到项目里
8. 尝试启动并进入网站
9. 访问`<你的域名>/Init/Mi/<masterKey>`，以生成/更新数据库架构
10. 如果有人忘记密码，访问`<你的域名>/ResetPwd/<masterKey>/<用户名>`，重置其密码

### 注意
如果使用命令行启动，确保命令行工作目录（pwd）和程序目录（exe所在目录）一致，否则可能会出现各种问题。

## 帮助
部署/使用遇到任何问题，请加qq群 302104258 联系作者

## 许可证
Apache-2.0