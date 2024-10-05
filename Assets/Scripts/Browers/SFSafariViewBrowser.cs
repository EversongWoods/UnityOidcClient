namespace Assets
{
    public class SFSafariViewBrowser : IdentityBrowser
    {
        protected override void Launch(string url)
        {
            SFSafariView.LaunchUrl(url);
        }

        public override void Dismiss()
        {
            SFSafariView.Dismiss();
        }
    }
}
