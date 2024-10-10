[English](./README.md)
# Unity3D 基于 OIDC 标准的用于安全登录的客户端（使用系统内置浏览器）

需要OpenID Connect / OAuth 2认证的移动应用通常会使用像Google的[AppAuth](https://github.com/openid/AppAuth-Android)这样的库，它可用于Android和iOS，但不幸的是*不适用*于Unity Android/iOS应用。

幸运的是，有一个可以使用的C#库：[IdentityModel.OidcClient2](https://github.com/IdentityModel/IdentityModel.OidcClient2)。然而，这不仅仅是一个可以直接使用的包 - 要在Unity项目中成功使用它需要相当多的配置步骤。  
但这并不太繁琐，最终结果是您的应用将在iOS上使用SFSafariViewController，在Android上使用Chrome Custom Tabs，并能够与任何OAuth 2 / OpenID Connect服务器配合使用。

这个仓库包含了一个Unity 2022 Android/iOS应用的示例，演示了如何实现这一目标。它使用了identityserver的演示实例([demo.duendesoftware.com](https://demo.duendesoftware.com/)) - 您可以在[这里](https://github.com/IdentityServer/IdentityServer4.Demo)查看源代码。示例运行的视频：[Android](https://codenature.info/pub/unityauth/android-identitymodel-unity-sample.mp4)和[iOS](https://codenature.info/pub/unityauth/iphone-identitymodel-unity-sample.mp4)。

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

## iOS支持

* 从UnityAppController派生一个objective-c类来处理认证重定向（参见OAuthUnityAppController.mm）。
* 在Assets/Plugins/iOS中包含一个用于与SFSafariViewController交互的类（参见SafariView.mm）。
* 在Unity中，在Project视图中选择SafariView.mm，在Inspector面板中，在'Rarely used services'下选择'SafariServices'。

## Android支持

* 从https://github.com/googlesamples/unity-jar-resolver 导入Unity的Google Play Services Resolver包
* 添加一个Android Unity插件项目来处理认证重定向（参见AndroidUnityPlugin项目）。
* 您需要将classes.jar从Unity安装文件夹（例如C:\Program Files\Unity\Editor\Data\PlaybackEngines\AndroidPlayer\Variations\mono\Release\Classes\classes.jar）复制到AndroidUnityPlugin/app/libs。
* 该项目包含一个处理认证重定向的活动和一些用于将项目导出为JAR文件的构建脚本。
* 创建/修改Assets/Plugins/Android/AndroidManifest.xml以包含OAuthRedirectActivity，确保在data元素的schema属性中指定了重定向URL。

## 重要提示：Unity版本更新

* 使用Unity 2022或更新版本

## 参考

两篇关键的博客文章帮助我弄清楚如何实现这一目标：

* [从Unity打开SFSafariViewController / Chrome Custom Tabs](https://qiita.com/lucifuges/items/b17d602417a9a249689f)（使用Google翻译）
* [使用Android Studio为Unity创建Android插件](http://www.thegamecontriver.com/2015/04/android-plugin-unity-android-studio.html)

使用IdentityModel.OidcClient2的其他平台代码示例可以在[这里](https://github.com/IdentityModel/IdentityModel.OidcClient.Samples)找到。
