using Assets;

using UnityEngine;

public class StandaloneBrowser : IdentityBrowser
{
    protected override void Launch(string url)
    {
        Application.OpenURL(url);
    }
}
