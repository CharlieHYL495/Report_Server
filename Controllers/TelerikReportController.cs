using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Cors;
using Telerik.Reporting.Services;
using Telerik.Reporting.Services.AspNetCore;

namespace Report.Server.Controllers
{
    [ApiController]
    [Route("api/v1/telerik")]
    [EnableCors]
    public class TelerikReportController : ReportsControllerBase
    {
        public TelerikReportController(IReportServiceConfiguration reportServiceConfiguration)
            : base(reportServiceConfiguration)
        { }
        protected override HttpStatusCode SendMailMessage(MailMessage mailMessage)
        {
           throw new System.NotImplementedException();
        }
    }
}
