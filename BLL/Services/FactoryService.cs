using BLL.Interfaces;
using DAL.Repositories;

namespace BLL.Services
{
    public static class FactoryService
    {
        public static IUserService CreateUserService()
        {
            var repo = new UserRepository();
            return new UserService(repo);
        }
        public static ITicketService CreateTicketService()
        {
            return new TicketService();
        }
        public static IReportService CreateReportService()
        {
            return new ReportService();
        }
        public static IRouteManagerService CreateRouteManagerService()
        {
            return new RouteManagerService();
        }
    }
}