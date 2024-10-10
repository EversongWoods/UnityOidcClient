[简体中文](./README.ZH.md)
# Unity3D Authentication Client for Secure Login based on the OIDC standard

This is a secure login application that uses OpenID Connect / OAuth2 authentication, developed based on [IdentityModel.OidcClient2](https://github.com/IdentityModel/IdentityModel.OidcClient2). It enables secure login through the system's built-in browser to any third-party or self-hosted IdentityServer that follows the OpenID Connect / OAuth2 standard for single sign-on (SSO). The application has three built-in browsers for different scenarios:

* **Android** , it uses **Chrome Custom Tabs**
* **iOS** , it uses **SFSafariViewController**
* **UnityEditor** , for development mode, it uses the default browser of Windows / MacOS

This repository contains a sample Unity2022 Android/iOS/UnityEditor application that demonstrates how to implement this. It uses a demo instance of IdentityServer ([demo.duendesoftware.com](https://demo.duendesoftware.com/)).

![DEMO](./DEMO.gif)

You can login with `alice/alice` or `bob/bob`

## Unity configuration notes:

* Ensure your Unity project's .NET version is set to 4.x in player settings.
* Add link.xml and mcs.rsp files to your Assets folder.

## Unity Scene configuration

* It's important to note that the iOS and Android specific browser handling uses Unity's UnitySendMessage() function to notify the C# code of auth replies:

UnitySendMessage("SignInCanvas", "OnAuthReply", queryString);

So it's expected that your sign-in scene has a GameObject called SignInCanvas that has a script attached with  function OnAuthReply, as demonstrated in the example scene in this repo.

## Important: Unity Version Update

* Use with Unity 2022 or newer

## References

Two critical blog posts that enabled me to work out how to achieve this:

* [Open SFSafariViewController / Chrome Custom Tabs from Unity](https://qiita.com/lucifuges/items/b17d602417a9a249689f) (use Google Translate)
* [Create An Android Plugin For Unity Using Android Studio](http://www.thegamecontriver.com/2015/04/android-plugin-unity-android-studio.html)

Code samples using IdentityModel.OidcClient2 for other platforms [here](https://github.com/IdentityModel/IdentityModel.OidcClient.Samples).
