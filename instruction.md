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