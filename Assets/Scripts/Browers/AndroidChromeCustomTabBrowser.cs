namespace Assets
{
    public class AndroidChromeCustomTabBrowser : IdentityBrowser
    {
        protected override void Launch(string url)
        {
            AndroidChromeCustomTab.LaunchUrl(url);
        }
    }
}
