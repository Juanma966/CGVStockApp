# 🏗️ CGVStockApp - ARQUITECTURA CQRS (REVISADO)
## .NET 9 | Clean Architecture | CQRS + MediatR | Sin Repository Pattern

**Versión:** 2.0 (Corregida)  
**Cambios aplicados:**
- ✅ Eliminado Repository Pattern + UnitOfWork
- ✅ Acceso a datos directo vía IApplicationDbContext en Handlers
- ✅ AutoMapper limitado (solo complejos) + Select() para Queries
- ✅ Mantenido AccountingMovement para Dashboard
- ✅ ProductCode AANNNN sin cambios
- ✅ Custom Auth + JWT (sin ASP.NET Core Identity)
- ✅ Corregido error conceptual CQRS

---

## 📋 TABLA DE CONTENIDOS

1. [Cambios Principales](#cambios-principales)
2. [¿Por qué CQRS?](#por-qué-cqrs)
3. [Arquitectura General](#arquitectura-general)
4. [Estructura de Proyectos](#estructura-de-proyectos)
5. [Acceso a Datos: IApplicationDbContext](#acceso-a-datos-iapplicationdbcontext)
6. [Flujo CQRS Corregido](#flujo-cqrs-corregido)
7. [Patrones de Implementación](#patrones-de-implementación)
8. [Features del Proyecto](#features-del-proyecto)

---

## 🔄 CAMBIOS PRINCIPALES

### Cambio 1: Eliminación de Repository Pattern

**ANTES:**
```
Handler → UnitOfWork → ProductRepository → DbContext → BD
```

**AHORA:**
```
Handler → IApplicationDbContext → DbContext → BD
```

**Razón:** EF Core es la abstracción. Repository + UnitOfWork agrega complejidad sin beneficio real en CQRS.

### Cambio 2: AutoMapper Limitado

**ANTES:** AutoMapper para TODO (entities ↔ DTOs)

**AHORA:**
- AutoMapper: Solo para mappings complejos (Command → Entity)
- Select(): Para Queries (claridad + performance)

**Razón:** Select() es más explícito, mejor performance, menos dependencias.

### Cambio 3: AccountingMovement Mantenido

**Decisión:** ✅ MANTENER

**Razón:** Requisito original incluye "Dashboard donde verá ventas y compras en forma de tabla de libro diario"

**Entidades:**
- `Sale`: Registra venta
- `AccountingMovement`: Registro contable (entrada/salida)
- `Expense`: Gastos manuales (futuro)

### Cambio 4: ProductCode Sin Cambios

✅ Mantener formato AANNNN con generación automática

**Recomendación:** Agregar UNIQUE constraint en BD

### Cambio 5: Auth Custom + JWT

✅ Mantener sistema personalizado (sin ASP.NET Core Identity)

**Razón:** Plazos firmes (45-50 días). Identity agrega complejidad de setup sin beneficio crítico.

### Cambio 6: Corrección CQRS

**ANTES (INCORRECTO):**
```
Commands = C/R/U/D
Queries = G
```

**AHORA (CORRECTO):**
```
Commands = Create, Update, Delete + Acciones que modifican estado
Queries = Read + Búsquedas sin modificación de estado
```

---

## 🤔 ¿POR QUÉ CQRS?

### Separación de Responsabilidades

**Commands:** Operaciones que MODIFICAN estado
- CreateProductCommand
- UpdatePriceCommand
- CreateSaleCommand
- DeleteProductCommand

**Queries:** Operaciones que LEEN estado (sin modificaciones)
- GetProductsQuery
- SearchProductsQuery
- GetMinimumStockQuery
- GetDashboardQuery

### Ventajas en CGVStockApp

| Aspecto | Beneficio |
|---------|-----------|
| **Organización** | Features autosuficientes por dominio |
| **Testing** | Handlers independientes, fácil mockear DbContext |
| **Performance** | Queries optimizadas con Select() |
| **Mantenibilidad** | Cambios en una feature no afectan otras |
| **Escalabilidad** | Base para CQRS puro (BD separadas) futuro |
| **Auditoría** | Cada Command es registrable |

---

## 🏗️ ARQUITECTURA GENERAL

### Diagrama de Flujo Actualizado

```
┌─────────────────────────────────────────────────────────┐
│                   React Frontend                        │
│              (PC/Tablet/Mobile)                         │
└────────────┬────────────────────────────────────────────┘
             │ HTTP Request
             ▼
┌─────────────────────────────────────────────────────────┐
│              CGVStockApp.Api (Port 5000)                │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Controllers (Route + Auth + Error Handling)      │   │
│  │  - ProductController                            │   │
│  │  - SalesController                              │   │
│  │  - DashboardController                          │   │
│  │  - etc.                                          │   │
│  └─────────────────┬────────────────────────────────┘   │
│                    │                                    │
│  Inyección de MediatR Sender                           │
│  (enruta a Application)                                │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│          CGVStockApp.Application (CQRS LOGIC)          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ COMMANDS (Escriben)                              │   │
│  │  ├── CreateProductCommand                       │   │
│  │  ├── UpdatePriceCommand                         │   │
│  │  └── CreateSaleCommand                          │   │
│  │     ▼                                             │   │
│  │ HANDLERS (Ejecutan lógica)                       │   │
│  │  ├── CreateProductCommandHandler                │   │
│  │  │   └── Recibe IApplicationDbContext           │   │
│  │  │   └── Accede directo: context.Products       │   │
│  │  │   └── Persiste cambios                       │   │
│  │  └── CreateSaleCommandHandler                   │   │
│  │      └── Valida stock + descuenta               │   │
│  │                                                  │   │
│  │ QUERIES (Leen)                                  │   │
│  │  ├── GetProductsQuery                           │   │
│  │  ├── GetMinimumStockQuery                       │   │
│  │  └── GetDashboardQuery                          │   │
│  │     ▼                                             │   │
│  │ HANDLERS (Leen + Retornan)                      │   │
│  │  ├── GetProductsQueryHandler                    │   │
│  │  │   └── Usa Select() para proyectar DTO       │   │
│  │  │   └── Retorna List<ProductResponse>          │   │
│  │  └── GetDashboardQueryHandler                   │   │
│  │      └── Combina Sales + AccountingMovements   │   │
│  │                                                  │   │
│  │ VALIDATORS (FluentValidation)                   │   │
│  │  └── CreateProductCommandValidator              │   │
│  │  └── CreateSaleCommandValidator                 │   │
│  │                                                  │   │
│  │ MAPPERS (AutoMapper - solo complejos)          │   │
│  │  └── CreateProductCommand → Product             │   │
│  │  └── NO AutoMapper para Queries (Select())      │   │
│  │                                                  │   │
│  │ PIPELINE BEHAVIORS (Middleware CQRS)            │   │
│  │  ├── ValidationBehavior                         │   │
│  │  ├── LoggingBehavior                            │   │
│  │  └── PerformanceBehavior                        │   │
│  └──────────────────────────────────────────────────┘   │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│      CGVStockApp.Infrastructure (PERSISTENCIA)         │
│  ┌──────────────────────────────────────────────────┐   │
│  │ ApplicationDbContext                             │   │
│  │  └── DbSet<Product>, DbSet<Sale>, etc.         │   │
│  │  └── EntityTypeConfigurations                   │   │
│  │  └── SaveChangesAsync()                         │   │
│  │                                                  │   │
│  │ Services (Utilidades)                           │   │
│  │  ├── JwtTokenService                            │   │
│  │  ├── PasswordHashService                        │   │
│  │  └── PdfGenerationService                       │   │
│  │                                                  │   │
│  │ Migrations (EF Core)                            │   │
│  └──────────────────────────────────────────────────┘   │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│          PostgreSQL Database                           │
│  ├── products (con ProductCode UNIQUE)                │
│  ├── categories                                        │
│  ├── sales                                             │
│  ├── accounting_movements (libro diario)              │
│  └── ... (tablas restantes)                           │
└─────────────────────────────────────────────────────────┘
```

### Reglas de Dependencia (Clean Architecture)

```
┌─────────────────────────────────────┐
│   Domain (Entidades)                │  ← NO depende de nada
│  ├── Entities                       │
│  ├── Enums                          │
│  ├── Exceptions                     │
│  └── Common (AuditableEntity)       │
└─────────────────────────────────────┘
         ▲
         │ depende de
         │
┌─────────────────────────────────────┐
│ Application (CQRS)                  │  ← Depende de Domain
│  ├── Features/*/Commands            │     + Interfaces
│  ├── Features/*/Queries             │
│  ├── Features/*/Handlers            │
│  ├── Features/*/Validators          │
│  └── Common/Interfaces/             │
│      IApplicationDbContext ◄────────┤───── CLAVE
└─────────────────────────────────────┘
         ▲
         │ depende de
         │
┌─────────────────────────────────────┐
│ Infrastructure (Persistencia)       │  ← Depende de Domain+App
│  ├── Persistence/Context            │
│  │   └── ApplicationDbContext       │
│  ├── Persistence/Configurations     │
│  ├── Services                       │
│  └── Migrations                     │
└─────────────────────────────────────┘
         ▲
         │ depende de
         │
┌─────────────────────────────────────┐
│   Api (Controllers)                 │  ← Depende de Application
│  ├── Controllers/                   │
│  ├── Middlewares/                   │
│  └── Program.cs (DI)                │
└─────────────────────────────────────┘
```

---

## 📁 ESTRUCTURA DE PROYECTOS

### Árbol Actualizado (Sin Repository Pattern)

```
CGVStockApp/
│
├── CGVStockApp.Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── Category.cs
│   │   ├── Product.cs
│   │   ├── Sale.cs
│   │   ├── SaleDetail.cs
│   │   ├── StockMovement.cs
│   │   └── AccountingMovement.cs        ← MANTENIDO
│   │
│   ├── Enums/
│   │   ├── RoleType.cs
│   │   ├── CustomerType.cs
│   │   ├── PaymentMethodType.cs
│   │   ├── StockMovementType.cs
│   │   └── AccountingMovementType.cs
│   │
│   ├── Common/
│   │   ├── AuditableEntity.cs
│   │   └── IAggregateRoot.cs
│   │
│   └── Exceptions/
│       ├── DomainException.cs
│       ├── InsufficientStockException.cs
│       └── InvalidProductException.cs
│
├── CGVStockApp.Application/
│   ├── Features/
│   │   ├── Authentication/
│   │   │   ├── Commands/
│   │   │   │   └── LoginCommand.cs
│   │   │   ├── Handlers/
│   │   │   │   └── LoginCommandHandler.cs
│   │   │   ├── DTOs/
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   └── LoginResponse.cs
│   │   │   └── Validators/
│   │   │       └── LoginCommandValidator.cs
│   │   │
│   │   ├── Products/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateProductCommand.cs
│   │   │   │   ├── UpdateProductCommand.cs
│   │   │   │   ├── DeleteProductCommand.cs
│   │   │   │   └── UpdatePriceCommand.cs
│   │   │   ├── Handlers/
│   │   │   │   ├── CreateProductCommandHandler.cs
│   │   │   │   ├── UpdatePriceCommandHandler.cs
│   │   │   │   └── ... (otros handlers)
│   │   │   ├── Queries/
│   │   │   │   ├── GetProductsQuery.cs
│   │   │   │   ├── SearchProductsQuery.cs
│   │   │   │   ├── GetProductByIdQuery.cs
│   │   │   │   └── GetMinimumStockProductsQuery.cs
│   │   │   ├── QueryHandlers/
│   │   │   │   ├── GetProductsQueryHandler.cs
│   │   │   │   ├── SearchProductsQueryHandler.cs
│   │   │   │   └── ... (otros query handlers)
│   │   │   ├── DTOs/
│   │   │   │   ├── CreateProductRequest.cs
│   │   │   │   ├── UpdateProductRequest.cs
│   │   │   │   └── ProductResponse.cs
│   │   │   ├── Validators/
│   │   │   │   ├── CreateProductCommandValidator.cs
│   │   │   │   └── UpdateProductCommandValidator.cs
│   │   │   └── Mappings/
│   │   │       └── ProductMappingProfile.cs    ← Solo complejos
│   │   │
│   │   ├── Categories/ ... (similar)
│   │   ├── Subcategories/ ... (similar)
│   │   │
│   │   ├── Sales/
│   │   │   ├── Commands/
│   │   │   │   └── CreateSaleCommand.cs
│   │   │   ├── Handlers/
│   │   │   │   └── CreateSaleCommandHandler.cs
│   │   │   ├── Queries/
│   │   │   │   ├── GetSalesQuery.cs
│   │   │   │   ├── GetSaleByIdQuery.cs
│   │   │   │   └── GetSalesByPeriodQuery.cs
│   │   │   ├── QueryHandlers/
│   │   │   │   └── ... (query handlers)
│   │   │   ├── DTOs/
│   │   │   │   ├── CreateSaleRequest.cs
│   │   │   │   └── SaleResponse.cs
│   │   │   ├── Validators/
│   │   │   │   └── CreateSaleCommandValidator.cs
│   │   │   └── Mappings/
│   │   │       └── SaleMappingProfile.cs    ← Si es necesario
│   │   │
│   │   ├── Users/ ... (similar)
│   │   ├── Roles/ ... (similar)
│   │   │
│   │   └── Dashboard/
│   │       ├── Queries/
│   │       │   ├── GetDailySalesQuery.cs
│   │       │   ├── GetWeeklySalesQuery.cs
│   │       │   ├── GetMonthlySalesQuery.cs
│   │       │   └── GetTotalSalesQuery.cs
│   │       ├── QueryHandlers/
│   │       │   └── ... (handlers con Select())
│   │       └── DTOs/
│   │           ├── DashboardResponse.cs
│   │           └── AccountingMovementDto.cs
│   │
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs
│   │   │   ├── LoggingBehavior.cs
│   │   │   └── PerformanceBehavior.cs
│   │   ├── Exceptions/
│   │   │   ├── ValidationException.cs
│   │   │   └── NotFoundException.cs
│   │   ├── Interfaces/
│   │   │   ├── IRequest.cs
│   │   │   └── IApplicationDbContext.cs    ← CLAVE (reemplaza IUnitOfWork)
│   │   └── Constants/
│   │       └── ErrorMessages.cs
│   │
│   ├── DependencyInjection.cs
│   └── CGVStockApp.Application.csproj
│
├── CGVStockApp.Infrastructure/
│   ├── Persistence/
│   │   ├── Context/
│   │   │   └── ApplicationDbContext.cs    ← Implementa IApplicationDbContext
│   │   │
│   │   ├── Configurations/
│   │   │   ├── ProductConfiguration.cs
│   │   │   ├── SaleConfiguration.cs
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── CategoryConfiguration.cs
│   │   │   ├── SubcategoryConfiguration.cs
│   │   │   ├── StockMovementConfiguration.cs
│   │   │   └── AccountingMovementConfiguration.cs
│   │   │
│   │   └── Migrations/
│   │       └── ... (migraciones cronológicas)
│   │
│   ├── Services/
│   │   ├── JwtTokenService.cs
│   │   ├── PasswordHashService.cs
│   │   └── PdfGenerationService.cs
│   │
│   ├── DependencyInjection.cs
│   └── CGVStockApp.Infrastructure.csproj
│
├── CGVStockApp.Api/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ProductController.cs
│   │   ├── CategoryController.cs
│   │   ├── SubcategoryController.cs
│   │   ├── SalesController.cs
│   │   ├── UserController.cs
│   │   └── DashboardController.cs
│   │
│   ├── Middlewares/
│   │   ├── ErrorHandlingMiddleware.cs
│   │   └── JwtMiddleware.cs
│   │
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── CGVStockApp.Api.csproj
│
└── frontend/
    ├── src/
    │   ├── pages/
    │   ├── components/
    │   ├── services/
    │   ├── store/
    │   └── App.jsx
    └── package.json
```

---

## 🔌 ACCESO A DATOS: IApplicationDbContext

### CLAVE: Interfaz en Application, Implementación en Infrastructure

**Application/Common/Interfaces/IApplicationDbContext.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Domain.Entities;

namespace CGVStockApp.Application.Common.Interfaces
{
    /// <summary>
    /// Abstracción de acceso a datos.
    /// Handlers dependen de esta interfaz, NO del DbContext concreto.
    /// </summary>
    public interface IApplicationDbContext
    {
        DbSet<Product> Products { get; }
        DbSet<Category> Categories { get; }
        DbSet<Subcategory> Subcategories { get; }
        DbSet<Sale> Sales { get; }
        DbSet<SaleDetail> SaleDetails { get; }
        DbSet<StockMovement> StockMovements { get; }
        DbSet<AccountingMovement> AccountingMovements { get; }
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
```

**Infrastructure/Persistence/Context/ApplicationDbContext.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Infrastructure.Persistence.Configurations;

namespace CGVStockApp.Infrastructure.Persistence.Context
{
    /// <summary>
    /// DbContext concreto que implementa IApplicationDbContext.
    /// Contiene toda la configuración de entidades.
    /// </summary>
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        
        // DbSets
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<AccountingMovement> AccountingMovements { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Aplicar configuraciones
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new SubcategoryConfiguration());
            modelBuilder.ApplyConfiguration(new SaleConfiguration());
            modelBuilder.ApplyConfiguration(new SaleDetailConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new StockMovementConfiguration());
            modelBuilder.ApplyConfiguration(new AccountingMovementConfiguration());
        }
    }
}
```

### Inyección en Program.cs

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    // Registrar DbContext
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
    
    // Registrar interfaz (handlers dependen de esto)
    services.AddScoped<IApplicationDbContext>(provider =>
        provider.GetRequiredService<ApplicationDbContext>());
    
    // Registrar servicios
    services.AddScoped<JwtTokenService>();
    services.AddScoped<PasswordHashService>();
    services.AddScoped<PdfGenerationService>();
    
    return services;
}
```

### Uso en Handlers (ANTES vs AHORA)

**ANTES (con Repository):**
```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    
    public CreateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product { /* ... */ };
        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProductResponse>(product);
    }
}
```

**AHORA (DbContext directo):**
```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    
    public CreateProductCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product { /* ... */ };
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProductResponse>(product);
    }
}
```

**Ventajas:**
- ✅ Menos código
- ✅ Menos abstracciones
- ✅ Más directo y legible
- ✅ EF Core IS la abstracción

---

## 🔄 FLUJO CQRS CORREGIDO

### Concepto Correcto

**COMMANDS** (Modifican estado):
- Create Product
- Update Product
- Delete Product
- Update Price (acción de negocio)
- Create Sale
- Process Refund
- Register Expense

**QUERIES** (Leen sin modificar):
- Get Products
- Search Products
- Get Minimum Stock
- Get Dashboard Data
- Get Sales by Period
- Get User Info

### Ejemplo 1: CreateProductCommand

```
1. USUARIO envía POST /api/products
   {"name": "Manzana", "price": 100, ...}

2. CONTROLLER
   await _mediator.Send(new CreateProductCommand { ... })

3. APPLICATION (CQRS)
   ├─ ValidationBehavior ejecuta CreateProductCommandValidator
   │  └─ Valida reglas: nombre requerido, precio > 0, etc.
   │
   ├─ CreateProductCommandHandler.Handle()
   │  ├─ Crea entidad: var product = new Product { ... }
   │  ├─ Accede directo: _context.Products.Add(product)
   │  ├─ Persiste: await _context.SaveChangesAsync()
   │  └─ Mapea a DTO: return _mapper.Map<ProductResponse>(product)
   │
   └─ PerformanceBehavior registra tiempo

4. CONTROLLER
   └─ Retorna 201 Created { id, name, price, ... }
```

### Ejemplo 2: GetProductsQuery

```
1. USUARIO envía GET /api/products?categoryId=5

2. CONTROLLER
   await _mediator.Send(new GetProductsQuery { CategoryId = 5 })

3. APPLICATION (CQRS)
   ├─ GetProductsQueryHandler.Handle()
   │  ├─ Query sin repositorio:
   │  │  var products = await _context.Products
   │  │      .Where(p => p.CategoryId == 5)
   │  │      .Select(p => new ProductResponse
   │  │      {
   │  │          Id = p.Id,
   │  │          Name = p.Name,
   │  │          Price = p.PublicPrice
   │  │      })
   │  │      .ToListAsync()
   │  │
   │  └─ Retorna List<ProductResponse>
   │
   └─ LoggingBehavior registra búsqueda

4. CONTROLLER
   └─ Retorna 200 OK [ { id, name, price }, ... ]
```

---

## 🎯 PATRONES DE IMPLEMENTACIÓN

### 1. CQRS con MediatR

```
Command Request
    ↓
MediatR Pipeline
    ├─ Behaviors (Validation, Logging)
    └─ Handler
        ├─ Ejecuta lógica
        ├─ IApplicationDbContext.SaveChangesAsync()
        └─ Retorna Response
```

### 2. Sin AutoMapper (Queries)

**❌ NO HAGAS:**
```csharp
var products = await _context.Products.ToListAsync();
return _mapper.Map<List<ProductResponse>>(products);
```

**✅ HAZ:**
```csharp
var products = await _context.Products
    .Select(p => new ProductResponse
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.PublicPrice,
        Stock = p.AvailableStock
    })
    .ToListAsync();
return products;
```

**Razón:** Select() es más rápido (proyecta en BD), más explícito, sin dependencias.

### 3. AutoMapper Solo para Complejos

**✅ USA AutoMapper:**
```csharp
// Command complejos con lógica de transformación
CreateMap<CreateProductCommand, Product>()
    .ForMember(dest => dest.ProductCode, 
        opt => opt.MapFrom(_ => GenerateProductCode()))
    .ForMember(dest => dest.AvailableStock,
        opt => opt.MapFrom(src => src.InitialStock));
```

**❌ NO USES:**
```csharp
// Simples 1-a-1, sin transformación
CreateMap<GetProductsQuery, Product>();
CreateMap<Product, ProductResponse>();
```

### 4. FluentValidation en Validators

```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100);
        
        RuleFor(x => x.PublicPrice)
            .GreaterThan(0).WithMessage("Precio debe ser positivo");
        
        RuleFor(x => x.CategoryId)
            .MustAsync(async (id, ct) =>
            {
                if (!id.HasValue) return true;
                return await context.Categories
                    .AnyAsync(c => c.Id == id.Value, ct);
            })
            .WithMessage("Categoría no existe");
    }
}
```

### 5. Pipeline Behaviors

**ValidationBehavior:**
```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();
        
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }
        
        if (failures.Any())
            throw new ValidationException("Validation failed", failures);
        
        return await next();
    }
}
```

---

## 🎪 FEATURES DEL PROYECTO

### Feature 1: Authentication
- **Command:** LoginCommand
- **Handler:** LoginCommandHandler (genera JWT)
- **DTO:** LoginRequest, LoginResponse
- **Validator:** LoginCommandValidator

### Feature 2: Products
- **Commands:** CreateProductCommand, UpdateProductCommand, DeleteProductCommand, UpdatePriceCommand
- **Queries:** GetProductsQuery, SearchProductsQuery, GetMinimumStockProductsQuery, GetProductByIdQuery
- **DTO:** CreateProductRequest, ProductResponse
- **AutoMapper:** Solo si hay transformación compleja
- **Queries:** Usan Select() explícito

### Feature 3: Sales
- **Command:** CreateSaleCommand (lógica compleja)
  - Valida stock
  - Reduce AvailableStock
  - Registra AccountingMovement
  - Calcula precio según CustomerType
- **Queries:** GetSalesQuery, GetSalesByPeriodQuery
- **DTO:** CreateSaleRequest, SaleResponse

### Feature 4: Dashboard
- **Queries:** GetDailySalesQuery, GetWeeklySalesQuery, GetMonthlySalesQuery
- **Handlers:** Combinan Sales + AccountingMovements con Select()
- **DTO:** DashboardResponse con AccountingMovementDto

### Feature 5: Users, Categories, Subcategories
- **CRUD completo** (Commands + Queries)
- **Especiales:** UpdatePasswordCommand con hash

---

## 🏛️ ENTIDADES CLAVE

### AccountingMovement (MANTENIDA)

```csharp
public class AccountingMovement : AuditableEntity
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }
    public AccountingMovementType Type { get; set; } // Sale, Expense, Refund
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsIncome { get; set; } // true = entrada, false = salida
    
    // Relaciones
    public int? SaleId { get; set; }
    public Sale Sale { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
}
```

**Uso:**
- Sale registra automáticamente entrada
- Admin registra manualmente salidas (gastos)
- Dashboard combina para "libro diario"

### Product (Con ProductCode)

```csharp
public class Product : AuditableEntity, IAggregateRoot
{
    public int Id { get; set; }
    public string ProductCode { get; set; } // AANNNN (UNIQUE en BD)
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal PublicPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int InitialStock { get; set; }
    public int AvailableStock { get; set; }
    public int AlertStock { get; set; }
    
    public int? CategoryId { get; set; }
    public Category Category { get; set; }
    
    public int? SubcategoryId { get; set; }
    public Subcategory Subcategory { get; set; }
    
    // Lógica de dominio
    public void DecreaseStock(int quantity)
    {
        if (quantity > AvailableStock)
            throw new InsufficientStockException(
                $"Stock insuficiente. Disponible: {AvailableStock}");
        AvailableStock -= quantity;
    }
    
    public void UpdatePrices(decimal percentageChange)
    {
        decimal factor = 1 + (percentageChange / 100m);
        PublicPrice = Math.Round(PublicPrice * factor, 2);
        WholesalePrice = Math.Round(WholesalePrice * factor, 2);
    }
}
```

**ProductCode Generación:**
- Formato: `AANNNN`
- Ejemplo: `FL0001` (Frutas - Limón - 0001)
- Generado automáticamente en handler
- UNIQUE constraint en BD para garantizar unicidad

---

## ✅ RESUMEN DE DECISIONES APLICADAS

| Decisión | Estado | Impacto |
|----------|--------|--------|
| **Sin Repository Pattern** | ✅ Aplicada | Menos código, acceso directo vía IApplicationDbContext |
| **AutoMapper limitado** | ✅ Aplicada | Queries con Select(), Commands con AutoMapper si es necesario |
| **AccountingMovement** | ✅ Mantenida | Requisito de Dashboard/Libro Diario |
| **ProductCode AANNNN** | ✅ Sin cambios | + UNIQUE constraint en BD |
| **Custom Auth + JWT** | ✅ Mantenida | Sin ASP.NET Core Identity (plazos) |
| **Corrección CQRS** | ✅ Aplicada | Commands = C/U/D, Queries = R |

---

## 🎯 CONCLUSIÓN

CGVStockApp v2.0 implementa:
- ✅ CQRS con MediatR (sin complejidades innecesarias)
- ✅ DbContext directo (sin Repository Pattern)
- ✅ AutoMapper selectivo + Select() para performance
- ✅ AccountingMovement para Dashboard
- ✅ ProductCode automático AANNNN
- ✅ Auth custom + JWT
- ✅ Clean Architecture mantenida
- ✅ 45-50 días de plazo

**Resultado:** Arquitectura limpia, mantenible, sin sobrecarga de abstracciones innecesarias.

---

**Próximo documento:** Implementación práctica actualizada (sin Repository, con IApplicationDbContext)
