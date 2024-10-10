[English](./README.md)
# Unity3D 基于 OIDC 标准的用于安全登录的客户端

这是一个可以使用 OpenID Connect / OAuth2 认证的安全登录程序, 基于[IdentityModel.OidcClient2](https://github.com/IdentityModel/IdentityModel.OidcClient2)进行开发。
它通过系统内置的浏览器进行安全登录任何基于 OpenId Connect / OAuth2 标准的第三方或者自建 IdentityServer 进行单点登录(SSO)，并且该程序根据不同的场景内置了三种浏览器。

* 在 **Android** 上使用 **Chrome Custom Tabs**
* 在 **iOS** 上使用 **SFSafariViewController**
* 在 **UnityEditor** 下作为开发开发模式，使用 Windows/Mac 的默认浏览器

这个仓库包含了一个 Unity2022 Android/iOS/UnityEditor 应用的示例，演示了如何实现这一目标。它使用了identityserver的演示实例([demo.duendesoftware.com](https://demo.duendesoftware.com/))。

![DEMO](./DEMO.gif)

您可以使用`alice/alice`或`bob/bob`登录

## Unity配置注意事项：

* 确保您的Unity项目在播放器设置中将.NET版本设置为4.x。
* 将link.xml和mcs.rsp文件添加到您的Assets文件夹。

## Unity场景配置

* 需要注意的是，iOS和Android特定的浏览器处理使用Unity的UnitySendMessage()函数来通知C#代码认证回复：

```csharp
UnitySendMessage("SignInCanvas", "OnAuthReply", queryString);
```

因此，预期您的登录场景有一个名为SignInCanvas的GameObject，其中附加了一个带有OnAuthReply函数的脚本，如本仓库中的示例场景所演示的那样。


## 重要提示：Unity版本更新

* 使用Unity 2022或更新版本

## 参考

两篇关键的博客文章帮助我弄清楚如何实现这一目标：

* [从Unity打开SFSafariViewController / Chrome Custom Tabs](https://qiita.com/lucifuges/items/b17d602417a9a249689f)（使用Google翻译）
* [使用Android Studio为Unity创建Android插件](http://www.thegamecontriver.com/2015/04/android-plugin-unity-android-studio.html)

使用IdentityModel.OidcClient2的其他平台代码示例可以在[这里](https://github.com/IdentityModel/IdentityModel.OidcClient.Samples)找到。
