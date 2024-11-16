using Sentry.Protocol;
namespace Report.Server
{
    public static class Location
    {
        public static string RootPath = "";
        public static string ReportPath(string file)
        {
            return RootPath + $@"/reports/{file}";
        }
    }
}