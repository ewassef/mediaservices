using System.Web;
using System.Web.Mvc;

namespace Media.FusedConnection
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new LoggingHandleErrorAttribute());
        }
    }

    public class LoggingHandleErrorAttribute : HandleErrorAttribute
    {
        #region Overrides of HandleErrorAttribute

        /// <summary>
        /// Called when an exception occurs.
        /// </summary>
        /// <param name="filterContext">The action-filter context.</param><exception cref="T:System.ArgumentNullException">The <paramref name="filterContext"/> parameter is null.</exception>
        public override void OnException(ExceptionContext filterContext)
        {
            
            System.Diagnostics.Trace.WriteLine(filterContext.Exception.ToString());
            base.OnException(filterContext);
        }

        #endregion
    }
}