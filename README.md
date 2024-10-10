[简体中文](./README.ZH.md)
# Unity3D Authentication Client for Secure Login based on the OIDC standard (using built-in system browser)

Mobile applications that need OpenID Connect / OAuth 2 authentication would normally use a library like Google's [AppAuth](https://github.com/openid/AppAuth-Android), which is available for Android and iOS, but unfortunatley *not* for Unity Android/iOS apps.

Luckily, there's a C# library that does work: [IdentityModel.OidcClient2](https://github.com/IdentityModel/IdentityModel.OidcClient2).  This is not just a drop-in package, however - there are quite a few steps to configure your Unity project to use it successfully.  
But it's not too onerous, and the end result is your app will use SFSafariViewController on iOS and Chrome Custom Tabs on Android, and be able to work with any OAuth 2 / OpenID Connect server.

This repository contains an example Unity 2022 Android/iOS app that demonstrates how this can be achieved.  It uses a demo instance of identityserver ([demo.duendesoftware.com](https://demo.duendesoftware.com/)). 

![DEMO](./DEMO.gif)

You can login with `alice/alice` or `bob/bob`

## Unity configuration notes:

* Ensure your Unity project's .NET version is set to 4.x in player settings.
* Add link.xml and mcs.rsp files to your Assets folder.

## Unity Scene configuration

* It's important to note that the iOS and Android specific browser handling uses Unity's UnitySendMessage() function to notify the C# code of auth replies:

UnitySendMessage("SignInCanvas", "OnAuthReply", queryString);

So it's expected that your sign-in scene has a GameObject called SignInCanvas that has a script attached with  function OnAuthReply, as demonstrated in the example scene in this repo.

## iOS support

* Derive an objective-c class from UnityAppController to handle auth redirects (see OAuthUnityAppController.mm).
* Include a class for interacting with SFSafariViewController in Assets/Plugins/iOS (see SafariView.mm).
* In Unity, select SafariView.mm in Project view, and in Inspector pane, under 'Rarely used services' select 'SafariServices'.

## Android support

* Import the Google Play Services Resolver for Unity package from https://github.com/googlesamples/unity-jar-resolver
* Add an Android Unity plugin project to handle auth redirects (see AndroidUnityPlugin project).
* You will need to copy classes.jar from your Unity install folder e.g. C:\Program Files\Unity\Editor\Data\PlaybackEngines\AndroidPlayer\Variations\mono\Release\Classes\classes.jar to AndroidUnityPlugin/app/libs.
* This project contains an activity that handles auth redirects and some build scripts to export the project as a JAR file.
* Create/modify Assets/Plugins/Android/AndroidManifest.xml to include the OAuthRedirectActivity, ensuring it has the redirect URL specified in the data element's schema attribute.

## Important: Unity Version Update

* Use with Unity 2022 or newer

## References

Two critical blog posts that enabled me to work out how to achieve this:

* [Open SFSafariViewController / Chrome Custom Tabs from Unity](https://qiita.com/lucifuges/items/b17d602417a9a249689f) (use Google Translate)
* [Create An Android Plugin For Unity Using Android Studio](http://www.thegamecontriver.com/2015/04/android-plugin-unity-android-studio.html)

Code samples using IdentityModel.OidcClient2 for other platforms [here](https://github.com/IdentityModel/IdentityModel.OidcClient.Samples).
