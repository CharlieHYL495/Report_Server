using Sentry.Protocol;
namespace Report.Server
{
    public static class Location
    {
        public static string RootPath = @"C:\";
        public static string ReportPath(string file)
        {
            return RootPath + $@"/Reports/{file}";
        }
    }
}