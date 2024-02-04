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
1. [Visual Studio](https://visualstudio.microsoft.com/zh-hans/) 尽可能新版+web应用开发负载（如果下载不动就手动设置dns为8.8.8.8）
2. [node客户端](https://nodejs.org/en) 尽可能新版，并确认命令行中有npm命令可用
3. （可选）[git客户端](https://git-scm.com/downloads) 用来下载代码和提交更新
4. （可选）visual studio code（和第一条是两个东西）用来编辑前端代码
5. 一台windows系统的服务器（linux的也行，但是我自己没搞过）

### 步骤
1. 下载代码文件  
    1-1.如果使用git，在命令行中输入`git clone 【本仓库链接】`
2. 进入前端文件夹(/RailChessFront/railchess-frontend)，在命令行中输入`npm install`和`npm run build`
3. 双击项目根目录的sln文件，进入vs
4. 点击顶部栏进入`工具-nuget包管理器-包管理器控制台`，输入`update-database`
5. 点击顶部绿色启动按钮启动调试，检查是否正常
6. 停止调试，点击顶部栏`生成-发布`即可选择位置导出
7. windows服务器上安装[.net7.0 hosting bundle](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-aspnetcore-7.0.15-windows-hosting-bundle-installer), linux的另外找教程，*“部署asp.netCore应用”*
8. 把导出的程序移动到服务器上，并给予Users用户组该文件夹的控制权限，用IIS新建网站并指向该文件夹
9. 尝试启动并进入网站