I'm building a Bus Ticket Reservation System using ASP.NET Core with MySQL. The project must follow a strict layered architecture:

- **Controllers** → Only routing and HTTP handling, no business logic
- **Services** → All business logic lives here (Service Layer)
- **Models/Domain** → Entity classes matching the database
- **DTOs** → Data Transfer Objects for input/output
- **Repositories** → Database access only (Repository Pattern)

**Database tables:** roles, users, stations, routes, route_stations, buses, departures, seats, tickets, passengers, payments, logs, password_resets

**Please create the following folder structure and files:**
```
Models/
  Role.cs
  User.cs
  Station.cs
  Route.cs
  RouteStation.cs
  Bus.cs
  Departure.cs
  Seat.cs
  Ticket.cs
  Passenger.cs
  Payment.cs
  Log.cs
  PasswordReset.cs

DTOs/
  Auth/
    LoginDto.cs
    RegisterDto.cs
    AuthResponseDto.cs
  Ticket/
    CreateTicketDto.cs
    TicketResponseDto.cs
  Search/
    SearchQueryDto.cs
    DepartureResponseDto.cs

Services/
  Interfaces/
    IAuthService.cs
    ITicketService.cs
    ISearchService.cs
    ILogService.cs
  AuthService.cs
  TicketService.cs
  SearchService.cs
  LogService.cs

Repositories/
  Interfaces/
    IUserRepository.cs
    ITicketRepository.cs
    IDepartureRepository.cs
    ISeatRepository.cs
  UserRepository.cs
  TicketRepository.cs
  DepartureRepository.cs
  SeatRepository.cs

Controllers/
  AuthController.cs
  SearchController.cs
  TicketController.cs
  AdminController.cs
  DashboardController.cs
Requirements:

Use Entity Framework Core for database access
MySQL with Pomelo.EntityFrameworkCore.MySql package
JWT authentication
All models must have proper navigation properties and data annotations
Services must use repository interfaces (dependency injection)
Controllers must only call service methods, no direct DB access
Include DbContext class (AppDbContext.cs)
Add dependency injection setup for Program.cs
Follow C# naming conventions

Start with the Models first, then DTOs, then Interfaces, then implementations.

The Models, DTOs, Services and Repositories are already created. Now create only the **Controllers** for the Bus Ticket Reservation System in ASP.NET Core.
```
Controllers/
  AuthController.cs
  SearchController.cs
  TicketController.cs
  DashboardController.cs
  AdminController.cs

  Requirements:

Controllers only handle HTTP requests and call service methods
No business logic in controllers
No direct DB access
Use dependency injection for services
AuthController: Login, Register, Logout, ForgotPassword, ResetPassword endpoints
SearchController: Search departures by origin/destination/date
TicketController: Purchase ticket, view ticket, cancel ticket
DashboardController: User's tickets and profile (authenticated users only)
AdminController: CRUD for buses, routes, departures, users (admin role only)
Add proper route attributes
Add JWT authorization attributes where needed
Return proper HTTP status codes
Do NOT create views yet

I have an ASP.NET Core Bus Ticket Reservation System. The Controllers, Services, Repositories and Models are all complete. Now convert all HTML files into Razor .cshtml views and connect them to the backend.Existing HTML files to convert:

Views/auth/login.html → Views/Auth/Giris.cshtml
Views/auth/register.html → Views/Auth/Kayit.cshtml
Views/home/index.html → Views/Sefer/Index.cshtml
Views/search/search-results.html → Views/Sefer/AramaSonuclari.cshtml
Views/search/seat-selection.html → Views/Sefer/Detay.cshtml
Views/tickets/checkout.html → Views/Bilet/SatinAl.cshtml
Views/dashboard/dashboard.html → Views/Bilet/Liste.cshtml
Views/admin/admin.html → Views/Admin/Dashboard.cshtml

Also create:

Views/Shared/_Layout.cshtml — shared navbar + footer
Views/Odeme/Odeme.cshtml — payment page
Views/Odeme/Basarisiz.cshtml — payment failed page
Views/Auth/SifremiUnuttum.cshtml — forgot password
Views/Auth/SifreSifirla.cshtml — reset password

Conversion rules:

Keep ALL existing HTML structure, CSS classes, Bootstrap 5, Lucide icons — do not change the design
Replace hardcoded href links:

login.html → @Url.Action("Giris", "Auth")
register.html → @Url.Action("Kayit", "Auth")
index.html → @Url.Action("Index", "Sefer")
search-results.html → @Url.Action("AramaSonuclari", "Sefer")
dashboard.html → @Url.Action("Liste", "Bilet")
admin.html → @Url.Action("Dashboard", "Admin")


Replace hardcoded CSS/JS paths:

/css/style.css → @Url.Content("~/css/style.css")
/js/main.js → @Url.Content("~/js/main.js")


Move navbar and footer into _Layout.cshtml, use @RenderBody() in layout
Each view starts with @{ Layout = "_Layout"; }
Convert all forms:

Add method="post" and asp-action, asp-controller attributes
Add @Html.AntiForgeryToken() to every POST form

Use asp-for on input fields matching DTO properties


Show validation errors: @Html.ValidationSummary(true)
Dynamic data from ViewBag/Model:

Login page: show @ViewBag.ErrorMessage if exists
Search results: loop @foreach(var sefer in Model)
Seat selection: render seats from @Model.Seats
Dashboard: loop user tickets @foreach(var bilet in Model)
Admin panel: loop tables with @foreach for buses, routes, users


Add auth checks at top of protected pages:

Dashboard, checkout: @if(!User.Identity.IsAuthenticated) { Response.Redirect("/Auth/Giris"); }
Admin: @if(!User.IsInRole("admin")) { Response.Redirect("/"); }


Navbar: show Login/Register if not authenticated, show username + logout if authenticated:

@if(User.Identity.IsAuthenticated) {
    <span>@User.Identity.Name</span>
    <a asp-action="Logout" asp-controller="Auth">Çıkış</a>
} else {
    <a asp-action="Giris" asp-controller="Auth">Giriş</a>
    <a asp-action="Kayit" asp-controller="Auth">Kayıt Ol</a>
}
Start with Views/Shared/_Layout.cshtml first, then Auth views, then others.

Make it secure and it should be acceptable for MVC structure