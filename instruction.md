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

Start with the Models first, then DTOs

Create only the Model classes and DTOs for a Bus Ticket Reservation System in ASP.NET Core with Entity Framework Core + MySQL.
Models folder (Models/):
Role, User, Station, Route, RouteStation, Bus, Departure, Seat, Ticket, Passenger, Payment, Log, PasswordReset
DTOs folder (DTOs/):

Auth/LoginDto.cs, RegisterDto.cs, AuthResponseDto.cs
Ticket/CreateTicketDto.cs, TicketResponseDto.cs
Search/SearchQueryDto.cs, DepartureResponseDto.cs

Also create AppDbContext.cs in the root.
Requirements:

Proper data annotations
Navigation properties between entities
Pomelo.EntityFrameworkCore.MySql
No services, no controllers, no repositories — just models and DTOs.

